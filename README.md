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

## Быстрый старт в Docker

```bash
cp .env.example .env
```

Задайте в `.env` свой пароль `POSTGRES_PASSWORD` и ключ подписи `JWT_SIGNING_KEY` (не короче 32 символов):

```bash
openssl rand -base64 48
```

Затем:

```bash
docker compose up -d --build
```

После запуска:

| Адрес | Что это |
| --- | --- |
| http://localhost:8080/health | Проверка состояния API и подключения к PostgreSQL |
| http://localhost:8080/openapi/v1.json | Документ OpenAPI (только в среде Development) |
| `localhost:5432` | PostgreSQL для IDE и `dotnet ef` |

## Локальный запуск без Docker для API

Поднимите только базу:

```bash
docker compose up -d postgres
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
| `docker compose` | файл `.env` (шаблон — `.env.example`); compose передаёт в контейнер только переменные из секции `environment` в `compose.yaml` |
| Прод | переменные окружения контейнера |

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

При запуске через `docker compose` добавьте в секцию `environment` сервиса `api` в `compose.yaml` строку

```yaml
      Cors__AllowedOrigins__0: ${CORS_ALLOWED_ORIGIN}
```

и задайте `CORS_ALLOWED_ORIGIN` в `.env`. Без этой строки compose не передаст значение в контейнер.

## Запуск в проде

1. Соберите образ из `API/Dockerfile`.
2. Задайте переменные окружения контейнера:

   | Переменная | Значение |
   | --- | --- |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__Database` | строка подключения к боевой базе |
   | `Jwt__SigningKey` | отдельный случайный ключ для прода, не короче 32 символов |
   | `Cors__AllowedOrigins__0` | origin frontend, например `https://curator.example` |

3. Примените миграции вручную до запуска новой версии:

   ```bash
   ConnectionStrings__Database="<строка подключения к боевой базе>" dotnet ef database update --project Infrastructure --startup-project API
   ```

4. Запустите контейнер и проверьте `GET /health` — ожидается `200 Healthy`.

Лимит запросов считается по IP клиента. Если API стоит за reverse proxy или балансировщиком, все запросы
приходят с IP прокси и делят один лимит — тогда нужно настроить проброс реального IP (`X-Forwarded-For`).

Если приложение не стартует, в логах контейнера будет причина: не задана строка подключения
или `Jwt:SigningKey` отсутствует либо короче 32 символов.

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
