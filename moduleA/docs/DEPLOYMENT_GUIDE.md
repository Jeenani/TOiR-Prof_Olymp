# Инструкция по развертыванию АИС «ТОиР-Проф»

## 1. Требования к окружению
| Компонент | Минимальные / целевые требования |
|---|---|
| **Операционная система** | Windows 10/11 (x64) или Linux (Ubuntu 22.04+) |
| **СУБД** | PostgreSQL **18.x** (совместима с 15+) |
| **Платформа .NET** | .NET SDK **10.0** (для сборки/запуска API, Desktop, Blazor и xUnit-тестов) |
| **Node.js (для Web SPA)** | Node.js **20.x+** и npm **10.x+** |
| **Аппаратные ресурсы** | CPU 2+ ядра, RAM от 4 ГБ, свободное место на диске от 1.5 ГБ |

---

## 2. Пошаговая последовательность установки

### Шаг 1. Подготовка и развертывание базы данных PostgreSQL
1. Убедитесь, что служба PostgreSQL запущена на порту `5432`.
2. Создайте базу данных `toir_prof` (если не создана):
   ```sql
   CREATE DATABASE toir_prof ENCODING 'UTF8';
   ```
3. Примените единый скрипт создания 22 таблиц 3НФ, триггеров/процедур и тестовых данных:
   ```powershell
   psql -U postgres -d toir_prof -f moduleA/database/toir_prof_db.sql
   ```
   *(Альтернативно: `python moduleA/database/apply_setup.py` — автоматически применит `toir_prof_db.sql` и проверит наличие $\ge 5$ записей во всех 22 таблицах.)*

### Шаг 2. Настройка и запуск серверного REST API (`ToirProf.Api`)
1. Откройте `moduleA/src/ToirProf.Api/appsettings.json` и проверьте строку подключения к БД и параметры JWT.
2. Соберите и запустите API:
   ```powershell
   dotnet restore ToirProf.slnx
   dotnet build ToirProf.slnx
   dotnet run --project moduleA/src/ToirProf.Api/ToirProf.Api.csproj
   ```
3. Сервер будет доступен по адресу `http://localhost:5000`, Swagger UI — `http://localhost:5000/swagger`.

### Шаг 3. Запуск Web-клиента (`toir-prof-web`, Vue 3 SPA)
```powershell
cd moduleA/src/toir-prof-web
npm install
npm run dev       # режим разработки на http://localhost:5173
# или для production-сборки:
npm run build
```

### Шаг 4. Запуск Desktop-клиента (`ToirProf.Desktop`, Avalonia UI)
```powershell
dotnet run --project moduleA/src/ToirProf.Desktop/ToirProf.Desktop.csproj
```
*(Desktop-клиент общается с системой строго по HTTP REST API на `http://localhost:5000`; прямого подключения к БД в нём нет.)*

### Шаг 5. Запуск модульных (xUnit) и интеграционных тестов
```powershell
dotnet test ToirProf.slnx
python test_api.py
```

---

## 3. Описание конфигурации и параметров (`appsettings.json` и клиентские настройки)
| Параметр | Файл | Назначение и значение по умолчанию |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ToirProf.Api/appsettings.json` | Подключение к PostgreSQL: `Host=localhost;Port=5432;Database=toir_prof;Username=postgres;Password=...` |
| `Jwt:Key` | `ToirProf.Api/appsettings.json` | Секретный ключ подписи JWT (минимум 32 символа) |
| `Jwt:Issuer` / `Jwt:Audience` | `ToirProf.Api/appsettings.json` | Издатель (`ToirProfApi`) и аудитория (`ToirProfClients`) токенов |
| Порты сервисов | `launchSettings.json` / Vite | REST API: `5000` (HTTP), Web SPA: `5173`, PostgreSQL: `5432` |
| `settings.json` / `errors.log` / `drafts.json` | `%LocalAppData%\ToirProf.Desktop\` | Локальные настройки темы/шрифта/шаблонов отчётов, журнал ошибок и офлайн-черновики заявок |

---

## 4. Проверка работоспособности и типовые проблемы
- **Проверка после развертывания:** откройте `http://localhost:5000/swagger/index.html`, выполните `POST /api/auth/login` с учётными данными `admin@pmz.ru` / `password123` и убедитесь, что возвращается `200 OK` с полем `token`.
- **Ошибка подключения к БД (`28P01` / connection refused):** проверьте пароль пользователя `postgres` в `appsettings.json` и статус службы `postgresql-x64-18`.
- **Desktop/Web не видит API:** убедитесь, что запущен `ToirProf.Api` на порту `5000`, а в настройках Desktop (`Настройки -> Адрес REST API`) указан `http://localhost:5000`.
- **Занятый порт 5000:** завершите зависший процесс `ToirProf.Api` или задайте другой порт через `--urls http://localhost:5050`.
