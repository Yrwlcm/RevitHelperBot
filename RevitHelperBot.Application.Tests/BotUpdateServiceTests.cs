using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Entities;
using RevitHelperBot.Core.Interfaces;
using Xunit;

namespace RevitHelperBot.Application.Tests;

public class BotUpdateServiceTests
{
    private static BotUpdateService CreateService(FakeSender sender) =>
        new(sender, NullLogger<BotUpdateService>.Instance);

    [Fact]
    public async Task StartCommand_SendsSystemOnlineResponse()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var message = new BotMessage(123, "user", "/start", "/start");

        await service.HandleMessageAsync(message, CancellationToken.None);

        Assert.Single(sender.SentMessages);
        Assert.Equal((123L, "System Online"), sender.SentMessages[0]);
    }

    [Fact]
    public async Task EchoMessage_SendsBackSameText()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var text = "hello there";
        var message = new BotMessage(321, "user", text, null);

        await service.HandleMessageAsync(message, CancellationToken.None);

        Assert.Single(sender.SentMessages);
        Assert.Equal((321L, text), sender.SentMessages[0]);
    }

    [Fact]
    public async Task EmptyMessage_DoesNotSend()
    {
        var sender = new FakeSender();
        var service = CreateService(sender);
        var message = new BotMessage(999, "user", "   ", null);

        await service.HandleMessageAsync(message, CancellationToken.None);

        Assert.Empty(sender.SentMessages);
    }

    private sealed class FakeSender : IBotMessageSender
    {
        public List<(long ChatId, string Text)> SentMessages { get; } = new();

        public Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken)
        {
            SentMessages.Add((chatId, text));
            return Task.CompletedTask;
        }
    }
}
