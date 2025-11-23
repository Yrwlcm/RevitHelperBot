namespace RevitHelperBot.Application.Messaging;

public interface IBotResponseSender
{
    Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken);
}
