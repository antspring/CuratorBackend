# Куратор — backend

Backend учебной платформы «Куратор» для подбора наставников: ученик (mentee) находит наставника (mentor),
пара работает над целями и задачами, куратор сопровождает процесс.

Статус: в разработке, этап MVP (Must Have).

## Стек

| Компонент | Технология |
| --- | --- |
| Платформа | .NET 10, ASP.NET Core |
| База данных | PostgreSQL 18 |
| Доступ к данным | EF Core 10 + Npgsql, именование таблиц и колонок в `snake_case` |
| API | REST-контроллеры, встроенный OpenAPI, ошибки в формате `application/problem+json` |
| Тесты | xUnit |
| Локальный запуск | Docker Compose |

## Архитектура

Модульный монолит по принципам чистой архитектуры. Зависимости направлены внутрь:

```
API ──► Infrastructure ──► Application ──► Domain
 └──────────────────────────►┘
```

| Проект | Назначение |
| --- | --- |
| `Domain` | Сущности и бизнес-инварианты. Ни от чего не зависит: без EF Core и ASP.NET Core |
| `Application` | Сценарии использования и узкие интерфейсы хранилищ и внешних сервисов |
| `Infrastructure` | Реализация интерфейсов: EF Core, PostgreSQL, внешние сервисы |
| `API` | Контроллеры, middleware, точка сборки приложения |
| `Tests/UnitTests` | Модульные тесты, включая проверку направления зависимостей между слоями |

Принципы: SOLID, KISS, DRY. Без MediatR, универсального Repository и собственного Unit of Work.

## Требования

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) — версия зафиксирована в `global.json`
- [Docker](https://www.docker.com/) с Docker Compose

## Локальный запуск в Docker

```bash
cp .env.example .env
```

Если `.env` уже есть, дополните его недостающими переменными из шаблона.

Задайте в `.env` пароль `POSTGRES_PASSWORD`, ключ подписи `JWT_SIGNING_KEY` и ключи `FILE_STORAGE_ACCESS_KEY` и `FILE_STORAGE_SECRET_KEY` для локального хранилища. Ключ подписи должен быть не короче 32 символов:

```bash
openssl rand -base64 48
```

Затем:

```bash
docker compose -f compose.dev.yaml up -d --build
```

После запуска:

| Адрес | Что это |
| --- | --- |
| http://localhost:8080/health | Проверка состояния API и подключения к PostgreSQL |
| http://localhost:8080/openapi/v1.json | Документ OpenAPI (только в среде Development) |
| `localhost:5432` | PostgreSQL для IDE и `dotnet ef` |
| `localhost:9000` | RustFS для хранения аватаров |
| http://localhost:8025 | Входящие письма в Mailpit |
| `localhost:1025` | SMTP Mailpit для запуска API из IDE |

Из других контейнеров Compose Mailpit доступен на `mailpit:1025`. Mailpit сохраняет полученные письма в локальном почтовом ящике для разработки.

Для новой базы примените [миграции](#миграции-базы-данных) вручную.

## Локальный запуск без Docker для API

Поднимите локальные сервисы:

```bash
docker compose -f compose.dev.yaml up -d postgres storage-init mailpit
```

Восстановите локальные инструменты (`dotnet-ef`):

```bash
dotnet tool restore
```

Задайте строку подключения в user secrets. Пароль тот же, что `POSTGRES_PASSWORD` в `.env`:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=curator;Username=curator;Password=<пароль>" --project API
```

Задайте ключ подписи JWT:

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project API
```

Задайте `FileStorage:AccessKey` и `FileStorage:SecretKey` в user secrets со значениями `FILE_STORAGE_ACCESS_KEY` и `FILE_STORAGE_SECRET_KEY` из `.env`. Если меняли имя бакета или порт RustFS, задайте также `FileStorage:Bucket`, `FileStorage:ServiceUrl` и `FileStorage:PublicBaseUrl`.

Запустите API:

```bash
dotnet run --project API
```

## Конфигурация и секреты

Секретов в репозитории нет: ни в `appsettings*.json`, ни в коде. Приложение читает настройки по ключу
и не знает, откуда пришло значение. Источники по возрастанию приоритета:
`appsettings.json` → `appsettings.{Environment}.json` → user secrets (только Development) → переменные окружения.

В переменных окружения `:` заменяется на `__`: ключ `ConnectionStrings:Database` задаётся
переменной `ConnectionStrings__Database`.

| Ключ | Назначение |
| --- | --- |
| `ConnectionStrings:Database` | Строка подключения к PostgreSQL. Обязателен: без него приложение не стартует |
| `Jwt:SigningKey` | Ключ подписи JWT, не короче 32 символов. Секрет. Обязателен: без него приложение не стартует |
| `Jwt:Issuer`, `Jwt:Audience` | Издатель и аудитория токенов. Заданы в `appsettings.json`, не секрет |
| `Jwt:AccessTokenLifetime`, `Jwt:RefreshTokenLifetime` | Сроки жизни токенов: `00:15:00` и `14.00:00:00` |
| `Cors:AllowedOrigins` | Массив origin frontend, которым разрешены запросы к API |
| `RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:Window` | Лимит запросов к регистрации, входу и обновлению токена с одного IP: по умолчанию 10 за `00:01:00`. Сверх лимита — `429` с заголовком `Retry-After` |

Где задаются значения:

| Среда | Источник |
| --- | --- |
| `dotnet run` / IDE | dotnet user secrets проекта `API` |
| Dev Compose | `.env` из `.env.example` |
| Prod Compose | `.env.prod` из `.env.prod.example` или переменные окружения из хранилища секретов |

### Origin frontend (CORS)

Frontend работает на другом домене, поэтому API разрешает запросы только с перечисленных origin.

- **Development:** разрешены `http://localhost:3000` и `http://localhost:5173` (`appsettings.Development.json`).
- **Остальные среды:** список пуст, все cross-origin запросы запрещены, пока origin не задан явно.
  Если frontend получает ошибку CORS в браузере, первым делом проверьте эту настройку.

Origin задаётся без пути и без завершающего `/`: `https://curator.example`, а не `https://curator.example/`.

`Cors:AllowedOrigins` — массив, поэтому в переменной окружения у ключа есть индекс:

```bash
Cors__AllowedOrigins__0=https://curator.example
```

В `compose.prod.yaml` значение передаётся через `CORS_ALLOWED_ORIGIN`.

## Запуск в проде

`compose.prod.yaml` запускает API в среде `Production`. PostgreSQL и S3-совместимое хранилище предоставляются отдельно.

1. Скопируйте `.env.prod.example` в `.env.prod`. Укажите опубликованный образ с нужным тегом в `CURATOR_API_IMAGE`, строку подключения `DATABASE_CONNECTION_STRING`, отдельный ключ `JWT_SIGNING_KEY`, `CORS_ALLOWED_ORIGIN` и параметры бакета `FILE_STORAGE_*`.

   В шаблоне приведён пример для Yandex Object Storage. Для выбранного хранилища укажите адрес S3 API (`FILE_STORAGE_SERVICE_URL`), регион (`FILE_STORAGE_REGION`) и режим адресации бакета (`FILE_STORAGE_FORCE_PATH_STYLE`). `FILE_STORAGE_PUBLIC_BASE_URL` задаёт базовый адрес, по которому клиенты открывают аватары.

2. Скачайте образ. Для приватного пакета GitHub Container Registry сначала авторизуйтесь через `docker login ghcr.io` с токеном, у которого есть право `read:packages`.

   ```bash
   docker compose --env-file .env.prod -f compose.prod.yaml pull api
   ```

3. Примените миграции вручную до запуска новой версии:

   ```bash
   ConnectionStrings__Database="<строка подключения к боевой базе>" dotnet ef database update --project Infrastructure --startup-project API
   ```

4. Запустите контейнер и проверьте `GET /health` — ожидается `200 Healthy`.

   ```bash
   docker compose --env-file .env.prod -f compose.prod.yaml up -d
   curl -fsS http://localhost:8080/health
   ```

По умолчанию API слушает `127.0.0.1:8080` на хосте для доступа через reverse proxy. Адрес и порт можно задать через `API_BIND_ADDRESS` и `API_PORT`.

Лимит запросов считается по IP клиента. Если API стоит за reverse proxy или балансировщиком, все запросы
приходят с IP прокси и делят один лимит — тогда нужно настроить проброс реального IP (`X-Forwarded-For`).

Если приложение не стартует, проверьте его логи командой `docker compose --env-file .env.prod -f compose.prod.yaml logs api`.

## Миграции базы данных

Миграции применяются **только вручную**, приложение при старте их не запускает.

Создать миграцию:

```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project API --output-dir Persistence/Migrations
```

Применить к базе из user secrets:

```bash
dotnet ef database update --project Infrastructure --startup-project API
```

## Тесты

```bash
dotnet test
```

| Проект | Что проверяет |
| --- | --- |
| `Tests/UnitTests` | Доменные инварианты и направление зависимостей между слоями |
| `Tests/IntegrationTests` | HTTP API целиком: регистрация, вход, токены, ошибки, ограничение частоты |

Интеграционные тесты поднимают настоящий PostgreSQL 18 в контейнере через Testcontainers,
поэтому для них должен быть запущен Docker. Строка подключения, ключ JWT и миграции настраиваются
автоматически — user secrets и `.env` не нужны.
