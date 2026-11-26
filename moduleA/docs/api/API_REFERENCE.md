# Документация REST API АИС «ТОиР-Проф»

Полная спецификация OpenAPI 3.0 доступна в файле `moduleA/docs/api/openapi.yaml` и интерактивно через Swagger UI по адресу `http://localhost:5000/swagger`.

---

## 1. Аутентификация, безопасность и разграничение прав (RBAC)

- **Схема аутентификации:** Bearer JWT (`Authorization: Bearer <token>`), алгоритм подписи HMAC-SHA256, время жизни токена — 8 часов.
- **Роли в системе:**
  - `admin` (Администратор системы) — полный доступ ко всем разделам, справочникам, пользователям и аудиту.
  - `chief_engineer` (Главный инженер) — аналитика, отчёты, утверждение графиков ТО, контроль простоев.
  - `master` (Мастер цеха) — создание заявок, контроль оборудования цеха, закрытие выполненных заявок.
  - `brigadier` (Бригадир ремонтной бригады) — взятие заявок в работу, списание материалов, заполнение отчёта о выполнении.
- **Защита эндпоинтов:** Все модифицирующие операции (`POST`, `PUT`, `DELETE`) защищены атрибутом `[Authorize]`; при отсутствии или истечении токена сервер возвращает `401 Unauthorized`.

---

## 2. Группы эндпоинтов и параметры

### 2.1. Аутентификация и оргструктура (`/api/auth`)
| Метод | Путь | Назначение | Параметры |
|---|---|---|---|
| `POST` | `/api/auth/login` | Авторизация и выпуск JWT | Body: `email` (string, обяз.), `password` (string, обяз.), `rememberMe` (bool, опц.) |
| `GET` | `/api/auth/me` | Профиль текущего пользователя | Header `Authorization: Bearer ...` |
| `GET` | `/api/auth/roles` | Справочник ролей | — |
| `GET` | `/api/auth/workshops` | Справочник цехов | — |
| `GET` | `/api/auth/teams` | Ремонтные бригады (состав, цехи M:N, загрузка) | Query: `workshopId` (int, опц.) |

### 2.2. Оборудование (`/api/equipment`)
| Метод | Путь | Назначение | Параметры |
|---|---|---|---|
| `GET` | `/api/equipment` | Пагинированный реестр с фильтрами | Query: `workshopId`, `statusId`, `model`, `search`, `sortBy`, `sortDesc`, `page` (def 1), `pageSize` (1..100) |
| `GET` | `/api/equipment/statuses` | Справочник статусов оборудования | — |
| `GET` | `/api/equipment/{id}` | Детальная карточка (статусы, ТО, заявки) | Path: `id` (int, обяз.) |
| `POST` | `/api/equipment` | Добавление оборудования (JWT) | Body: `CreateEquipmentRequest` |
| `PUT` | `/api/equipment/{id}` | Обновление данных и статуса (JWT) | Path: `id`; Body: `UpdateEquipmentRequest` |
| `DELETE` | `/api/equipment/{id}` | Безопасное удаление (JWT, `409` при связях) | Path: `id` (int, обяз.) |

### 2.3. Заявки на ремонт (`/api/work-orders`, алиас `/api/WorkOrders`)
| Метод | Путь | Назначение | Параметры |
|---|---|---|---|
| `GET` | `/api/work-orders` | Реестр заявок с фильтрами и сортировкой | Query: `statusId`, `priorityId`, `workshopId`, `teamId`, `search`, `sortBy`, `sortDesc`, `page`, `pageSize` |
| `GET` | `/api/work-orders/statuses` | Справочник статусов заявок | — |
| `GET` | `/api/work-orders/priorities` | Справочник приоритетов заявок | — |
| `GET` | `/api/work-orders/{id}` | Карточка заявки с материалами | Path: `id` (int, обяз.) |
| `POST` | `/api/work-orders` | Создание заявки (`Z-YYYY-XXXX`, JWT) | Body: `CreateWorkOrderRequest` |
| `PUT` | `/api/work-orders/{id}/assign-team` | Назначение бригады с контролем занятости (`409`) | Path: `id`; Body: `{ "teamId": int }` |
| `PUT` | `/api/work-orders/{id}/status` | Смена статуса, расчёт простоя и списание материалов | Path: `id`; Body: `UpdateOrderStatusRequest` |
| `POST` | `/api/work-orders/{id}/materials` | Привязка материала со склада к заявке | Path: `id`; Body: `{ "warehouseId": int?, "materialId": int, "quantityUsed": decimal }` |

### 2.4. График ППР (`/api/schedules`), Склады (`/api/materials`) и Аналитика (`/api/analytics`)
- `GET /api/schedules` (`workshopId`, `equipmentId`, `month`, `year`, `status`), `POST /api/schedules`, `POST /api/schedules/{id}/generate-order`, `GET /api/schedules/types`.
- `GET /api/materials` (`warehouseId`, `onlyLowStock`), `GET /api/materials/warehouses`, `GET /api/materials/suppliers`, `POST /api/materials/{id}/adjust` (`warehouseId`, `quantityDelta`, `reason`).
- `GET /api/analytics/dashboard` (`workshopId`), `GET /api/analytics/workshop-summary/{workshopId}` (`startDate`, `endDate`).

---

## 3. Структуры запросов, примеры и коды ошибок

### Пример успешного входа (`POST /api/auth/login` -> `200 OK`)
```json
// Запрос
{ "email": "admin@pmz.ru", "password": "password123", "rememberMe": true }

// Ответ
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-11-27T09:00:00Z",
  "user": { "userId": 1, "email": "admin@pmz.ru", "roleCode": "admin", "roleName": "Администратор системы", "workshopId": 4 }
}
```

### Пример создания заявки (`POST /api/work-orders` -> `201 Created`)
```json
{
  "orderType": "emergency",
  "priorityId": 4,
  "equipmentId": 1,
  "description": "Повышенная вибрация шпинделя",
  "failureReason": "Износ подшипника передней опоры",
  "plannedStartDate": "2026-11-27T10:00:00Z",
  "plannedEndDate": "2026-11-27T14:00:00Z"
}
```

### Коды ответов и обработка ошибок
| HTTP-код | Значение | Пример причины в АИС «ТОиР-Проф» |
|---|---|---|
| `200 / 201` | Успешно | Данные получены или ресурс создан (`Location` header) |
| `400 Bad Request` | Ошибка валидации | Неверные даты (`OperationDate < CommissioningDate`), пустая причина у аварийной заявки, отрицательный остаток |
| `401 Unauthorized` | Не авторизован | Отсутствует или просрочен JWT-токен, неверный пароль |
| `403 Forbidden` | Доступ запрещён | Деактивированный пользователь или недостаточно прав |
| `404 Not Found` | Не найдено | Указан несуществующий `equipmentId`, `workOrderId` или `materialId` |
| `409 Conflict` | Конфликт целостности | Дубль `inventoryNumber`, удаление оборудования с заявками, пересечение графика бригады |
| `500 Server Error` | Ошибка сервера | Исключение на сервере/БД (возвращается JSON `{ "message": "..." }` без утечки стека) |
