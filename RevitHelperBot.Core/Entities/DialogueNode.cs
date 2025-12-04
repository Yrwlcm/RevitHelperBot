namespace RevitHelperBot.Core.Entities;

public sealed record DialogueNode(
    string Id,
    string Text,
    string? ImageUrl,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<ButtonOption> Buttons);
