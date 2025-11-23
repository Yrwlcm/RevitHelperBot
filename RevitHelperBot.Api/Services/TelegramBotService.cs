using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RevitHelperBot.Core.Entities;
using RevitHelperBot.Core.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RevitHelperBot.Api.Services;

public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient botClient;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TelegramBotService> logger;

    public TelegramBotService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotService> logger)
    {
        this.botClient = botClient;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);

        var botDetails = await botClient.GetMeAsync(stoppingToken);
        logger.LogInformation("Telegram bot @{Username} started polling", botDetails.Username);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is not { Text: { } messageText } message)
        {
            return;
        }

        var command = ExtractCommand(messageText);

        using var scope = scopeFactory.CreateScope();
        var botUpdateService = scope.ServiceProvider.GetRequiredService<IBotUpdateService>();

        try
        {
            var botMessage = new BotMessage(
                message.Chat.Id,
                message.From?.Username,
                messageText,
                command);

            await botUpdateService.HandleMessageAsync(botMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process update for chat {ChatId}", message.Chat.Id);
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error [{apiRequestException.ErrorCode}]: {apiRequestException.Message}",
            _ => exception.Message
        };

        logger.LogError(exception, "Telegram polling error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    private static string? ExtractCommand(string messageText)
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
