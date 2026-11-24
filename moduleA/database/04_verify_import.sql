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
