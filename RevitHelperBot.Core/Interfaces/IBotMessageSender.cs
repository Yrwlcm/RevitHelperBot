namespace RevitHelperBot.Core.Interfaces;

public interface IBotMessageSender
{
    Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken);
}
