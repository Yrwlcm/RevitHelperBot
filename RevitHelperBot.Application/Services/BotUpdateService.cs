using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Core.Entities;
using RevitHelperBot.Core.Interfaces;

namespace RevitHelperBot.Application.Services;

public class BotUpdateService : IBotUpdateService
{
    private readonly IConversationEngine conversationEngine;
    private readonly IScenarioService scenarioService;
    private readonly IDocumentSearchService documentSearchService;
    private readonly IBotResponseSender responseSender;
    private readonly AdminOptions adminOptions;
    private readonly ILogger<BotUpdateService> logger;

    public BotUpdateService(
        IConversationEngine conversationEngine,
        IScenarioService scenarioService,
        IDocumentSearchService documentSearchService,
        IBotResponseSender responseSender,
        IOptions<AdminOptions> adminOptions,
        ILogger<BotUpdateService> logger)
    {
        this.conversationEngine = conversationEngine;
        this.scenarioService = scenarioService;
        this.documentSearchService = documentSearchService;
        this.responseSender = responseSender;
        this.adminOptions = adminOptions.Value;
        this.logger = logger;
    }

    public async Task HandleUpdateAsync(BotUpdate update, CancellationToken cancellationToken)
    {
        if (update is null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        if (IsReloadCommand(update.Command) || IsReindexCommand(update.Command))
        {
            await HandleReindexAsync(update, cancellationToken);
            return;
        }

        try
        {
            await conversationEngine.HandleAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process update for chat {ChatId}", update.ChatId);
            throw;
        }
    }

    private async Task HandleReindexAsync(BotUpdate update, CancellationToken cancellationToken)
    {
        if (!IsAdmin(update.SenderId))
        {
            await responseSender.SendAsync(update.ChatId, new BotResponse("⛔ Доступ запрещён.", null), cancellationToken);
            return;
        }

        var errors = new List<string>();

        try
        {
            await scenarioService.ReloadData(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to reload scenario configuration");
            errors.Add("scenario");
        }

        try
        {
            await documentSearchService.ReloadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to rebuild documents index");
            errors.Add("documents");
        }

        var indexStatus = documentSearchService.GetStatus();
        var message = errors.Count == 0
            ? $"✅ Индекс обновлён. Документов: {indexStatus.DocumentCount}."
            : $"⚠️ Обновление завершилось с ошибками ({string.Join(", ", errors)}). Документов: {indexStatus.DocumentCount}.";

        await responseSender.SendAsync(update.ChatId, new BotResponse(message, null), cancellationToken);
    }

    private bool IsAdmin(long senderId) => adminOptions.AllowedUserIds.Contains(senderId);

    private static bool IsReloadCommand(string? command) =>
        string.Equals(command, "/reload", StringComparison.OrdinalIgnoreCase);

    private static bool IsReindexCommand(string? command) =>
        string.Equals(command, "/reindex", StringComparison.OrdinalIgnoreCase);
}
