namespace RevitHelperBot.Api.Contracts;

public sealed record SimulateRequest
{
    public long ChatId { get; init; }
    public long? SenderId { get; init; }
    public string? Username { get; init; }
    public string? Text { get; init; }
    public string? CallbackData { get; init; }
}

public sealed record SimulateResponse(IReadOnlyList<SimulateMessage> Messages);

public sealed record SimulateMessage(string Text, string? ImageUrl, IReadOnlyList<SimulateButton> Buttons);

public sealed record SimulateButton(string Text, string NextNodeId);
