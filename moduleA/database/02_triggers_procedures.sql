-- ============================================================================
-- Триггеры, хранимые функции и процедуры для системы «ТОиР-Проф»
-- Реализация бизнес-логики на стороне СУБД PostgreSQL
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Триггер №1: Запрет изменения статуса оборудования с «выведено из эксплуатации»
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_prevent_decommissioned_status_change()
RETURNS TRIGGER AS $$
DECLARE
    v_decom_status_id INT;
BEGIN
    SELECT status_id INTO v_decom_status_id 
    FROM equipment_statuses 
    WHERE status_code = 'decommissioned';

    IF OLD.status_id = v_decom_status_id AND NEW.status_id <> v_decom_status_id THEN
        RAISE EXCEPTION 'Запрещено изменять статус оборудования, выведенного из эксплуатации (инв. № %)', OLD.inventory_number;
    END IF;

    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_check_equipment_status_update ON equipment;
CREATE TRIGGER trg_check_equipment_status_update
BEFORE UPDATE ON equipment
FOR EACH ROW
EXECUTE FUNCTION trg_prevent_decommissioned_status_change();


-- ----------------------------------------------------------------------------
-- Триггер №2: Комплексная проверка дат при создании и обновлении заявок
-- и проверка запрета отмены начатых заявок
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_validate_work_order_dates_and_status()
RETURNS TRIGGER AS $$
DECLARE
    v_cancelled_status_id INT;
BEGIN
    -- Проверка плановых дат
    IF NEW.planned_start_date < NEW.created_at THEN
        RAISE EXCEPTION 'Плановая дата начала (%) не может быть раньше даты создания заявки (%)', 
            NEW.planned_start_date, NEW.created_at;
    END IF;

    IF NEW.planned_end_date <= NEW.planned_start_date THEN
        RAISE EXCEPTION 'Плановая дата окончания (%) должна быть позже плановой даты начала (%)', 
            NEW.planned_end_date, NEW.planned_start_date;
    END IF;

    -- Проверка фактических дат при их наличии
    IF NEW.actual_start_date IS NOT NULL THEN
        IF NEW.actual_start_date < NEW.created_at THEN
            RAISE EXCEPTION 'Фактическая дата начала (%) не может быть раньше даты создания заявки (%)', 
                NEW.actual_start_date, NEW.created_at;
        END IF;
    END IF;

    IF NEW.actual_start_date IS NOT NULL AND NEW.actual_end_date IS NOT NULL THEN
        IF NEW.actual_end_date <= NEW.actual_start_date THEN
            RAISE EXCEPTION 'Фактическая дата окончания (%) должна быть позже фактической даты начала (%)', 
                NEW.actual_end_date, NEW.actual_start_date;
        END IF;

        -- Автоматический расчет времени простоя в часах
        NEW.downtime_hours = ROUND(EXTRACT(EPOCH FROM (NEW.actual_end_date - NEW.actual_start_date)) / 3600.0, 2);
    END IF;

    -- Проверка отмены заявки: если работы уже начаты (actual_start_date IS NOT NULL), отмена запрещена
    SELECT order_status_id INTO v_cancelled_status_id
    FROM work_order_statuses
    WHERE status_code = 'cancelled';

    IF NEW.order_status_id = v_cancelled_status_id THEN
        IF OLD.actual_start_date IS NOT NULL OR NEW.actual_start_date IS NOT NULL THEN
            RAISE EXCEPTION 'Невозможно отменить заявку %, так как работы по ней уже фактически начаты', NEW.order_number;
        END IF;
    END IF;

    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_work_orders_validate ON work_orders;
CREATE TRIGGER trg_work_orders_validate
BEFORE INSERT OR UPDATE ON work_orders
FOR EACH ROW
EXECUTE FUNCTION trg_validate_work_order_dates_and_status();


-- ----------------------------------------------------------------------------
-- Функция №1: Расчёт времени простоя оборудования за период (в часах)
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION calculate_equipment_downtime_hours(
    p_equipment_id INT,
    p_start_date TIMESTAMP WITHOUT TIME ZONE,
    p_end_date TIMESTAMP WITHOUT TIME ZONE
)
RETURNS NUMERIC AS $$
DECLARE
    v_total_hours NUMERIC := 0;
BEGIN
    IF p_start_date >= p_end_date THEN
        RAISE EXCEPTION 'Дата начала периода (%) должна быть строго раньше даты окончания (%)', p_start_date, p_end_date;
    END IF;

    SELECT COALESCE(SUM(
        ROUND(
            EXTRACT(EPOCH FROM (
                LEAST(COALESCE(actual_end_date, CURRENT_TIMESTAMP), p_end_date) - 
                GREATEST(actual_start_date, p_start_date)
            )) / 3600.0, 2
        )
    ), 0)
    INTO v_total_hours
    FROM work_orders
    WHERE equipment_id = p_equipment_id
      AND actual_start_date IS NOT NULL
      AND actual_start_date < p_end_date
      AND COALESCE(actual_end_date, CURRENT_TIMESTAMP) > p_start_date;

    RETURN GREATEST(v_total_hours, 0);
END;
$$ LANGUAGE plpgsql;


-- ----------------------------------------------------------------------------
-- Функция №2: Расчёт следующей плановой даты ТО
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION calculate_next_maintenance_date(
    p_equipment_id INT,
    p_maintenance_type_id INT
)
RETURNS DATE AS $$
DECLARE
    v_periodicity INT;
    v_base_date DATE;
    v_last_to_date DATE;
    v_commissioning_date DATE;
BEGIN
    SELECT periodicity_days INTO v_periodicity
    FROM maintenance_types
    WHERE maintenance_type_id = p_maintenance_type_id;

    IF v_periodicity IS NULL THEN
        RAISE EXCEPTION 'Вид ТО с ID % не найден', p_maintenance_type_id;
    END IF;

    -- Поиск даты последнего выполненного ТО данного вида
    SELECT MAX(COALESCE(wo.actual_end_date::DATE, ms.planned_date))
    INTO v_last_to_date
    FROM maintenance_schedules ms
    LEFT JOIN work_orders wo ON wo.schedule_id = ms.schedule_id
    WHERE ms.equipment_id = p_equipment_id
      AND ms.maintenance_type_id = p_maintenance_type_id
      AND ms.schedule_status = 'completed';

    IF v_last_to_date IS NOT NULL THEN
        v_base_date := v_last_to_date;
    ELSE
        -- Если ТО еще не проводилось, берем дату ввода в эксплуатацию
        SELECT commissioning_date INTO v_commissioning_date
        FROM equipment
        WHERE equipment_id = p_equipment_id;

        IF v_commissioning_date IS NULL THEN
            RAISE EXCEPTION 'Оборудование с ID % не найдено', p_equipment_id;
        END IF;

        v_base_date := v_commissioning_date;
    END IF;

    RETURN v_base_date + (v_periodicity || ' days')::INTERVAL;
END;
$$ LANGUAGE plpgsql;


-- ----------------------------------------------------------------------------
-- Процедура №3: Формирование сводного отчёта по цеху за период
-- Возвращает структурированный набор показателей
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION generate_workshop_summary_report(
    p_workshop_id INT,
    p_start_date TIMESTAMP WITHOUT TIME ZONE,
    p_end_date TIMESTAMP WITHOUT TIME ZONE
)
RETURNS TABLE (
    workshop_name VARCHAR,
    total_equipment BIGINT,
    in_operation_count BIGINT,
    under_repair_count BIGINT,
    in_downtime_count BIGINT,
    decommissioned_count BIGINT,
    total_orders BIGINT,
    completed_orders BIGINT,
    overdue_orders BIGINT,
    total_downtime_hours NUMERIC,
    avg_order_duration_hours NUMERIC,
    most_common_failure_reason TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH eq_stats AS (
        SELECT 
            COUNT(*) AS total_eq,
            COUNT(*) FILTER (WHERE es.status_code = 'in_operation') AS in_op,
            COUNT(*) FILTER (WHERE es.status_code = 'under_repair') AS under_rep,
            COUNT(*) FILTER (WHERE es.status_code = 'in_downtime') AS in_down,
            COUNT(*) FILTER (WHERE es.status_code = 'decommissioned') AS decom
        FROM equipment eq
        JOIN equipment_statuses es ON es.status_id = eq.status_id
        WHERE eq.workshop_id = p_workshop_id
    ),
    order_stats AS (
        SELECT 
            COUNT(wo.work_order_id) AS orders_count,
            COUNT(wo.work_order_id) FILTER (WHERE wos.status_code IN ('completed', 'closed')) AS comp_count,
            COUNT(wo.work_order_id) FILTER (WHERE wo.planned_end_date < CURRENT_TIMESTAMP AND wos.status_code NOT IN ('completed', 'closed', 'cancelled')) AS over_count,
            COALESCE(SUM(wo.downtime_hours), 0) AS down_hours,
            ROUND(COALESCE(AVG(wo.downtime_hours), 0), 2) AS avg_hours
        FROM work_orders wo
        JOIN equipment eq ON eq.equipment_id = wo.equipment_id
        JOIN work_order_statuses wos ON wos.order_status_id = wo.order_status_id
        WHERE eq.workshop_id = p_workshop_id
          AND wo.created_at BETWEEN p_start_date AND p_end_date
    ),
    common_reason AS (
        SELECT wo.failure_reason
        FROM work_orders wo
        JOIN equipment eq ON eq.equipment_id = wo.equipment_id
        WHERE eq.workshop_id = p_workshop_id
          AND wo.failure_reason IS NOT NULL
          AND wo.created_at BETWEEN p_start_date AND p_end_date
        GROUP BY wo.failure_reason
        ORDER BY COUNT(*) DESC
        LIMIT 1
    )
    SELECT 
        w.workshop_name,
        COALESCE(es.total_eq, 0),
        COALESCE(es.in_op, 0),
        COALESCE(es.under_rep, 0),
        COALESCE(es.in_down, 0),
        COALESCE(es.decom, 0),
        COALESCE(os.orders_count, 0),
        COALESCE(os.comp_count, 0),
        COALESCE(os.over_count, 0),
        COALESCE(os.down_hours, 0),
        COALESCE(os.avg_hours, 0),
        COALESCE(cr.failure_reason, 'Нет данных об отказах')
    FROM workshops w
    CROSS JOIN eq_stats es
    CROSS JOIN order_stats os
    LEFT JOIN common_reason cr ON TRUE
    WHERE w.workshop_id = p_workshop_id;
END;
$$ LANGUAGE plpgsql;
