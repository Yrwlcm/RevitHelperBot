using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Conversation;

public class ConversationEngine : IConversationEngine
{
    private readonly IConversationStateStore stateStore;
    private readonly ILocalizationService localizationService;
    private readonly IBotResponseSender responseSender;
    private readonly IScenarioService scenarioService;

    public ConversationEngine(
        IConversationStateStore stateStore,
        ILocalizationService localizationService,
        IBotResponseSender responseSender,
        IScenarioService scenarioService)
    {
        this.stateStore = stateStore;
        this.localizationService = localizationService;
        this.responseSender = responseSender;
        this.scenarioService = scenarioService;
    }

    public async Task HandleAsync(BotUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        cancellationToken.ThrowIfCancellationRequested();

        if (IsStartCommand(update.Command))
        {
            await stateStore.SetStateAsync(update.ChatId, ConversationState.TopicSelection, cancellationToken);
            var rootNode = scenarioService.GetNode("start");

            if (rootNode is not null)
            {
                await SendNodeAsync(update.ChatId, rootNode, cancellationToken);
                return;
            }

            await responseSender.SendAsync(
                update.ChatId,
                new BotResponse(localizationService.WelcomeMessage),
                cancellationToken);
            return;
        }

        var payload = update.CallbackData ?? update.Text;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        var node = ResolveNode(payload);
        if (node is not null)
        {
            await stateStore.SetStateAsync(update.ChatId, ConversationState.InDialogue, cancellationToken);
            await SendNodeAsync(update.ChatId, node, cancellationToken);
            return;
        }

        var state = await stateStore.GetStateAsync(update.ChatId, cancellationToken);
        var responseText = localizationService.FormatEcho(payload, state);
        await responseSender.SendAsync(update.ChatId, new BotResponse(responseText), cancellationToken);
    }

    private static bool IsStartCommand(string? command) =>
        string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase);

    private DialogueNode? ResolveNode(string payload) =>
        string.IsNullOrWhiteSpace(payload)
            ? null
            : scenarioService.GetNode(payload) ?? scenarioService.FindByKeyword(payload);

    private Task SendNodeAsync(long chatId, DialogueNode node, CancellationToken cancellationToken)
    {
        var buttons = node.Buttons.Count == 0 ? null : node.Buttons;
        var response = new BotResponse(node.Text, buttons, node.ImageUrl);
        return responseSender.SendAsync(chatId, response, cancellationToken);
    }
}
