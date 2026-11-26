-- ============================================================================
-- АИС «ТОиР-Проф»: Единый SQL-скрипт БД (PostgreSQL, строгая 3НФ, 22 таблицы)
-- Схема + Ограничения + Триггеры и Функции + Тестовые данные + Проверка
-- ============================================================================

DROP TABLE IF EXISTS audit_logs, work_order_materials, warehouse_materials, materials,
    suppliers, warehouses, work_orders, work_order_statuses, work_order_priorities,
    maintenance_schedules, maintenance_types, equipment_status_history, equipment,
    equipment_models, manufacturers, equipment_statuses, team_members, team_workshops,
    repair_teams, users, workshops, roles CASCADE;

CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL UNIQUE,
    role_name VARCHAR(100) NOT NULL,
    role_description TEXT
);

CREATE TABLE workshops (
    workshop_id SERIAL PRIMARY KEY,
    workshop_code VARCHAR(50) NOT NULL UNIQUE,
    workshop_name VARCHAR(150) NOT NULL,
    workshop_description TEXT
);

CREATE TABLE repair_teams (
    team_id SERIAL PRIMARY KEY,
    team_code VARCHAR(50) NOT NULL UNIQUE,
    team_name VARCHAR(150) NOT NULL,
    specialization VARCHAR(100) NOT NULL
);

CREATE TABLE team_workshops (
    team_id INT NOT NULL REFERENCES repair_teams(team_id) ON DELETE CASCADE,
    workshop_id INT NOT NULL REFERENCES workshops(workshop_id) ON DELETE CASCADE,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (team_id, workshop_id)
);

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
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE team_members (
    team_id INT NOT NULL REFERENCES repair_teams(team_id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    is_team_leader BOOLEAN NOT NULL DEFAULT FALSE,
    joined_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (team_id, user_id)
);

CREATE TABLE equipment_statuses (
    status_id SERIAL PRIMARY KEY,
    status_code VARCHAR(50) NOT NULL UNIQUE,
    status_name VARCHAR(100) NOT NULL,
    description TEXT
);

CREATE TABLE manufacturers (
    manufacturer_id SERIAL PRIMARY KEY,
    manufacturer_name VARCHAR(150) NOT NULL UNIQUE,
    country VARCHAR(100) DEFAULT 'Россия'
);

CREATE TABLE equipment_models (
    model_id SERIAL PRIMARY KEY,
    manufacturer_id INT NOT NULL REFERENCES manufacturers(manufacturer_id) ON DELETE RESTRICT,
    model_name VARCHAR(100) NOT NULL,
    equipment_category VARCHAR(100),
    UNIQUE (manufacturer_id, model_name)
);

CREATE TABLE equipment (
    equipment_id SERIAL PRIMARY KEY,
    workshop_id INT NOT NULL REFERENCES workshops(workshop_id) ON DELETE RESTRICT,
    status_id INT NOT NULL REFERENCES equipment_statuses(status_id) ON DELETE RESTRICT,
    model_id INT NOT NULL REFERENCES equipment_models(model_id) ON DELETE RESTRICT,
    inventory_number VARCHAR(100) NOT NULL UNIQUE,
    equipment_name VARCHAR(200) NOT NULL,
    serial_number VARCHAR(100),
    warranty_until DATE,
    passport_url VARCHAR(255),
    commissioning_date DATE NOT NULL,
    operation_date DATE NOT NULL,
    decommissioning_date DATE,
    created_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    updated_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (commissioning_date <= operation_date AND operation_date <= CURRENT_DATE),
    CHECK (decommissioning_date IS NULL OR decommissioning_date > commissioning_date)
);

CREATE TABLE equipment_status_history (
    history_id SERIAL PRIMARY KEY,
    equipment_id INT NOT NULL REFERENCES equipment(equipment_id) ON DELETE CASCADE,
    status_id INT NOT NULL REFERENCES equipment_statuses(status_id) ON DELETE RESTRICT,
    reason TEXT,
    changed_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    changed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE maintenance_types (
    maintenance_type_id SERIAL PRIMARY KEY,
    type_code VARCHAR(50) NOT NULL UNIQUE,
    type_name VARCHAR(100) NOT NULL,
    periodicity_days INT NOT NULL CHECK (periodicity_days > 0),
    standard_duration_hours NUMERIC(6,2) NOT NULL CHECK (standard_duration_hours > 0),
    requires_stop BOOLEAN NOT NULL DEFAULT TRUE,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE maintenance_schedules (
    schedule_id SERIAL PRIMARY KEY,
    equipment_id INT NOT NULL REFERENCES equipment(equipment_id) ON DELETE RESTRICT,
    maintenance_type_id INT NOT NULL REFERENCES maintenance_types(maintenance_type_id) ON DELETE RESTRICT,
    assigned_team_id INT REFERENCES repair_teams(team_id) ON DELETE SET NULL,
    planned_date DATE NOT NULL,
    schedule_status VARCHAR(50) NOT NULL DEFAULT 'planned'
        CHECK (schedule_status IN ('planned', 'assigned', 'in_progress', 'completed', 'overdue', 'cancelled')),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE work_order_statuses (
    order_status_id SERIAL PRIMARY KEY,
    status_code VARCHAR(50) NOT NULL UNIQUE,
    status_name VARCHAR(100) NOT NULL
);

CREATE TABLE work_order_priorities (
    priority_id SERIAL PRIMARY KEY,
    priority_code VARCHAR(50) NOT NULL UNIQUE,
    priority_name VARCHAR(100) NOT NULL
);

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
    planned_start_date TIMESTAMP NOT NULL,
    planned_end_date TIMESTAMP NOT NULL,
    actual_start_date TIMESTAMP,
    actual_end_date TIMESTAMP,
    downtime_hours NUMERIC(8,2) DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (planned_start_date < planned_end_date),
    CHECK (actual_start_date IS NULL OR actual_end_date IS NULL OR actual_start_date < actual_end_date)
);

CREATE TABLE warehouses (
    warehouse_id SERIAL PRIMARY KEY,
    warehouse_code VARCHAR(50) NOT NULL UNIQUE,
    warehouse_name VARCHAR(150) NOT NULL,
    location_description TEXT
);

CREATE TABLE suppliers (
    supplier_id SERIAL PRIMARY KEY,
    supplier_code VARCHAR(50) NOT NULL UNIQUE,
    supplier_name VARCHAR(150) NOT NULL UNIQUE,
    inn VARCHAR(20),
    contact_phone VARCHAR(50),
    contact_email VARCHAR(150)
);

CREATE TABLE materials (
    material_id SERIAL PRIMARY KEY,
    supplier_id INT REFERENCES suppliers(supplier_id) ON DELETE SET NULL,
    material_code VARCHAR(50) NOT NULL UNIQUE,
    material_name VARCHAR(200) NOT NULL,
    unit_of_measure VARCHAR(20) NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE warehouse_materials (
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id) ON DELETE RESTRICT,
    material_id INT NOT NULL REFERENCES materials(material_id) ON DELETE CASCADE,
    current_stock NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (current_stock >= 0),
    minimum_stock NUMERIC(10,2) NOT NULL DEFAULT 0 CHECK (minimum_stock >= 0),
    storage_cell VARCHAR(50),
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (warehouse_id, material_id)
);

CREATE TABLE work_order_materials (
    work_order_id INT NOT NULL REFERENCES work_orders(work_order_id) ON DELETE CASCADE,
    warehouse_id INT NOT NULL,
    material_id INT NOT NULL,
    quantity_used NUMERIC(10,2) NOT NULL CHECK (quantity_used > 0),
    historical_unit_price NUMERIC(12,2) NOT NULL CHECK (historical_unit_price >= 0),
    used_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    comment_text TEXT,
    PRIMARY KEY (work_order_id, warehouse_id, material_id),
    FOREIGN KEY (warehouse_id, material_id) REFERENCES warehouse_materials(warehouse_id, material_id) ON DELETE RESTRICT
);

CREATE TABLE audit_logs (
    log_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    old_value JSONB,
    new_value JSONB,
    logged_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- 2. ТРИГГЕРЫ И ХРАНИМЫЕ ФУНКЦИИ
-- ============================================================================

-- Триггер №1: Запрет смены статуса оборудования с «выведено из эксплуатации»
CREATE OR REPLACE FUNCTION trg_prevent_decommissioned_status_change()
RETURNS TRIGGER AS $$
DECLARE v_decom_id INT;
BEGIN
    SELECT status_id INTO v_decom_id FROM equipment_statuses WHERE status_code = 'decommissioned';
    IF OLD.status_id = v_decom_id AND NEW.status_id <> v_decom_id THEN
        RAISE EXCEPTION 'Запрещено менять статус списанного оборудования (%)', OLD.inventory_number;
    END IF;
    NEW.updated_at := CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_equipment_status_update
BEFORE UPDATE ON equipment FOR EACH ROW
EXECUTE FUNCTION trg_prevent_decommissioned_status_change();

-- Триггер №2: Проверка дат заявок (вкл. created_at не из будущего) + расчёт простоя
CREATE OR REPLACE FUNCTION trg_validate_work_order_dates_and_status()
RETURNS TRIGGER AS $$
DECLARE v_cancel_id INT;
BEGIN
    IF NEW.created_at > CURRENT_TIMESTAMP + INTERVAL '1 minute' THEN
        RAISE EXCEPTION 'Дата создания заявки не может быть в будущем';
    END IF;
    IF NEW.planned_start_date < NEW.created_at OR NEW.planned_end_date <= NEW.planned_start_date THEN
        RAISE EXCEPTION 'Некорректные плановые даты заявки %', NEW.order_number;
    END IF;
    IF NEW.actual_start_date IS NOT NULL AND NEW.actual_start_date < NEW.created_at THEN
        RAISE EXCEPTION 'Фактическая дата начала не может быть раньше создания заявки';
    END IF;
    IF NEW.actual_start_date IS NOT NULL AND NEW.actual_end_date IS NOT NULL THEN
        IF NEW.actual_end_date <= NEW.actual_start_date THEN
            RAISE EXCEPTION 'Фактическая дата окончания должна быть позже начала';
        END IF;
        NEW.downtime_hours := ROUND(EXTRACT(EPOCH FROM (NEW.actual_end_date - NEW.actual_start_date)) / 3600.0, 2);
    END IF;
    SELECT order_status_id INTO v_cancel_id FROM work_order_statuses WHERE status_code = 'cancelled';
    IF NEW.order_status_id = v_cancel_id AND COALESCE(OLD.actual_start_date, NEW.actual_start_date) IS NOT NULL THEN
        RAISE EXCEPTION 'Нельзя отменить уже начатую заявку %', NEW.order_number;
    END IF;
    NEW.updated_at := CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_work_orders_validate
BEFORE INSERT OR UPDATE ON work_orders FOR EACH ROW
EXECUTE FUNCTION trg_validate_work_order_dates_and_status();

-- Функция №1: Расчёт времени простоя оборудования за период
CREATE OR REPLACE FUNCTION calculate_equipment_downtime_hours(
    p_equipment_id INT, p_start_date TIMESTAMP, p_end_date TIMESTAMP
) RETURNS NUMERIC AS $$
BEGIN
    IF p_start_date >= p_end_date THEN
        RAISE EXCEPTION 'Дата начала периода должна быть раньше даты окончания';
    END IF;
    RETURN COALESCE((
        SELECT SUM(ROUND(EXTRACT(EPOCH FROM (
            LEAST(COALESCE(actual_end_date, CURRENT_TIMESTAMP), p_end_date) -
            GREATEST(actual_start_date, p_start_date))) / 3600.0, 2))
        FROM work_orders
        WHERE equipment_id = p_equipment_id AND actual_start_date IS NOT NULL
          AND actual_start_date < p_end_date AND COALESCE(actual_end_date, CURRENT_TIMESTAMP) > p_start_date
    ), 0);
END;
$$ LANGUAGE plpgsql;

-- Функция №2: Расчёт следующей плановой даты ТО
CREATE OR REPLACE FUNCTION calculate_next_maintenance_date(p_equipment_id INT, p_maintenance_type_id INT)
RETURNS DATE AS $$
DECLARE v_days INT; v_base DATE;
BEGIN
    SELECT periodicity_days INTO v_days FROM maintenance_types WHERE maintenance_type_id = p_maintenance_type_id;
    SELECT MAX(COALESCE(wo.actual_end_date::DATE, ms.planned_date)) INTO v_base
    FROM maintenance_schedules ms LEFT JOIN work_orders wo ON wo.schedule_id = ms.schedule_id
    WHERE ms.equipment_id = p_equipment_id AND ms.maintenance_type_id = p_maintenance_type_id AND ms.schedule_status = 'completed';
    IF v_base IS NULL THEN
        SELECT commissioning_date INTO v_base FROM equipment WHERE equipment_id = p_equipment_id;
    END IF;
    RETURN v_base + (v_days || ' days')::INTERVAL;
END;
$$ LANGUAGE plpgsql;

-- Функция №3: Сводный отчёт по цеху за период
DROP FUNCTION IF EXISTS generate_workshop_summary_report(INT, TIMESTAMP, TIMESTAMP);
CREATE OR REPLACE FUNCTION generate_workshop_summary_report(
    p_workshop_id INT, p_start_date TIMESTAMP, p_end_date TIMESTAMP
) RETURNS TABLE (
    workshop_name VARCHAR, total_equipment BIGINT, under_repair_count BIGINT,
    total_orders BIGINT, completed_orders BIGINT, total_downtime_hours NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT w.workshop_name,
        (SELECT COUNT(*) FROM equipment e WHERE e.workshop_id = w.workshop_id),
        (SELECT COUNT(*) FROM equipment e JOIN equipment_statuses s ON s.status_id = e.status_id
         WHERE e.workshop_id = w.workshop_id AND s.status_code = 'under_repair'),
        (SELECT COUNT(*) FROM work_orders wo JOIN equipment e ON e.equipment_id = wo.equipment_id
         WHERE e.workshop_id = w.workshop_id AND wo.created_at BETWEEN p_start_date AND p_end_date),
        (SELECT COUNT(*) FROM work_orders wo JOIN equipment e ON e.equipment_id = wo.equipment_id
         JOIN work_order_statuses ws ON ws.order_status_id = wo.order_status_id
         WHERE e.workshop_id = w.workshop_id AND ws.status_code IN ('completed','closed')
           AND wo.created_at BETWEEN p_start_date AND p_end_date),
        COALESCE((SELECT SUM(wo.downtime_hours) FROM work_orders wo JOIN equipment e ON e.equipment_id = wo.equipment_id
                  WHERE e.workshop_id = w.workshop_id AND wo.created_at BETWEEN p_start_date AND p_end_date), 0)
    FROM workshops w WHERE w.workshop_id = p_workshop_id;
END;
$$ LANGUAGE plpgsql;
-- ============================================================================
-- 3. ТЕСТОВЫЕ ДАННЫЕ (>= 5 записей в каждой из 22 таблиц)
-- ============================================================================
INSERT INTO roles (role_id, role_code, role_name, role_description) VALUES
(1, 'admin', 'Администратор системы', 'Полный доступ'),
(2, 'chief_engineer', 'Главный инженер', 'Аналитика и контроль ТО'),
(3, 'maintenance_engineer', 'Инженер по ТОиР', 'Графики ТО и нормативы'),
(4, 'workshop_master', 'Мастер цеха', 'Заявки и оборудование цеха'),
(5, 'team_member', 'Бригадир / Исполнитель', 'Выполнение ремонтов и списание');

INSERT INTO workshops (workshop_id, workshop_code, workshop_name, workshop_description) VALUES
(1, 'MC-01', 'Цех №1: Механосборочный (МСЦ)', 'Механическая обработка деталей'),
(2, 'ST-02', 'Цех №2: Сварочно-термический (СТЦ)', 'Сварка и термообработка'),
(3, 'ET-03', 'Цех №3: Электротехнический (ЭТЦ)', 'Электрооборудование и КИПиА'),
(4, 'RMS', 'Ремонтно-механическая служба (РМС)', 'Центральная служба ТОиР'),
(5, 'WH-MAIN', 'Центральное складское хозяйство', 'Хранение запчастей и ГСМ');

INSERT INTO repair_teams (team_id, team_code, team_name, specialization) VALUES
(1, 'BR-01', 'Бригада №1 (Механики-ремонтники)', 'Станки 16К20, 6Р13Ф3'),
(2, 'BR-02', 'Бригада №2 (Электрики-сварщики)', 'Сварочное и термическое оборудование'),
(3, 'BR-03', 'Бригада №3 (Электромонтажники)', 'Трансформаторы и электроприводы'),
(4, 'BR-04', 'Бригада №4 (Наладчики КИПиА)', 'Датчики и частотные преобразователи'),
(5, 'BR-05', 'Бригада №5 (Слесари-диагносты)', 'Вибродиагностика (мобильная)');

INSERT INTO team_workshops (team_id, workshop_id, is_primary) VALUES
(1, 1, TRUE), (1, 2, FALSE), (2, 2, TRUE), (3, 3, TRUE), (4, 3, TRUE),
(4, 1, FALSE), (5, 1, FALSE), (5, 2, FALSE), (5, 3, FALSE), (5, 4, TRUE);

-- Пароль всех демо-пользователей: password123 (SHA-256)
INSERT INTO users (user_id, role_id, workshop_id, email, phone, first_name, last_name, patronymic, password_hash) VALUES
(1, 1, 4, 'admin@pmz.ru', '+7-800-000-0001', 'Алексей', 'Администраторов', 'Игоревич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'),
(2, 2, 4, 'egorov.aa@pmz.ru', '+7-800-000-0002', 'Анатолий', 'Егоров', 'Алексеевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'),
(3, 3, 4, 'belov.ia@pmz.ru', '+7-800-000-0003', 'Игорь', 'Белов', 'Анатольевич', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'),
(4, 4, 1, 'ivanov.ii@pmz.ru', '+7-800-000-0004', 'Иван', 'Иванов', 'Иванович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'),
(5, 4, 2, 'petrov.pp@pmz.ru', '+7-800-000-0005', 'Петр', 'Петров', 'Петрович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'),
(6, 5, 1, 'sokolov.vm@pmz.ru', '+7-800-000-0007', 'Владимир', 'Соколов', 'Михайлович', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f');

INSERT INTO team_members (team_id, user_id, is_team_leader) VALUES
(1, 6, TRUE), (1, 4, FALSE), (2, 5, TRUE), (3, 3, TRUE), (4, 2, TRUE);

INSERT INTO equipment_statuses (status_id, status_code, status_name) VALUES
(1, 'in_operation', 'В работе'),
(2, 'under_repair', 'В ремонте'),
(3, 'in_downtime', 'В простое'),
(4, 'decommissioned', 'Выведено из эксплуатации'),
(5, 'in_standby', 'В резерве');

INSERT INTO manufacturers (manufacturer_id, manufacturer_name, country) VALUES
(1, 'Красный Пролетарий', 'Россия'), (2, 'ГЗФС', 'Россия'),
(3, 'Ивановский станкозавод', 'Россия'), (4, 'ЗАО «Электрик»', 'Россия'),
(5, 'МЭТЗ им. В.И. Козлова', 'Беларусь'), (6, 'ВЭМЗ', 'Россия');

INSERT INTO equipment_models (model_id, manufacturer_id, model_name, equipment_category) VALUES
(1, 1, '16К20', 'Токарные станки'), (2, 2, '6Р13Ф3', 'Фрезерные станки'),
(3, 3, '2М614', 'Расточные станки'), (4, 4, 'АДФ-1000', 'Сварочное оборудование'),
(5, 5, 'ТМ-400/10', 'Силовые трансформаторы'), (6, 6, '4А250М4', 'Электродвигатели');

INSERT INTO equipment (equipment_id, workshop_id, status_id, model_id, inventory_number, equipment_name, serial_number, commissioning_date, operation_date, created_by_user_id) VALUES
(1, 1, 1, 1, 'EQ-001', 'Токарно-винторезный станок', 'SN-16K20-8472', '2018-03-15', '2018-03-20', 1),
(2, 1, 1, 2, 'EQ-002', 'Вертикально-фрезерный станок', 'SN-6R13-1094', '2019-07-20', '2019-07-25', 1),
(3, 1, 2, 3, 'EQ-003', 'Горизонтально-расточной станок', 'SN-2M614-551', '2017-11-10', '2017-11-15', 1),
(4, 2, 1, 4, 'EQ-004', 'Сварочный автомат', 'SN-ADF-1000-88', '2019-02-02', '2019-02-05', 1),
(5, 3, 1, 5, 'EQ-005', 'Трансформатор силовой', 'SN-TM400-09', '2016-01-15', '2016-01-20', 1),
(6, 3, 3, 6, 'EQ-006', 'Электродвигатель асинхронный', 'SN-4A250-101', '2020-03-10', '2020-03-15', 1);

INSERT INTO equipment_status_history (equipment_id, status_id, reason, changed_by_user_id) VALUES
(1, 1, 'Ввод в промышленную эксплуатацию', 3),
(2, 1, 'Завершено плановое ТО-1', 4),
(3, 2, 'Люфт шпинделя — остановлен на ремонт', 4),
(4, 1, 'Возвращён в работу после замены ролика', 5),
(6, 3, 'Временный простой в ожидании подшипника', 3);

INSERT INTO maintenance_types (maintenance_type_id, type_code, type_name, periodicity_days, standard_duration_hours) VALUES
(1, 'TO-1', 'Ежемесячное ТО', 30, 4.0),
(2, 'TO-2', 'Квартальное ТО', 90, 8.0),
(3, 'TO-3', 'Полугодовое ТО', 180, 16.0),
(4, 'TO-4', 'Годовое ТО', 365, 32.0),
(5, 'TP', 'Текущий ремонт', 90, 12.0),
(6, 'KR', 'Капитальный ремонт', 1825, 120.0);

INSERT INTO maintenance_schedules (schedule_id, equipment_id, maintenance_type_id, assigned_team_id, planned_date, schedule_status) VALUES
(1, 1, 2, 1, '2026-10-20', 'completed'),
(2, 2, 1, 1, '2026-10-25', 'completed'),
(3, 4, 1, 2, '2026-11-05', 'completed'),
(4, 5, 2, 3, '2026-11-15', 'completed'),
(5, 1, 1, 1, CURRENT_DATE + 2, 'planned'),
(6, 6, 1, 3, CURRENT_DATE - 3, 'planned');

INSERT INTO work_order_statuses (order_status_id, status_code, status_name) VALUES
(1, 'created', 'Создана'), (2, 'assigned', 'Назначена'), (3, 'in_progress', 'В работе'),
(4, 'waiting_materials', 'Ожидает материалов'), (5, 'completed', 'Выполнена'),
(6, 'closed', 'Закрыта'), (7, 'cancelled', 'Отменена');

INSERT INTO work_order_priorities (priority_id, priority_code, priority_name) VALUES
(1, 'low', 'Низкий'), (2, 'medium', 'Средний'), (3, 'high', 'Высокий'),
(4, 'critical', 'Критический'), (5, 'emergency', 'Аварийный');

INSERT INTO work_orders (
    work_order_id, order_number, order_type, priority_id, order_status_id, equipment_id,
    master_user_id, assigned_team_id, schedule_id, description, failure_reason, report_text,
    planned_start_date, planned_end_date, actual_start_date, actual_end_date, downtime_hours, created_at
) VALUES
(1, 'Z-2026-0001', 'planned', 2, 6, 1, 4, 1, 1, 'Плановое ТО-2 станка 16К20', NULL, 'Заменены фильтры и масло', '2026-10-20 08:00', '2026-10-20 16:00', '2026-10-20 08:15', '2026-10-20 15:45', 7.50, '2026-10-19 10:00'),
(2, 'Z-2026-0002', 'planned', 2, 6, 2, 4, 1, 2, 'Плановое ТО-1 фрезерного станка', NULL, 'Смазка направляющих и подшипников', '2026-10-25 08:00', '2026-10-25 12:00', '2026-10-25 08:30', '2026-10-25 12:15', 3.75, '2026-10-24 09:00'),
(3, 'Z-2026-0003', 'emergency', 4, 6, 4, 5, 2, NULL, 'Аварийная остановка автомата АДФ-1000', 'Перегрев тиристорного блока', 'Замена вентилятора и предохранителя', '2026-11-05 09:00', '2026-11-05 15:00', '2026-11-05 09:10', '2026-11-05 14:40', 5.50, '2026-11-05 08:30'),
(4, 'Z-2026-0004', 'emergency', 4, 3, 3, 4, 1, NULL, 'Заклинивание шпиндельной бабки', 'Износ клиновых ремней', NULL, '2026-11-26 08:00', '2026-11-27 18:00', '2026-11-26 08:30', NULL, 0, '2026-11-26 07:30'),
(5, 'Z-2026-0005', 'planned', 2, 2, 1, 4, 1, 5, 'Плановое ТО-1 токарного станка', NULL, NULL, '2026-11-29 08:00', '2026-11-29 12:00', NULL, NULL, 0, '2026-11-25 10:00');

INSERT INTO warehouses (warehouse_id, warehouse_code, warehouse_name, location_description) VALUES
(1, 'WH-01', 'Склад №1: Механика и подшипники', 'Корпус 1, блок А'),
(2, 'WH-02', 'Склад №2: Электротехника и КИПиА', 'Корпус 3, 2-й этаж'),
(3, 'WH-03', 'Склад №3: Смазочные материалы и ГСМ', 'Хранилище ГСМ'),
(4, 'WH-04', 'Склад №4: Сварочные материалы', 'Корпус 2, пристройка'),
(5, 'WH-05', 'Склад №5: Общезаводские расходники', 'АБК, склад 1');

INSERT INTO suppliers (supplier_id, supplier_code, supplier_name, inn, contact_phone, contact_email) VALUES
(1, 'SUP-01', 'Лукойл-Смазочные материалы', '7708004767', '+7-495-981-71-00', 'sales@lukoil-masla.ru'),
(2, 'SUP-02', 'СтанкоКомплект', '5260123456', '+7-831-412-00-15', 'order@stankokomplekt.ru'),
(3, 'SUP-03', 'РТИ-Снаб', '5258987654', '+7-831-245-30-40', 'rti@rtisnab.ru'),
(4, 'SUP-04', 'ЕПК Волжский', '3435012345', '+7-8443-55-12-00', 'epk@epkgroup.ru'),
(5, 'SUP-05', 'СпецЭлектрод', '7720011223', '+7-495-306-44-00', 'zakaz@spetselectrode.ru');

INSERT INTO materials (material_id, supplier_id, material_code, material_name, unit_of_measure, unit_price) VALUES
(1, 1, 'М-001', 'Масло индустриальное И-20', 'л', 150.00),
(2, 1, 'М-002', 'Смазка Литол-24', 'кг', 320.00),
(3, 2, 'М-003', 'Фильтр масляный (16К20)', 'шт', 850.00),
(4, 3, 'М-004', 'Ремень приводной клиновой', 'шт', 1200.00),
(5, 4, 'М-005', 'Подшипник радиальный 6-205', 'шт', 450.00),
(6, 5, 'М-006', 'Электрод сварочный МР-3', 'кг', 280.00);

INSERT INTO warehouse_materials (warehouse_id, material_id, current_stock, minimum_stock, storage_cell) VALUES
(3, 1, 200.0, 50.0, 'С-3-01'), (1, 1, 8.0, 10.0, 'А-1-02'),
(3, 2, 80.0, 20.0, 'С-3-02'), (1, 3, 15.0, 5.0, 'А-1-12'),
(1, 4, 10.0, 3.0, 'А-2-05'), (2, 5, 50.0, 15.0, 'Б-1-44'),
(4, 6, 300.0, 100.0, 'Г-1-01');

INSERT INTO work_order_materials (work_order_id, warehouse_id, material_id, quantity_used, historical_unit_price, comment_text) VALUES
(1, 3, 1, 10.0, 150.00, 'Замена масла в гидросистеме'),
(1, 1, 3, 1.0, 850.00, 'Плановая замена фильтра'),
(2, 3, 2, 2.5, 320.00, 'Смазка направляющих'),
(2, 1, 4, 1.0, 1200.00, 'Замена приводного ремня'),
(3, 2, 5, 2.0, 450.00, 'Замена подшипников вентилятора');

INSERT INTO audit_logs (user_id, entity_name, entity_id, action_type, new_value) VALUES
(1, 'equipment', 1, 'CREATE', '{"inventory_number":"EQ-001"}'),
(4, 'work_orders', 1, 'CREATE', '{"order_number":"Z-2026-0001"}'),
(4, 'work_orders', 1, 'STATUS_CHANGE', '{"status":"completed"}'),
(4, 'work_orders', 1, 'STATUS_CHANGE', '{"status":"closed"}'),
(5, 'work_orders', 3, 'CREATE', '{"order_number":"Z-2026-0003"}');

-- Автосинхронизация всех SERIAL-последовательностей и проверка >= 5 строк в 22 таблицах
DO $$
DECLARE r RECORD; v_cnt BIGINT;
BEGIN
    FOR r IN
        SELECT c.relname AS tbl, a.attname AS col, pg_get_serial_sequence(c.relname, a.attname) AS seq
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
        JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
        WHERE c.relkind = 'r' AND pg_get_serial_sequence(c.relname, a.attname) IS NOT NULL
    LOOP
        EXECUTE format('SELECT setval(%L, COALESCE((SELECT MAX(%I) FROM %I), 1))', r.seq, r.col, r.tbl);
    END LOOP;
    FOR r IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename LOOP
        EXECUTE format('SELECT COUNT(*) FROM %I', r.tablename) INTO v_cnt;
        IF v_cnt < 5 THEN
            RAISE EXCEPTION 'Таблица % содержит меньше 5 записей (%)', r.tablename, v_cnt;
        END IF;
        RAISE NOTICE '[OK] %: % строк', r.tablename, v_cnt;
    END LOOP;
END $$;


