using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Messaging;

public sealed record BotResponse(string Text, IReadOnlyList<ButtonOption>? Buttons = null, string? ImageUrl = null);
