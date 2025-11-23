# RevitHelpBot

Telegram-бот на .NET 9 для диагностики проблем с Revit-файлами. Архитектура — модульный монолит, чтобы логику можно было переиспользовать в плагине Revit (C#) без переписывания.

## Что есть сейчас
- Слои: `RevitHelpBot.Core` (доменные DTO), `RevitHelperBot.Application` (сервисы/обработчики), `RevitHelperBot.Api` (хост, DI, Telegram).
- Бот как `BackgroundService` с библиотекой `Telegram.Bot`.
- Excel-хранилище сценария загружается и кэшируется (`ScenarioService` + `ExcelScenarioRepository`), есть команда `/reload` для горячей перезагрузки (только для `Admin:AllowedUserIds`).
- Диалоговый движок минимальный: `/start` шлет приветствие, остальное — простое эхо. Ветвление по Excel-графу, динамические кнопки, поиск по ключевым словам и ветки диагностики еще не реализованы.

## Запланированное (MVP)
- Дата-дривен диалог: кнопки и переходы из Excel, без if/else в коде.
- Поиск по `Keywords` при свободном вводе текста.
- Ветки диагностики (Акселератор, Диск, Worksets) с картинками.
- API для будущего плагина Revit на базе Core-моделей.

## Проекты
- `RevitHelpBot.Core` — доменные сущности и контракты (без зависимостей от фреймворков).
- `RevitHelpBot.Application` — сценарии, локализация, хранилище состояния диалогов, сервисы бота (используют только Core).
- `RevitHelpBot.Api` — Web API и хост для бота (DI, конфигурация, фоновые службы).

## Требования
- .NET SDK 9 (`/home/asd/.dotnet/dotnet` установлен локально).
- Интернет для первого `dotnet restore` (NuGet).

## Поведение бота (текущее)
- `/start` → приветствие `System Online`, установка состояния выбора темы.
- `/reload` → перезагрузка сценария с диска (доступно только `Admin:AllowedUserIds`).
- Любой другой текст или callback data → эхо этим же сообщением (пока без ветвления по сценариям).

## Конфигурация
- Telegram: `Telegram:BotToken` или переменная `Telegram__BotToken`.
- Админы: `Admin:AllowedUserIds` (список числовых chatId, например `[1,2,3]`).
- Сценарий: `Scenario:FilePath` путь к Excel-файлу (по умолчанию `data/scenario.xlsx`), используется `MiniExcel`.

### Excel-формат (как читает код сейчас)
- `Id` — уникальный ключ шага.
- `MessageText` — текст сообщения.
- `ImageUrl` — URL картинки (если нужен вывод с изображением).
- `Keywords` — список ключевых слов через запятую.
- `Buttons` — `Текст:NextId | Текст2:NextId2`.
> Позже нужно будет свести формат к колонкам из ТЗ менеджера (`Text`, `Image`, папка `/images`) и добавить поиск/ветвление.

## Запуск локально
```bash
/home/asd/.dotnet/dotnet run --project RevitHelperBot.Api
```
Здоровье: `GET /health`, корневой пинг: `GET /`.

## Docker
```bash
docker compose up --build
```
(Использует `docker-compose.yml`, требует `TELEGRAM_BOT_TOKEN`.)

## Тесты
```bash
/home/asd/.dotnet/dotnet test RevitHelperBot.sln
```
> При сетевых ограничениях для NuGet/VSTest может потребоваться разрешить сетевые сокеты.
