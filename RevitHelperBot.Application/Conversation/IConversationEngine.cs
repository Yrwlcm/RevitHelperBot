using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Conversation;

public interface IConversationEngine
{
    Task HandleAsync(BotUpdate update, CancellationToken cancellationToken);
}
