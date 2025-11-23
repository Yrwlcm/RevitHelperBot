using RevitHelperBot.Core.Interfaces;
using Telegram.Bot;

namespace RevitHelperBot.Api.Services;

public class TelegramBotMessageSender : IBotMessageSender
{
    private readonly ITelegramBotClient botClient;

    public TelegramBotMessageSender(ITelegramBotClient botClient)
    {
        this.botClient = botClient;
    }

    public Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        return botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
    }
}
