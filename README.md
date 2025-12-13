# RevitHelperBot — поиск по Word (.docx)

Проект превращён в чат-бот для поиска текста по вордовским файлам. Документы складываются в папку (можно с подпапками), бот индексирует текст и по запросу возвращает список совпадений с сохранением иерархии папок.

## Как пользоваться

1) Положите документы `.docx` в `data/docs` (вложенные папки сохраняются).
2) Запустите API и откройте веб-симулятор.
3) Отправьте `/start`, затем введите запрос (одно или несколько ключевых слов).
4) После добавления/удаления файлов выполните `/reindex` (доступно только админам).

## Ограничения / подготовка файлов

- Поддерживается `.docx` (Word OpenXML). Старый `.doc` нужно предварительно сохранить как `.docx`.
- Если документ — скан/картинки без текстового слоя, поиск не найдёт его (нужен OCR отдельно).
- Извлекается текст из `word/document.xml` (+ `footnotes`/`endnotes` при наличии).

## Конфигурация

- `Admin:AllowedUserIds` — кто может выполнять `/reindex` (senderId/chatId).
- `Documents:RootPath` — корневая папка документов (по умолчанию `data/docs`).
  - `MinQueryLength`, `MinTokenLength`, `MaxResults`, `MaxDegreeOfParallelism`.
- `Scenario:FilePath` — JSON для `/start` и кнопок помощи (по умолчанию `data/scenario.json`).

В `RevitHelperBot.Api/appsettings.Development.json` по умолчанию указан `Documents:RootPath = RevitHelperBot.Api/Examples` для локальной проверки на примерах.

## Запуск локально

```bash
/home/asd/.dotnet/dotnet run --project RevitHelperBot.Api
```

Откройте адрес из лога (обычно `http://localhost:5000`) и используйте симулятор чата.

## Docker

```bash
docker compose up --build
```

- `./data` монтируется в контейнер как `/app/data`
- документы кладите в `./data/docs`
- при обновлении файлов отправьте `/reindex`

## Тесты

```bash
/home/asd/.dotnet/dotnet test RevitHelperBot.sln
```

## Прикидочное нагрузочное тестирование

В репозитории есть консольный раннер `RevitHelperBot.Perf` — он может:
- сгенерировать пачку синтетических `.docx` (временная папка по умолчанию),
- построить индекс,
- прогнать серию поисковых запросов и вывести latency + память.

Пример (генерация + индекс + бенч поиска):

```bash
/home/asd/.dotnet/dotnet run --project RevitHelperBot.Perf -- --generate --docs 5000 --kb 32 --query "тз бим"
```

Пример (использовать текущую папку документов без генерации):

```bash
/home/asd/.dotnet/dotnet run --project RevitHelperBot.Perf -- --root ./data/docs --query "тз бим" --runs 200
```

Справка по параметрам:

```bash
/home/asd/.dotnet/dotnet run --project RevitHelperBot.Perf -- --help
```
