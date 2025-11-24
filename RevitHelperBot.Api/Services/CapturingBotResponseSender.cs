using RevitHelperBot.Application.Messaging;

namespace RevitHelperBot.Api.Services;

public class CapturingBotResponseSender : IBotResponseSender
{
    public List<BotResponse> Responses { get; } = new();

    public Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken)
    {
        Responses.Add(response);
        return Task.CompletedTask;
    }
}
