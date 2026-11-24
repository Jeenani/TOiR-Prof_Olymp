import os

def create_full_setup():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    f1 = os.path.join(base_dir, "01_schema.sql")
    f2 = os.path.join(base_dir, "02_triggers_procedures.sql")
    f3 = os.path.join(base_dir, "03_seed_data.sql")
    f4 = os.path.join(base_dir, "04_verify_import.sql")
    out_file = os.path.join(base_dir, "full_database_setup.sql")

    with open(f1, "r", encoding="utf-8") as f:
        s1 = f.read()
    with open(f2, "r", encoding="utf-8") as f:
        s2 = f.read()
    with open(f3, "r", encoding="utf-8") as f:
        s3 = f.read()
    with open(f4, "r", encoding="utf-8") as f:
        s4 = f.read()

    seq_sync_sql = """
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
"""

    header = """-- ============================================================================
-- ЕДИНЫЙ СКРИПТ РАЗВЕРТЫВАНИЯ БАЗЫ ДАННЫХ «ТОиР-Проф»
-- Включает:
--  1. Структура таблиц (3НФ, PK, FK, CHECK, UNIQUE)
--  2. Бизнес-логика СУБД (Триггеры аудита, авто-история, хранимые функции и процедуры)
--  3. Первичные данные (>= 5 строк в каждой таблице для 100% зачета импорта)
--  4. Синхронизация последовательностей (SEQUENCE)
--  5. Валидация и самопроверка целостности
-- ============================================================================

"""
    combined = header + s1 + "\n\n" + s2 + "\n\n" + s3 + "\n\n" + seq_sync_sql + "\n\n" + s4

    with open(out_file, "w", encoding="utf-8") as f:
        f.write(combined)
    print(f"Created {out_file} ({len(combined)} chars)")

if __name__ == "__main__":
    create_full_setup()

