using Microsoft.Extensions.DependencyInjection;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Contracts;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Services;

public class SimulationRunner
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<SimulationRunner> logger;

    public SimulationRunner(IServiceScopeFactory scopeFactory, ILogger<SimulationRunner> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task<SimulateResponse> RunAsync(SimulateRequest request, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var responseSender = new CapturingBotResponseSender();
        var engine = ActivatorUtilities.CreateInstance<ConversationEngine>(scope.ServiceProvider, responseSender);
        var botUpdateService = ActivatorUtilities.CreateInstance<BotUpdateService>(scope.ServiceProvider, engine, responseSender);

        var command = ExtractCommand(request.Text);
        var senderId = request.SenderId ?? request.ChatId;
        var update = new BotUpdate(
            request.ChatId,
            senderId,
            request.Username ?? "web-user",
            request.Text,
            command,
            request.CallbackData);

        try
        {
            await botUpdateService.HandleUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            // Known configuration issues (missing scenario)
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to process simulation request");
            throw;
        }

        var messages = responseSender.Responses
            .Select(r => new SimulateMessage(
                r.Text,
                r.ImageUrl,
                r.Buttons?.Select(b => new SimulateButton(b.Text, b.NextNodeId)).ToList() ?? new List<SimulateButton>()))
            .ToList();

        return new SimulateResponse(messages);
    }

    private static string? ExtractCommand(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return null;
        }

        if (messageText.StartsWith("/"))
        {
            var command = messageText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            return command?.ToLowerInvariant();
        }

        return null;
    }
}
