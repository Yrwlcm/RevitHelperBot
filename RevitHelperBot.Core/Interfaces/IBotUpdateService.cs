using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Core.Interfaces;

public interface IBotUpdateService
{
    Task HandleUpdateAsync(BotUpdate update, CancellationToken cancellationToken);
}
