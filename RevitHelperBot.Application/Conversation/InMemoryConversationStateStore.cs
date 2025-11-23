using System.Collections.Concurrent;

namespace RevitHelperBot.Application.Conversation;

public class InMemoryConversationStateStore : IConversationStateStore
{
    private readonly ConcurrentDictionary<long, ConversationState> stateByChat = new();

    public Task<ConversationState> GetStateAsync(long chatId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var state = stateByChat.TryGetValue(chatId, out var storedState)
            ? storedState
            : ConversationState.None;
        return Task.FromResult(state);
    }

    public Task SetStateAsync(long chatId, ConversationState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        stateByChat[chatId] = state;
        return Task.CompletedTask;
    }
}
