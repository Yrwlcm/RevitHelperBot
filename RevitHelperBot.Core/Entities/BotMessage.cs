namespace RevitHelperBot.Core.Entities;

public sealed record BotMessage(long ChatId, string? Username, string? Text, string? Command);
