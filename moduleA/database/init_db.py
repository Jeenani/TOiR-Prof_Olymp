import psycopg2
from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT
import sys

def setup_database():
    print("=== Инициализация Базы Данных ТОиР-Проф ===")
    
    # 1. Подключение к postgres для создания toir_prof
    conn = psycopg2.connect(host="127.0.0.1", user="postgres", port=5432)
    conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
    cur = conn.cursor()
    
    cur.execute("SELECT 1 FROM pg_database WHERE datname = 'toir_prof';")
    if not cur.fetchone():
        print("Создание базы данных toir_prof...")
        cur.execute("CREATE DATABASE toir_prof ENCODING 'UTF8';")
    else:
        print("База данных toir_prof уже существует.")
    
    cur.close()
    conn.close()

    # 2. Подключение непосредственно к toir_prof
    conn = psycopg2.connect(host="127.0.0.1", user="postgres", port=5432, dbname="toir_prof")
    conn.autocommit = True
    cur = conn.cursor()

    scripts = [
        ("01_schema.sql", "database/01_schema.sql"),
        ("02_triggers_procedures.sql", "database/02_triggers_procedures.sql"),
        ("03_seed_data.sql", "database/03_seed_data.sql"),
        ("04_verify_import.sql", "database/04_verify_import.sql"),
    ]

    for name, path in scripts:
        print(f"\nВыполнение {name}...")
        with open(path, "r", encoding="utf-8") as f:
            sql_content = f.read()
        cur.execute(sql_content)
        print(f"{name} успешно применен!")

    # 3. Вывод количества записей в каждой таблице
    print("\n" + "="*50)
    print("ИТОГОВАЯ СВОДКА ТАБЛИЦ И КОЛИЧЕСТВА СТРОК:")
    print("="*50)
    cur.execute("""
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        ORDER BY table_name;
    """)
    tables = [row[0] for row in cur.fetchall()]
    
    all_ok = True
    for t in tables:
        cur.execute(f"SELECT COUNT(*) FROM {t};")
        cnt = cur.fetchone()[0]
        status = "[УСПЕХ >= 5]" if cnt >= 5 else "[ОШИБКА < 5]"
        if cnt < 5:
            all_ok = False
        print(f"Таблица: {t:<28} Количество строк: {cnt:<4} {status}")

    print("="*50)
    if all_ok:
        print("ВЕСЬ ИМПОРТ ЗАВЕРШЕН НА 100%! ВСЕ ТАБЛИЦЫ СОДЕРЖАТ >= 5 СТРОК.")
    else:
        print("ВНИМАНИЕ: Некоторые таблицы содержат менее 5 строк!")
    print("="*50)

    cur.close()
    conn.close()

if __name__ == "__main__":
    setup_database()
