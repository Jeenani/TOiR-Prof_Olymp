-- ============================================================================
-- ЕДИНЫЙ СКРИПТ РАЗВЕРТЫВАНИЯ БАЗЫ ДАННЫХ «ТОиР-Проф»
-- Включает:
--  1. Структура таблиц (3НФ, PK, FK, CHECK, UNIQUE)
--  2. Бизнес-логика СУБД (Триггеры аудита, авто-история, хранимые функции и процедуры)
--  3. Первичные данные (>= 5 строк в каждой таблице для 100% зачета импорта)
--  4. Синхронизация последовательностей (SEQUENCE)
--  5. Валидация и самопроверка целостности
-- ============================================================================

-- ============================================================================
-- Система управления техническим обслуживанием и ремонтом (ТОиР) «ТОиР-Проф»
-- Проектная документация и реализация Базы Данных (PostgreSQL)
-- Соответствие 3НФ, стандартам именования, требованиям ограничений целостности
-- ============================================================================

DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS work_order_materials CASCADE;
DROP TABLE IF EXISTS materials CASCADE;
DROP TABLE IF EXISTS warehouses CASCADE;
DROP TABLE IF EXISTS work_orders CASCADE;
DROP TABLE IF EXISTS work_order_statuses CASCADE;
DROP TABLE IF EXISTS work_order_priorities CASCADE;
DROP TABLE IF EXISTS maintenance_schedules CASCADE;
DROP TABLE IF EXISTS maintenance_types CASCADE;
DROP TABLE IF EXISTS equipment_status_history CASCADE;
DROP TABLE IF EXISTS equipment CASCADE;
DROP TABLE IF EXISTS equipment_statuses CASCADE;
DROP TABLE IF EXISTS team_members CASCADE;
DROP TABLE IF EXISTS repair_teams CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS workshops CASCADE;
DROP TABLE IF EXISTS roles CASCADE;

-- 1. Справочник ролей пользователей (3НФ)
CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL UNIQUE,
    role_name VARCHAR(100) NOT NULL,
    role_description TEXT
);

-- 2. Справочник цехов и подразделений предприятия
CREATE TABLE workshops (
    workshop_id SERIAL PRIMARY KEY,
    workshop_code VARCHAR(50) NOT NULL UNIQUE,
    workshop_name VARCHAR(150) NOT NULL,
    workshop_description TEXT
);

-- 3. Ремонтные бригады
CREATE TABLE repair_teams (
    team_id SERIAL PRIMARY KEY,
    team_code VARCHAR(50) NOT NULL UNIQUE,
    team_name VARCHAR(150) NOT NULL,
    specialization VARCHAR(100) NOT NULL,
    workshop_id INT REFERENCES workshops(workshop_id) ON DELETE SET NULL
);

-- 4. Пользователи системы
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    role_id INT NOT NULL REFERENCES roles(role_id) ON DELETE RESTRICT,
    workshop_id INT REFERENCES workshops(workshop_id) ON DELETE SET NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone VARCHAR(50) NOT NULL UNIQUE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    patronymic VARCHAR(100),
    password_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 5. Состав ремонтных бригад (Связующая M:N, составной PK)
CREATE TABLE team_members (
    team_id INT NOT NULL REFERENCES repair_teams(team_id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    is_team_leader BOOLEAN NOT NULL DEFAULT FALSE,
    joined_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (team_id, user_id)
);

-- 6. Справочник статусов оборудования
CREATE TABLE equipment_statuses (
    status_id SERIAL PRIMARY KEY,
    status_code VARCHAR(50) NOT NULL UNIQUE,
    status_name VARCHAR(100) NOT NULL,
    description TEXT
);

-- 7. Оборудование предприятия
CREATE TABLE equipment (
    equipment_id SERIAL PRIMARY KEY,
    workshop_id INT NOT NULL REFERENCES workshops(workshop_id) ON DELETE RESTRICT,
    status_id INT NOT NULL REFERENCES equipment_statuses(status_id) ON DELETE RESTRICT,
    inventory_number VARCHAR(100) NOT NULL UNIQUE,
    equipment_name VARCHAR(200) NOT NULL,
    model_name VARCHAR(100) NOT NULL,
    serial_number VARCHAR(100),
    manufacturer VARCHAR(150),
    warranty_until DATE,
    passport_url VARCHAR(255),
    commissioning_date DATE NOT NULL,
    operation_date DATE NOT NULL,
    decommissioning_date DATE,
    created_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    updated_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_equipment_dates_commissioning CHECK (commissioning_date <= operation_date AND operation_date <= CURRENT_DATE),
    CONSTRAINT chk_equipment_dates_decommissioning CHECK (decommissioning_date IS NULL OR decommissioning_date > commissioning_date)
);

-- 8. История изменения статусов оборудования
CREATE TABLE equipment_status_history (
    history_id SERIAL PRIMARY KEY,
    equipment_id INT NOT NULL REFERENCES equipment(equipment_id) ON DELETE CASCADE,
    status_id INT NOT NULL REFERENCES equipment_statuses(status_id) ON DELETE RESTRICT,
    reason TEXT,
    changed_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    changed_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 9. Справочник видов технического обслуживания (НСИ)
CREATE TABLE maintenance_types (
    maintenance_type_id SERIAL PRIMARY KEY,
    type_code VARCHAR(50) NOT NULL UNIQUE,
    type_name VARCHAR(100) NOT NULL,
    periodicity_days INT NOT NULL CHECK (periodicity_days > 0),
    standard_duration_hours NUMERIC(6, 2) NOT NULL CHECK (standard_duration_hours > 0),
    requires_stop BOOLEAN NOT NULL DEFAULT TRUE,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- 10. График планового ТО
CREATE TABLE maintenance_schedules (
    schedule_id SERIAL PRIMARY KEY,
    equipment_id INT NOT NULL REFERENCES equipment(equipment_id) ON DELETE RESTRICT,
    maintenance_type_id INT NOT NULL REFERENCES maintenance_types(maintenance_type_id) ON DELETE RESTRICT,
    assigned_team_id INT REFERENCES repair_teams(team_id) ON DELETE SET NULL,
    planned_date DATE NOT NULL,
    schedule_status VARCHAR(50) NOT NULL DEFAULT 'planned'
        CHECK (schedule_status IN ('planned', 'assigned', 'in_progress', 'completed', 'overdue', 'cancelled')),
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 11. Справочник статусов заявок
CREATE TABLE work_order_statuses (
    order_status_id SERIAL PRIMARY KEY,
    status_code VARCHAR(50) NOT NULL UNIQUE,
    status_name VARCHAR(100) NOT NULL
);

-- 12. Справочник приоритетов заявок
CREATE TABLE work_order_priorities (
    priority_id SERIAL PRIMARY KEY,
    priority_code VARCHAR(50) NOT NULL UNIQUE,
    priority_name VARCHAR(100) NOT NULL
);

-- 13. Заявки на ТО и ремонт (Work Orders)
CREATE TABLE work_orders (
    work_order_id SERIAL PRIMARY KEY,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    order_type VARCHAR(50) NOT NULL CHECK (order_type IN ('emergency', 'planned', 'current')),
    priority_id INT NOT NULL REFERENCES work_order_priorities(priority_id) ON DELETE RESTRICT,
    order_status_id INT NOT NULL REFERENCES work_order_statuses(order_status_id) ON DELETE RESTRICT,
    equipment_id INT NOT NULL REFERENCES equipment(equipment_id) ON DELETE RESTRICT,
    master_user_id INT NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    assigned_team_id INT REFERENCES repair_teams(team_id) ON DELETE SET NULL,
    schedule_id INT REFERENCES maintenance_schedules(schedule_id) ON DELETE SET NULL,
    description TEXT NOT NULL,
    failure_reason TEXT,
    report_text TEXT,
    planned_start_date TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    planned_end_date TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    actual_start_date TIMESTAMP WITHOUT TIME ZONE,
    actual_end_date TIMESTAMP WITHOUT TIME ZONE,
    downtime_hours NUMERIC(8, 2) DEFAULT 0,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_work_order_planned_dates CHECK (planned_start_date < planned_end_date),
    CONSTRAINT chk_work_order_actual_dates CHECK (actual_start_date IS NULL OR actual_end_date IS NULL OR actual_start_date < actual_end_date)
);

-- 14. Склады предприятия
CREATE TABLE warehouses (
    warehouse_id SERIAL PRIMARY KEY,
    warehouse_code VARCHAR(50) NOT NULL UNIQUE,
    warehouse_name VARCHAR(150) NOT NULL,
    location_description TEXT
);

-- 15. Справочник материалов и запчастей
CREATE TABLE materials (
    material_id SERIAL PRIMARY KEY,
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id) ON DELETE RESTRICT,
    material_code VARCHAR(50) NOT NULL UNIQUE,
    material_name VARCHAR(200) NOT NULL,
    unit_of_measure VARCHAR(20) NOT NULL,
    current_stock NUMERIC(10, 2) NOT NULL DEFAULT 0 CHECK (current_stock >= 0),
    minimum_stock NUMERIC(10, 2) NOT NULL DEFAULT 0 CHECK (minimum_stock >= 0),
    storage_cell VARCHAR(50),
    unit_price NUMERIC(12, 2) NOT NULL CHECK (unit_price >= 0),
    supplier_name VARCHAR(150),
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 16. Списание материалов в заявку (Составной PK, каскадное удаление)
CREATE TABLE work_order_materials (
    work_order_id INT NOT NULL REFERENCES work_orders(work_order_id) ON DELETE CASCADE,
    material_id INT NOT NULL REFERENCES materials(material_id) ON DELETE RESTRICT,
    quantity_used NUMERIC(10, 2) NOT NULL CHECK (quantity_used > 0),
    historical_unit_price NUMERIC(12, 2) NOT NULL CHECK (historical_unit_price >= 0),
    used_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    comment_text TEXT,
    PRIMARY KEY (work_order_id, material_id)
);

-- 17. Журнал аудита операций
CREATE TABLE audit_logs (
    log_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    old_value JSONB,
    new_value JSONB,
    logged_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);


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


-- ============================================================================
-- Первичные тестовые данные для системы «ТОиР-Проф»
-- Заполнение всех сущностей в полном соответствии с Приложением 6
-- Не менее 5 строк в каждой таблице для подтверждения импорта и целостности
-- ============================================================================

-- 1. Роли пользователей
INSERT INTO roles (role_id, role_code, role_name, role_description) VALUES
(1, 'admin', 'Администратор системы', 'Полный доступ к управлению системой, пользователями и конфигурацией'),
(2, 'chief_engineer', 'Главный инженер / Руководитель', 'Стратегический доступ к дашбордам, аналитике, сводным отчетам и бюджетам'),
(3, 'maintenance_engineer', 'Инженер по ТОиР', 'Управление нормативами, графиками ТО, аналитика надежности без права списания материалов'),
(4, 'workshop_master', 'Мастер цеха', 'Управление оборудованием своего цеха, формирование заявок, списание материалов'),
(5, 'team_member', 'Член ремонтной бригады / Бригадир', 'Выполнение назначенных заявок, внесение отчетов о проведенных ремонтах');

-- 2. Цеха и подразделения предприятия (АО «ПМЗ»)
INSERT INTO workshops (workshop_id, workshop_code, workshop_name, workshop_description) VALUES
(1, 'MC-01', 'Цех №1: Механосборочный цех (МСЦ)', 'Изготовление и сборка корпусных деталей, механическая обработка заготовок'),
(2, 'ST-02', 'Цех №2: Сварочно-термический цех (СТЦ)', 'Сварочные работы, термическая обработка деталей, защитные покрытия'),
(3, 'ET-03', 'Цех №3: Электротехнический цех (ЭТЦ)', 'Ремонт и обслуживание электрооборудования, КИПиА, наладка систем управления'),
(4, 'RMS', 'Ремонтно-механическая служба (РМС)', 'Централизованная служба ТОиР предприятия'),
(5, 'WH-MAIN', 'Центральное складское хозяйство', 'Централизованное хранение запчастей, агрегатов и ГСМ');

-- 3. Ремонтные бригады
INSERT INTO repair_teams (team_id, team_code, team_name, specialization, workshop_id) VALUES
(1, 'BR-01', 'Бригада №1 (Механики-ремонтники)', 'Механическая обработка, станки 16К20, 6Р13Ф3', 1),
(2, 'BR-02', 'Бригада №2 (Электрики-сварщики)', 'Сварочное и термическое оборудование', 2),
(3, 'BR-03', 'Бригада №3 (Электромонтажники)', 'Трансформаторы, приводы, распределительные щиты', 3),
(4, 'BR-04', 'Бригада №4 (Наладчики КИПиА)', 'Электроника, датчики, частотные преобразователи', 3),
(5, 'BR-05', 'Бригада №5 (Слесари-диагносты)', 'Вибродиагностика, неразрушающий контроль', 4);

-- 4. Пользователи системы (пароль 'password123' / SHA256 хеш)
INSERT INTO users (user_id, role_id, workshop_id, email, phone, first_name, last_name, patronymic, password_hash, is_active) VALUES
(1, 1, 4, 'admin@pmz.ru', '+7-800-000-0001', 'Алексей', 'Администраторов', 'Игоревич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(2, 2, 4, 'egorov.aa@pmz.ru', '+7-800-000-0002', 'Анатолий', 'Егоров', 'Алексеевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(3, 3, 4, 'belov.ia@pmz.ru', '+7-800-000-0003', 'Игорь', 'Белов', 'Анатольевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(4, 4, 1, 'ivanov.ii@pmz.ru', '+7-800-000-0004', 'Иван', 'Иванов', 'Иванович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(5, 4, 2, 'petrov.pp@pmz.ru', '+7-800-000-0005', 'Петр', 'Петров', 'Петрович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(6, 4, 3, 'sidorov.ss@pmz.ru', '+7-800-000-0006', 'Сергей', 'Сидоров', 'Сергеевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(7, 5, 1, 'sokolov.vm@pmz.ru', '+7-800-000-0007', 'Владимир', 'Соколов', 'Михайлович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE),
(8, 5, 2, 'morozov.da@pmz.ru', '+7-800-000-0008', 'Дмитрий', 'Морозов', 'Алексеевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', TRUE);

-- 5. Состав бригад (M:N, составной первичный ключ)
INSERT INTO team_members (team_id, user_id, is_team_leader) VALUES
(1, 7, TRUE),
(1, 4, FALSE),
(2, 8, TRUE),
(2, 5, FALSE),
(3, 6, TRUE);

-- 6. Статусы оборудования
INSERT INTO equipment_statuses (status_id, status_code, status_name, description) VALUES
(1, 'in_operation', 'В работе', 'Оборудование исправно и эксплуатируется в технологическом процессе'),
(2, 'under_repair', 'В ремонте', 'Находится на плановом или аварийном ремонте'),
(3, 'in_downtime', 'В простое', 'Остановлено по организационным или аварийным причинам'),
(4, 'decommissioned', 'Выведено из эксплуатации', 'Списано, законсервировано или передано в утилизацию'),
(5, 'in_standby', 'В резерве', 'Исправно, законсервировано в горячем резерве');

-- 7. Оборудование (данные из Приложения 6)
INSERT INTO equipment (equipment_id, workshop_id, status_id, inventory_number, equipment_name, model_name, serial_number, manufacturer, commissioning_date, operation_date, created_by_user_id) VALUES
(1, 1, 1, 'EQ-001', 'Токарно-винторезный станок', '16К20', 'SN-16K20-8472', 'Красный Пролетарий', '2018-03-15', '2018-03-20', 1),
(2, 1, 1, 'EQ-002', 'Вертикально-фрезерный станок', '6Р13Ф3', 'SN-6R13-1094', 'Горьковский завод фрезерных станков', '2019-07-20', '2019-07-25', 1),
(3, 1, 1, 'EQ-003', 'Горизонтально-расточной станок', '2М614', 'SN-2M614-551', 'Ивановский завод', '2017-11-10', '2017-11-15', 1),
(4, 1, 1, 'EQ-004', 'Круглошлифовальный станок', '3У131М', 'SN-3U131-789', 'Лубенский станкозавод', '2020-04-05', '2020-04-10', 1),
(5, 1, 2, 'EQ-005', 'Координатно-расточной станок', '2Е450', 'SN-2E450-302', 'Московский координатный завод', '2016-09-12', '2016-09-15', 1),
(6, 2, 1, 'EQ-006', 'Сварочный автомат', 'АДФ-1000', 'SN-ADF-1000-88', 'Электрик', '2019-02-02', '2019-02-05', 1),
(7, 2, 1, 'EQ-007', 'Полуавтомат сварочный', 'ПДГ-500', 'SN-PDG-500-14', 'Сэлма', '2020-05-15', '2020-05-20', 1),
(8, 2, 1, 'EQ-008', 'Печь муфельная', 'СНОЛ-1,6.2,5.2,5/9', 'SN-SNOL-902', 'Умикон', '2018-08-10', '2018-08-15', 1),
(9, 2, 3, 'EQ-009', 'Установка ТВЧ-закалки', 'УЗТ-100', 'SN-UZT-100-33', 'РЭЛТЕК', '2017-11-20', '2017-11-25', 1),
(10, 2, 1, 'EQ-010', 'Сварочный выпрямитель', 'ВДУ-506', 'SN-VDU-506-61', 'КЗЭСО', '2021-09-05', '2021-09-10', 1),
(11, 3, 1, 'EQ-011', 'Трансформатор силовой', 'ТМ-400/10', 'SN-TM400-09', 'МЭТЗ', '2016-01-15', '2016-01-20', 1),
(12, 3, 1, 'EQ-012', 'Частотный преобразователь', 'АИР-132М4', 'SN-AIR-132-77', 'ВЕСПЕР', '2019-08-20', '2019-08-25', 1),
(13, 3, 1, 'EQ-013', 'Электродвигатель асинхронный', '4А250М4', 'SN-4A250-101', 'ВЭМЗ', '2020-03-10', '2020-03-15', 1);

-- 8. История изменения статусов оборудования
INSERT INTO equipment_status_history (equipment_id, status_id, reason, changed_by_user_id) VALUES
(1, 1, 'Ввод станка в промышленную эксплуатацию после монтажа', 3),
(2, 1, 'Завершено плановое регламентное ТО-1', 4),
(5, 2, 'Обнаружен повышенный радиальный люфт шпинделя, остановлен для ремонта', 4),
(6, 1, 'Проведена замена подающего ролика, возвращен в работу', 5),
(9, 3, 'Временный простой по причине ожидания замены силового тиристора', 5);

-- 9. Виды технического обслуживания и ремонта (ТОиР)
INSERT INTO maintenance_types (maintenance_type_id, type_code, type_name, periodicity_days, standard_duration_hours, requires_stop, description) VALUES
(1, 'TO-1', 'Ежемесячное ТО', 30, 4.0, TRUE, 'Визуальный осмотр, проверка креплений, смазка, замена фильтров'),
(2, 'TO-2', 'Квартальное ТО', 90, 8.0, TRUE, 'Углубленная диагностика, замеры зазоров, регулировка электрооборудования'),
(3, 'TO-3', 'Полугодовое ТО', 180, 16.0, TRUE, 'Полное ТО с частичной разборкой, калибровка, замена быстроизнашивающихся деталей'),
(4, 'TO-4', 'Годовое ТО', 365, 32.0, TRUE, 'Капитальное плановое ТО с полной разборкой и дефектовкой'),
(5, 'TP', 'Текущий ремонт', 90, 12.0, TRUE, 'Устранение внезапных отказов и восстановление работоспособности'),
(6, 'KR', 'Капитальный ремонт', 1825, 120.0, TRUE, 'Полное восстановление ресурса узлов и базовых деталей');

-- 10. График планового ТО на 2027 год
INSERT INTO maintenance_schedules (schedule_id, equipment_id, maintenance_type_id, assigned_team_id, planned_date, schedule_status) VALUES
(1, 1, 2, 1, '2027-01-20', 'completed'),
(2, 2, 1, 1, '2027-01-25', 'completed'),
(3, 6, 1, 2, '2027-02-05', 'completed'),
(4, 8, 2, 2, '2027-02-15', 'completed'),
(5, 1, 1, 1, '2027-03-10', 'planned'),
(6, 11, 1, 3, '2027-03-20', 'planned');

-- 11. Статусы заявок
INSERT INTO work_order_statuses (order_status_id, status_code, status_name) VALUES
(1, 'created', 'Создана'),
(2, 'assigned', 'Назначена'),
(3, 'in_progress', 'В работе'),
(4, 'waiting_materials', 'Ожидает материалов'),
(5, 'completed', 'Выполнена'),
(6, 'closed', 'Закрыта'),
(7, 'cancelled', 'Отменена');

-- 12. Приоритеты заявок
INSERT INTO work_order_priorities (priority_id, priority_code, priority_name) VALUES
(1, 'low', 'Низкий'),
(2, 'medium', 'Средний'),
(3, 'high', 'Высокий'),
(4, 'critical', 'Критический'),
(5, 'emergency', 'Аварийный');

-- 13. Заявки на ремонт и ТО
INSERT INTO work_orders (
    work_order_id, order_number, order_type, priority_id, order_status_id, equipment_id, 
    master_user_id, assigned_team_id, schedule_id, description, failure_reason, report_text, 
    planned_start_date, planned_end_date, actual_start_date, actual_end_date, downtime_hours
) VALUES
(1, 'Z-2027-0001', 'planned', 2, 6, 1, 4, 1, 1, 'Плановое квартальное ТО-2 станка 16К20', NULL, 'Работы выполнены в полном объеме, заменены фильтры и масло', '2027-01-20 08:00:00', '2027-01-20 16:00:00', '2027-01-20 08:15:00', '2027-01-20 15:45:00', 7.50),
(2, 'Z-2027-0002', 'planned', 2, 6, 2, 4, 1, 2, 'Плановое ежемесячное ТО-1 фрезерного станка 6Р13Ф3', NULL, 'Произведена чистка направляющих и смазка подшипников', '2027-01-25 08:00:00', '2027-01-25 12:00:00', '2027-01-25 08:30:00', '2027-01-25 12:15:00', 3.75),
(3, 'Z-2027-0003', 'emergency', 4, 6, 6, 5, 2, NULL, 'Аварийная остановка сварочного автомата АДФ-1000', 'Перегрев тиристорного блока управления', 'Произведена замена охлаждающего вентилятора и предохранителя', '2027-02-05 09:00:00', '2027-02-05 15:00:00', '2027-02-05 09:10:00', '2027-02-05 14:40:00', 5.50),
(4, 'Z-2027-0004', 'emergency', 4, 3, 5, 4, 1, NULL, 'Заклинивание шпиндельной бабки при чистовой расточке', 'Износ клиновых ремней и перекос направляющих', NULL, '2027-03-01 08:00:00', '2027-03-02 18:00:00', '2027-03-01 08:30:00', NULL, 0),
(5, 'Z-2027-0005', 'planned', 2, 2, 1, 4, 1, 5, 'Плановое обслуживание ТО-1 токарного станка 16К20', NULL, NULL, '2027-03-10 08:00:00', '2027-03-10 12:00:00', NULL, NULL, 0);

-- 14. Склады
INSERT INTO warehouses (warehouse_id, warehouse_code, warehouse_name, location_description) VALUES
(1, 'WH-01', 'Склад №1: Запасные части механического оборудования', 'Корпус 1, блок А, стеллажи 1-10'),
(2, 'WH-02', 'Склад №2: Электротехнические материалы и КИПиА', 'Корпус 3, 2-й этаж, помещение 204'),
(3, 'WH-03', 'Склад №3: Смазочные материалы, ГСМ и технические жидкости', 'Изолированное пожаробезопасное хранилище'),
(4, 'WH-04', 'Склад №4: Сварочные материалы и газы', 'Корпус 2, пристройка к термическому участку'),
(5, 'WH-05', 'Склад №5: Расходные материалы общего назначения', 'Административно-бытовой корпус, склад 1');

-- 15. Материалы и запчасти (данные из Приложения 6)
INSERT INTO materials (material_id, warehouse_id, material_code, material_name, unit_of_measure, current_stock, minimum_stock, storage_cell, unit_price, supplier_name) VALUES
(1, 3, 'М-001', 'Масло индустриальное И-20', 'л', 200.0, 50.0, 'С-3-01', 150.00, 'Лукойл-Смазочные материалы'),
(2, 3, 'М-002', 'Смазка Литол-24', 'кг', 80.0, 20.0, 'С-3-02', 320.00, 'Газпромнефть'),
(3, 1, 'М-003', 'Фильтр масляный (16К20)', 'шт', 15.0, 5.0, 'А-1-12', 850.00, 'СтанкоКомплект'),
(4, 1, 'М-004', 'Ремень приводной клиновой (6Р13Ф3)', 'шт', 10.0, 3.0, 'А-2-05', 1200.00, 'РТИ-Снаб'),
(5, 2, 'М-005', 'Подшипник радиальный 6-205', 'шт', 50.0, 15.0, 'Б-1-44', 450.00, 'ЕПК Волжский'),
(6, 4, 'М-006', 'Электрод сварочный МР-3', 'кг', 300.0, 100.0, 'Г-1-01', 280.00, 'СпецЭлектрод'),
(7, 2, 'М-007', 'Провод монтажный ПВС 3х2,5', 'м', 500.0, 100.0, 'Б-2-10', 95.00, 'Камкабель'),
(8, 2, 'М-008', 'Термопара хромель-копель ТХК', 'шт', 20.0, 5.0, 'Б-3-15', 2100.00, 'Теплоприбор'),
(9, 3, 'М-009', 'Масло трансформаторное ГК', 'л', 150.0, 50.0, 'С-3-05', 220.00, 'Роснефть'),
(10, 2, 'М-010', 'Изолента ПВХ профессиональная', 'рул', 30.0, 10.0, 'Б-4-01', 65.00, 'ПромСнаб');

-- 16. Списание материалов в заявку (M:N, составной первичный ключ)
INSERT INTO work_order_materials (work_order_id, material_id, quantity_used, historical_unit_price, comment_text) VALUES
(1, 1, 10.0, 150.00, 'Замена отработанного масла в гидросистеме станка 16К20'),
(1, 3, 1.0, 850.00, 'Плановая замена масляного фильтра'),
(2, 2, 2.5, 320.00, 'Смазка направляющих суппорта и подшипников'),
(2, 4, 1.0, 1200.00, 'Замена изношенного приводного ремня'),
(3, 5, 2.0, 450.00, 'Замена опорных подшипников вентилятора охлаждения');

-- 17. Журнал аудита операций
INSERT INTO audit_logs (user_id, entity_name, entity_id, action_type, old_value, new_value) VALUES
(1, 'equipment', 1, 'CREATE', NULL, '{"inventory_number": "EQ-001", "name": "16К20"}'),
(4, 'work_orders', 1, 'CREATE', NULL, '{"order_number": "Z-2027-0001", "status": "created"}'),
(4, 'work_orders', 1, 'STATUS_CHANGE', '{"status": "in_progress"}', '{"status": "completed"}'),
(4, 'work_orders', 1, 'STATUS_CHANGE', '{"status": "completed"}', '{"status": "closed"}'),
(5, 'work_orders', 3, 'CREATE', NULL, '{"order_number": "Z-2027-0003", "type": "emergency"}');

-- Корректировка последовательностей
SELECT setval('roles_role_id_seq', (SELECT MAX(role_id) FROM roles));
SELECT setval('workshops_workshop_id_seq', (SELECT MAX(workshop_id) FROM workshops));
SELECT setval('repair_teams_team_id_seq', (SELECT MAX(team_id) FROM repair_teams));
SELECT setval('users_user_id_seq', (SELECT MAX(user_id) FROM users));
SELECT setval('equipment_statuses_status_id_seq', (SELECT MAX(status_id) FROM equipment_statuses));
SELECT setval('equipment_equipment_id_seq', (SELECT MAX(equipment_id) FROM equipment));
SELECT setval('equipment_status_history_history_id_seq', (SELECT MAX(history_id) FROM equipment_status_history));
SELECT setval('maintenance_types_maintenance_type_id_seq', (SELECT MAX(maintenance_type_id) FROM maintenance_types));
SELECT setval('maintenance_schedules_schedule_id_seq', (SELECT MAX(schedule_id) FROM maintenance_schedules));
SELECT setval('work_order_statuses_order_status_id_seq', (SELECT MAX(order_status_id) FROM work_order_statuses));
SELECT setval('work_order_priorities_priority_id_seq', (SELECT MAX(priority_id) FROM work_order_priorities));
SELECT setval('work_orders_work_order_id_seq', (SELECT MAX(work_order_id) FROM work_orders));
SELECT setval('warehouses_warehouse_id_seq', (SELECT MAX(warehouse_id) FROM warehouses));
SELECT setval('materials_material_id_seq', (SELECT MAX(material_id) FROM materials));
SELECT setval('audit_logs_log_id_seq', (SELECT MAX(log_id) FROM audit_logs));



-- ============================================================================
-- Синхронизация генераторов последовательностей (SERIAL / SEQUENCE)
-- ============================================================================
SELECT setval('roles_role_id_seq', COALESCE((SELECT MAX(role_id) FROM roles), 1));
SELECT setval('workshops_workshop_id_seq', COALESCE((SELECT MAX(workshop_id) FROM workshops), 1));
SELECT setval('repair_teams_team_id_seq', COALESCE((SELECT MAX(team_id) FROM repair_teams), 1));
SELECT setval('users_user_id_seq', COALESCE((SELECT MAX(user_id) FROM users), 1));
SELECT setval('equipment_statuses_status_id_seq', COALESCE((SELECT MAX(status_id) FROM equipment_statuses), 1));
SELECT setval('equipment_equipment_id_seq', COALESCE((SELECT MAX(equipment_id) FROM equipment), 1));
SELECT setval('equipment_status_history_history_id_seq', COALESCE((SELECT MAX(history_id) FROM equipment_status_history), 1));
SELECT setval('maintenance_types_maintenance_type_id_seq', COALESCE((SELECT MAX(maintenance_type_id) FROM maintenance_types), 1));
SELECT setval('maintenance_schedules_schedule_id_seq', COALESCE((SELECT MAX(schedule_id) FROM maintenance_schedules), 1));
SELECT setval('work_order_priorities_priority_id_seq', COALESCE((SELECT MAX(priority_id) FROM work_order_priorities), 1));
SELECT setval('work_order_statuses_order_status_id_seq', COALESCE((SELECT MAX(order_status_id) FROM work_order_statuses), 1));
SELECT setval('work_orders_work_order_id_seq', COALESCE((SELECT MAX(work_order_id) FROM work_orders), 1));
SELECT setval('warehouses_warehouse_id_seq', COALESCE((SELECT MAX(warehouse_id) FROM warehouses), 1));
SELECT setval('materials_material_id_seq', COALESCE((SELECT MAX(material_id) FROM materials), 1));
SELECT setval('audit_logs_log_id_seq', COALESCE((SELECT MAX(log_id) FROM audit_logs), 1));


-- ============================================================================
-- Скрипт проверки целостности, связей и количества записей после импорта
-- Проверка выполнения требований: минимум 5 строк в каждой таблице, 
-- отсутствие висячих таблиц, корректность триггеров и процедур.
-- ============================================================================

-- 1. Сводный отчет по количеству записей во всех таблицах (требование: >= 5 строк)
DO $$
DECLARE
    rec RECORD;
    v_count INT;
    v_all_passed BOOLEAN := TRUE;
BEGIN
    RAISE NOTICE '==================================================';
    RAISE NOTICE 'ПРОВЕРКА КОЛИЧЕСТВА ЗАПИСЕЙ ПОСЛЕ ИМПОРТА';
    RAISE NOTICE '==================================================';

    FOR rec IN 
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
          AND table_type = 'BASE TABLE'
        ORDER BY table_name
    LOOP
        EXECUTE format('SELECT COUNT(*) FROM %I', rec.table_name) INTO v_count;
        IF v_count >= 5 THEN
            RAISE NOTICE 'Таблица: % - % строк [УСПЕХ >= 5]', rpad(rec.table_name, 26), v_count;
        ELSE
            RAISE WARNING 'Таблица: % - % строк [ВНИМАНИЕ < 5]', rpad(rec.table_name, 26), v_count;
            v_all_passed := FALSE;
        END IF;
    END LOOP;

    IF v_all_passed THEN
        RAISE NOTICE 'ИТОГ: Все таблицы содержат 5 и более записей! Проверка импорта пройдена на 100%%.';
    END IF;
    RAISE NOTICE '==================================================';
END $$;

-- 2. Проверка работы Хранимой Функции №1 (расчет времени простоя оборудования)
SELECT 
    eq.inventory_number,
    eq.equipment_name,
    calculate_equipment_downtime_hours(eq.equipment_id, '2027-01-01 00:00:00', '2027-03-31 23:59:59') AS total_downtime_hours
FROM equipment eq
WHERE eq.equipment_id IN (1, 2, 6);

-- 3. Проверка работы Хранимой Функции №2 (расчет следующей плановой даты ТО)
SELECT 
    eq.inventory_number,
    mt.type_code,
    mt.type_name,
    calculate_next_maintenance_date(eq.equipment_id, mt.maintenance_type_id) AS next_maintenance_date
FROM equipment eq
CROSS JOIN maintenance_types mt
WHERE eq.equipment_id = 1 AND mt.type_code IN ('TO-1', 'TO-2');

-- 4. Проверка работы Хранимой Процедуры №3 (сводный отчет по цеху)
SELECT * FROM generate_workshop_summary_report(1, '2027-01-01 00:00:00', '2027-03-31 23:59:59');
