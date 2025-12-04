# RevitHelpBot

Telegram-бот на .NET 9 для диагностики проблем с Revit-файлами. Архитектура — модульный монолит, чтобы логику можно было переиспользовать в плагине Revit (C#) без переписывания.

## Что есть сейчас
- Слои: `RevitHelpBot.Core` (доменные DTO), `RevitHelperBot.Application` (сервисы/обработчики), `RevitHelperBot.Api` (хост, DI, Telegram).
- Telegram-интеграция выключена: используется веб-симулятор и HTTP-контроллеры.
- JSON-хранилище сценария загружается и кэшируется (`ScenarioService` + `JsonScenarioRepository`), есть команда `/reload` для горячей перезагрузки (только для `Admin:AllowedUserIds`).
- Диалоговый движок читает граф из JSON: `/start` шлет корневой узел `Id=start` (если он есть) с кнопками, callback data ведут к узлам по `NextId`, свободный ввод ищет по `Keywords` и показывает найденный узел; если нет совпадений — эхо.
- Веб-симулятор чата без Telegram на `/` (статическая страница) и контроллер `POST /api/simulation` для тех же сценариев.

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
- Админы: `Admin:AllowedUserIds` (список числовых chatId, например `[1,2,3]`).
- Сценарий: `Scenario:FilePath` путь к JSON-файлу (по умолчанию `data/scenario.json`). Относительные пути резолвятся относительно `AppContext.BaseDirectory`.

### JSON-формат сценария
- Файл по умолчанию: `data/scenario.json`.
- Структура: массив объектов `{ "id": "start", "text": "Hello", "imageUrl": "https://...", "keywords": ["кэш","акселерратор"], "buttons": [ { "text": "Далее", "nextNodeId": "step2" } ] }`.
> При обновлении нужно отправить `/reload`, чтобы кэш перечитался без рестарта.

## Веб-симулятор
- Запуск: `dotnet run --project RevitHelperBot.Api`, затем открыть адрес из лога (обычно http://localhost:5000) в браузере.
- Интерфейс: поле ChatId, Username, окно сообщений и кнопок из сценария. Сообщения отправляются на `POST /api/simulation`.
- Проверка: `/start` отдает `start`-узел из JSON, кнопки переходят по `nextNodeId`, текст матчит `keywords`, остальное — эхо. `/reload` перечитывает `Scenario:FilePath`.

## API (для плагина/интеграций)
- `POST /api/simulation` — симуляция диалога, тело: `{ "chatId": 1, "username": "web-user", "text": "/start", "callbackData": null }`, ответ: список сообщений (text/image/buttons).
- `GET /health` — healthcheck.
- Telegram-поллинг отключен в текущей сборке; используйте HTTP API или веб-симулятор.

## Как проверить руками
- Локально: `dotnet run --project RevitHelperBot.Api`, отправить `/start` через веб-симулятор или `POST /api/simulation`. Нажать кнопки → переходы по `NextId`. Отправить текст, совпадающий с `Keywords` → приходит соответствующий узел; другой текст → эхо.
- Перезагрузка: обновить `data/scenario.json`, отправить `/reload` с аккаунта из `Admin:AllowedUserIds`, убедиться, что контент сменился.
- Если нужно проверить изображения, задать `ImageUrl` в JSON и увидеть фото+подпись в ответе.
- Для плагина Revit можно использовать тот же API (`/api/simulation`) или подключить `Core/Application` как библиотеку.

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
