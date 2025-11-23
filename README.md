# RevitHelpBot

Модульный монолит на .NET 9 с Telegram-подпиской в виде `BackgroundService`.

## Проекты
- `RevitHelpBot.Core` — доменные сущности и контракты (без зависимостей от фреймворков).
- `RevitHelpBot.Application` — сервисы/обработчики команд (используют только Core).
- `RevitHelpBot.Api` — Web API и хост для бота (DI, конфигурация, фоновые службы).

## Требования
- .NET SDK 9 (`/home/asd/.dotnet/dotnet` установлен локально).
- Токен бота: `appsettings.json` → `Telegram:BotToken` или переменная `Telegram__BotToken`.

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

## Поведение бота
- `/start` → `System Online`.
- Любой другой текст → эхо тем же сообщением.
