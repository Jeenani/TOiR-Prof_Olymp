import subprocess
import time
import urllib.request
import json
import sys

def test_api():
    print("=== ЗАПУСК ТЕСТИРОВАНИЯ ТОиР-ПРОФ REST API ===")
    
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", "moduleA/src/ToirProf.Api/ToirProf.Api.csproj", "--no-build"],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        encoding="utf-8",
        errors="replace"
    )
    
    base_url = "http://localhost:5000"
    print("Ожидание запуска API на http://localhost:5000...")
    
    started = False
    for _ in range(15):
        time.sleep(1)
        try:
            req = urllib.request.Request(f"{base_url}/swagger/v1/swagger.json")
            with urllib.request.urlopen(req, timeout=2) as resp:
                if resp.status == 200:
                    started = True
                    print("API успешно запущен и Swagger доступен!")
                    break
        except Exception:
            continue
            
    if not started:
        print("Ошибка: API не ответил за 15 секунд!")
        proc.kill()
        out, err = proc.communicate()
        print("STDOUT:", out)
        print("STDERR:", err)
        return False

    success = True
    try:
        # Тест 1: Swagger JSON
        print("\n[ТЕСТ 1] Swagger документация API...")
        req = urllib.request.Request(f"{base_url}/swagger/v1/swagger.json")
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode())
            print("  Зарегистрированные маршруты в API:")
            for p in sorted(data['paths'].keys()):
                print(f"    {p}")

            print(f"  УСПЕХ: title='{data['info']['title']}', paths count={len(data['paths'])}")

        # Тест 2: Авторизация (POST /api/auth/login)
        print("\n[ТЕСТ 2] JWT аутентификация (admin@pmz.ru)...")
        login_payload = json.dumps({"email": "admin@pmz.ru", "password": "password123"}).encode()
        req = urllib.request.Request(
            f"{base_url}/api/auth/login",
            data=login_payload,
            headers={"Content-Type": "application/json"}
        )
        token = None
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode())
            token = data["token"]
            user = data["user"]
            print(f"  УСПЕХ: Пользователь {user['fullName']} ({user['roleName']})")

        auth_headers = {"Authorization": f"Bearer {token}"}

        # Тест 3: /api/auth/me
        print("\n[ТЕСТ 3] Защищенный метод /api/auth/me...")
        req = urllib.request.Request(f"{base_url}/api/auth/me", headers=auth_headers)
        with urllib.request.urlopen(req) as resp:
            me = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Email={me['email']}, Role={me['roleCode']}")

        # Тест 4: /api/equipment
        print("\n[ТЕСТ 4] Реестр оборудования...")
        req = urllib.request.Request(f"{base_url}/api/equipment?page=1&pageSize=10")
        with urllib.request.urlopen(req) as resp:
            eq = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Найдено единиц: {eq['total']}, получено: {len(eq['items'])}")
            print(f"  Пример: {eq['items'][0]['inventoryNumber']} - {eq['items'][0]['equipmentName']}")

        # Тест 5: /api/work-orders
        print("\n[ТЕСТ 5] Реестр заявок...")
        req = urllib.request.Request(f"{base_url}/api/work-orders")
        with urllib.request.urlopen(req) as resp:
            wo = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Всего заявок: {wo['total']}, получено: {len(wo['items'])}")

        # Тест 6: /api/analytics/dashboard
        print("\n[ТЕСТ 6] Сводный дашборд KPI...")
        req = urllib.request.Request(f"{base_url}/api/analytics/dashboard")
        with urllib.request.urlopen(req) as resp:
            dash = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Оборудование: {dash['totalEquipment']}, Активных заявок: {dash['activeWorkOrders']}")
            print(f"  Суммарный простой: {dash['totalDowntimeHours']} ч., График ТО: {dash['maintenanceComplianceRate']}%")
            print(f"  Топ-5 простоев: {len(dash['top5Downtime'])}, Предупреждений: {len(dash['violations'])}")

        # Тест 7: /api/materials
        print("\n[ТЕСТ 7] Складской учет материалов...")
        req = urllib.request.Request(f"{base_url}/api/materials")
        with urllib.request.urlopen(req) as resp:
            mats = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Позиций: {len(mats)}")

        # Тест 8: /api/schedules
        print("\n[ТЕСТ 8] График ТО...")
        req = urllib.request.Request(f"{base_url}/api/schedules")
        with urllib.request.urlopen(req) as resp:
            scheds = json.loads(resp.read().decode())
            print(f"  УСПЕХ: Записей в графике ТО: {len(scheds)}")

        print("\n" + "="*55)
        print("ВСЕ 8 ТЕСТОВ REST API ПРОЙДЕНЫ НА 100%! СЕРВЕР ПОЛНОСТЬЮ РАБОТАЕТ.")
        print("="*55)

    except urllib.error.HTTPError as he:
        print(f"HTTP ERROR {he.code}: {he.read().decode('utf-8', errors='replace')[:500]}")
        success = False
    except Exception as ex:
        print(f"ОШИБКА: {ex}")
        success = False

    finally:
        print("Остановка тестового сервера...")
        proc.kill()
        proc.wait()

    return success

if __name__ == "__main__":
    if not test_api():
        sys.exit(1)

