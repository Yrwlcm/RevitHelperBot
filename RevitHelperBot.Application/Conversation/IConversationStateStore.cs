namespace RevitHelperBot.Application.Conversation;

public interface IConversationStateStore
{
    Task<ConversationState> GetStateAsync(long chatId, CancellationToken cancellationToken);

    Task SetStateAsync(long chatId, ConversationState state, CancellationToken cancellationToken);
}
