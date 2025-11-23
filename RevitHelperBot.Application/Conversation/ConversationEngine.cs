using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Conversation;

public class ConversationEngine : IConversationEngine
{
    private readonly IConversationStateStore stateStore;
    private readonly ILocalizationService localizationService;
    private readonly IBotResponseSender responseSender;

    public ConversationEngine(
        IConversationStateStore stateStore,
        ILocalizationService localizationService,
        IBotResponseSender responseSender)
    {
        this.stateStore = stateStore;
        this.localizationService = localizationService;
        this.responseSender = responseSender;
    }

    public async Task HandleAsync(BotUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        cancellationToken.ThrowIfCancellationRequested();

        if (IsStartCommand(update.Command))
        {
            await stateStore.SetStateAsync(update.ChatId, ConversationState.TopicSelection, cancellationToken);
            await responseSender.SendAsync(update.ChatId, new BotResponse(localizationService.WelcomeMessage), cancellationToken);
            return;
        }

        var payload = update.CallbackData ?? update.Text;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        var state = await stateStore.GetStateAsync(update.ChatId, cancellationToken);
        var responseText = localizationService.FormatEcho(payload, state);
        await responseSender.SendAsync(update.ChatId, new BotResponse(responseText), cancellationToken);
    }

    private static bool IsStartCommand(string? command) =>
        string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase);
}
