# Guide for AI Agents

- Use .NET 9 and the local SDK at `/home/asd/.dotnet/dotnet` for commands (`run`, `test`, `build`).
- Keep layers clean:
  - Core: only domain contracts/entities, no framework refs.
  - Application: orchestrates Core, no infrastructure dependencies.
  - Api: composition root, DI, hosting, Telegram integration.
- Style:
  - Do not prefix private fields with `_`; prefer `this.field` if needed.
  - Favor DI over statics/singletons; keep services small and testable.
  - Keep configuration via options/binding; respect `Telegram:BotToken` and env `Telegram__BotToken`.
- Testing: add/keep unit tests under `RevitHelperBot.Application.Tests`; run with `/home/asd/.dotnet/dotnet test RevitHelperBot.sln`.
- Containerization: Dockerfile lives in `RevitHelperBot.Api`; compose files expect `TELEGRAM_BOT_TOKEN`.
- No database yet; avoid adding persistence until explicitly requested.
