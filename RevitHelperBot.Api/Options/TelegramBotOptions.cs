namespace RevitHelperBot.Api.Options;

public sealed class TelegramBotOptions
{
    public const string SectionName = "Telegram";
    public const string HttpClientName = "telegram_bot_client";

    public string? BotToken { get; init; }
}
