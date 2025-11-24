using System.Linq;
using RevitHelperBot.Application.Messaging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RevitHelperBot.Api.Services;

public class TelegramBotResponseSender : IBotResponseSender
{
    private readonly ITelegramBotClient botClient;

    public TelegramBotResponseSender(ITelegramBotClient botClient)
    {
        this.botClient = botClient;
    }

    public async Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        cancellationToken.ThrowIfCancellationRequested();

        var replyMarkup = BuildMarkup(response);

        if (!string.IsNullOrWhiteSpace(response.ImageUrl))
        {
            await botClient.SendPhotoAsync(
                chatId,
                Telegram.Bot.Types.InputFile.FromUri(response.ImageUrl),
                caption: response.Text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendTextMessageAsync(
            chatId,
            response.Text,
            replyMarkup: replyMarkup,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    private static InlineKeyboardMarkup? BuildMarkup(BotResponse response)
    {
        if (response.Buttons is null || response.Buttons.Count == 0)
        {
            return null;
        }

        var rows = response.Buttons
            .Select(option => InlineKeyboardButton.WithCallbackData(option.Text, option.NextNodeId))
            .Chunk(2)
            .Select(chunk => chunk.ToArray())
            .ToArray();

        return rows.Length == 0 ? null : new InlineKeyboardMarkup(rows);
    }
}
