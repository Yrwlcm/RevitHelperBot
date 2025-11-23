namespace RevitHelperBot.Core.Entities;

public sealed record BotUpdate(
    long ChatId,
    long SenderId,
    string? Username,
    string? Text,
    string? Command,
    string? CallbackData);
