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

Задайте свой пароль `POSTGRES_PASSWORD` в `.env`, затем:

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

Где задаются значения:

| Среда | Источник |
| --- | --- |
| `dotnet run` / IDE | dotnet user secrets проекта `API` |
| `docker compose` | файл `.env` (шаблон — `.env.example`); compose передаёт в контейнер только переменные из секции `environment` в `compose.yaml` |
| Прод | переменные окружения контейнера |

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

## Правила разработки

- Каждая фича разрабатывается в отдельной ветке `feature/<name>` от `master` и затем сливается в `master`.
- Сообщения коммитов — [Conventional Commits](https://www.conventionalcommits.org/ru/) на русском,
  в безличной форме: `feat: добавлена регистрация`, `fix: исправлена проверка email`.
- Общие свойства сборки (`TargetFramework`, `Nullable`, `TreatWarningsAsErrors`) задаются в
  `Directory.Build.props` и не дублируются в `.csproj`. Предупреждения компилятора считаются ошибками.
