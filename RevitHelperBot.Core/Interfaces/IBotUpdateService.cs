using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Core.Interfaces;

public interface IBotUpdateService
{
    Task HandleMessageAsync(BotMessage message, CancellationToken cancellationToken);
}
