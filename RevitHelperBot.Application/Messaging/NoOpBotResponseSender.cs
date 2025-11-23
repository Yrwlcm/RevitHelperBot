namespace RevitHelperBot.Application.Messaging;

public class NoOpBotResponseSender : IBotResponseSender
{
    public Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
