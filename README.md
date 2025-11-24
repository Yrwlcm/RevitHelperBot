# RevitHelpBot

Telegram-бот на .NET 9 для диагностики проблем с Revit-файлами. Архитектура — модульный монолит, чтобы логику можно было переиспользовать в плагине Revit (C#) без переписывания.

## Что есть сейчас
- Слои: `RevitHelpBot.Core` (доменные DTO), `RevitHelperBot.Application` (сервисы/обработчики), `RevitHelperBot.Api` (хост, DI, Telegram).
- Бот как `BackgroundService` с библиотекой `Telegram.Bot`.
- Excel-хранилище сценария загружается и кэшируется (`ScenarioService` + `ExcelScenarioRepository`), есть команда `/reload` для горячей перезагрузки (только для `Admin:AllowedUserIds`).
- Диалоговый движок читает граф из Excel: `/start` шлет корневой узел `Id=start` (если он есть) с кнопками, callback data ведут к узлам по `NextId`, свободный ввод ищет по `Keywords` и показывает найденный узел; если нет совпадений — эхо.

## Запланированное (MVP)
- Доработать контент: ветки диагностики (Акселератор, Диск, Worksets) с картинками.
- Усилить поиск (взвешивание совпадений, fallback на текст).
- API для будущего плагина Revit на базе Core-моделей.

## Проекты
- `RevitHelpBot.Core` — доменные сущности и контракты (без зависимостей от фреймворков).
- `RevitHelpBot.Application` — сценарии, локализация, хранилище состояния диалогов, сервисы бота (используют только Core).
- `RevitHelpBot.Api` — Web API и хост для бота (DI, конфигурация, фоновые службы).

## Требования
- .NET SDK 9 (`/home/asd/.dotnet/dotnet` установлен локально).
- Интернет для первого `dotnet restore` (NuGet).

## Поведение бота (текущее)
- `/start` → установка состояния выбора темы и вывод узла с `Id=start` (если есть), иначе приветствие `System Online`.
- `/reload` → перезагрузка сценария с диска (доступно только `Admin:AllowedUserIds`).
- Callback data → переход к узлу с указанным `Id`.
- Текст без команды → поиск по `Keywords`; если узел найден — вывод текста/кнопок/картинки; если нет — эхо этим же текстом.

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

## Как проверить руками
- Локально: `dotnet run --project RevitHelperBot.Api`, в Telegram боте отправить `/start` и убедиться, что приходит корневой узел из Excel (или приветствие, если `start` не задан). Нажать кнопки → переходы по `NextId`. Отправить текст, совпадающий с `Keywords` в Excel → приходит соответствующий узел; другой текст → эхо.
- Перезагрузка: обновить Excel, отправить `/reload` с аккаунта из `Admin:AllowedUserIds`, убедиться, что контент сменился.
- Если нужно проверить изображения, задать `ImageUrl` в Excel и увидеть фото+подпись в ответе.

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
