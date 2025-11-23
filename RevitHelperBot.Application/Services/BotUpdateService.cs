using Microsoft.Extensions.Logging;
using RevitHelperBot.Core.Entities;
using RevitHelperBot.Core.Interfaces;

namespace RevitHelperBot.Application.Services;

public class BotUpdateService : IBotUpdateService
{
    private readonly IBotMessageSender messageSender;
    private readonly ILogger<BotUpdateService> logger;

    public BotUpdateService(IBotMessageSender messageSender, ILogger<BotUpdateService> logger)
    {
        this.messageSender = messageSender;
        this.logger = logger;
    }

    public async Task HandleMessageAsync(BotMessage message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            logger.LogInformation("Ignoring empty message from chat {ChatId}", message.ChatId);
            return;
        }

        if (string.Equals(message.Command, "/start", StringComparison.OrdinalIgnoreCase))
        {
            await messageSender.SendTextMessageAsync(message.ChatId, "System Online", cancellationToken);
            return;
        }

        await messageSender.SendTextMessageAsync(
            message.ChatId,
            message.Text!,
            cancellationToken);
    }
}
