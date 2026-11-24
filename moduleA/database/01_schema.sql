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
