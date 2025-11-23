using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Conversation;
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
    private readonly IBotResponseSender responseSender;
    private readonly AdminOptions adminOptions;
    private readonly ILogger<BotUpdateService> logger;

    public BotUpdateService(
        IConversationEngine conversationEngine,
        IScenarioService scenarioService,
        IBotResponseSender responseSender,
        IOptions<AdminOptions> adminOptions,
        ILogger<BotUpdateService> logger)
    {
        this.conversationEngine = conversationEngine;
        this.scenarioService = scenarioService;
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

        if (IsReloadCommand(update.Command))
        {
            await HandleReloadAsync(update, cancellationToken);
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

    private async Task HandleReloadAsync(BotUpdate update, CancellationToken cancellationToken)
    {
        if (!IsAdmin(update.SenderId))
        {
            await responseSender.SendAsync(update.ChatId, new BotResponse("⛔ Access denied.", null), cancellationToken);
            return;
        }

        await scenarioService.ReloadData(cancellationToken);
        await responseSender.SendAsync(update.ChatId, new BotResponse("✅ Configuration reloaded from disk.", null), cancellationToken);
    }

    private bool IsAdmin(long senderId) => adminOptions.AllowedUserIds.Contains(senderId);

    private static bool IsReloadCommand(string? command) =>
        string.Equals(command, "/reload", StringComparison.OrdinalIgnoreCase);
}
