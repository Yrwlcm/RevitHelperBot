using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Conversation;

public class ConversationEngine : IConversationEngine
{
    private readonly IConversationStateStore stateStore;
    private readonly ILocalizationService localizationService;
    private readonly IBotResponseSender responseSender;
    private readonly IScenarioService scenarioService;
    private readonly IDocumentSearchService documentSearchService;
    private readonly IDocumentSearchResultFormatter documentSearchResultFormatter;

    public ConversationEngine(
        IConversationStateStore stateStore,
        ILocalizationService localizationService,
        IBotResponseSender responseSender,
        IScenarioService scenarioService,
        IDocumentSearchService documentSearchService,
        IDocumentSearchResultFormatter documentSearchResultFormatter)
    {
        this.stateStore = stateStore;
        this.localizationService = localizationService;
        this.responseSender = responseSender;
        this.scenarioService = scenarioService;
        this.documentSearchService = documentSearchService;
        this.documentSearchResultFormatter = documentSearchResultFormatter;
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

        if (!string.IsNullOrWhiteSpace(update.CallbackData))
        {
            var node = scenarioService.GetNode(update.CallbackData);
            if (node is not null)
            {
                await stateStore.SetStateAsync(update.ChatId, ConversationState.InDialogue, cancellationToken);
                await SendNodeAsync(update.ChatId, node, cancellationToken);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(update.Text))
        {
            return;
        }

        if (update.Text.StartsWith("/", StringComparison.Ordinal))
        {
            await responseSender.SendAsync(
                update.ChatId,
                new BotResponse("Неизвестная команда. Введите текст для поиска по документам или отправьте /start."),
                cancellationToken);
            return;
        }

        await stateStore.SetStateAsync(update.ChatId, ConversationState.InDialogue, cancellationToken);

        var searchResult = await documentSearchService.SearchAsync(update.Text, cancellationToken);
        var responseText = documentSearchResultFormatter.Format(searchResult);
        await responseSender.SendAsync(update.ChatId, new BotResponse(responseText), cancellationToken);
    }

    private static bool IsStartCommand(string? command) =>
        string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase);

    private Task SendNodeAsync(long chatId, DialogueNode node, CancellationToken cancellationToken)
    {
        var buttons = node.Buttons.Count == 0 ? null : node.Buttons;
        var response = new BotResponse(node.Text, buttons, node.ImageUrl);
        return responseSender.SendAsync(chatId, response, cancellationToken);
    }
}
