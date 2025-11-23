using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Core.Entities;
using Xunit;

namespace RevitHelperBot.Application.Tests;

public class ConversationEngineTests
{
    private readonly InMemoryConversationStateStore stateStore = new();
    private readonly FakeResponseSender responseSender = new();
    private readonly FakeLocalizationService localization = new();

    private ConversationEngine CreateEngine() => new(stateStore, localization, responseSender);

    [Fact]
    public async Task StartCommand_SetsTopicSelectionAndSendsWelcome()
    {
        const string welcome = "System Online";
        localization.WelcomeMessage = welcome;
        var engine = CreateEngine();
        var update = new BotUpdate(1, 2, "user", "/start", "/start", null);

        await engine.HandleAsync(update, CancellationToken.None);

        Assert.Single(responseSender.Responses);
        Assert.Equal(welcome, responseSender.Responses[0].Text);
        Assert.Equal(ConversationState.TopicSelection, await stateStore.GetStateAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task CallbackData_IsPreferredOverText()
    {
        var engine = CreateEngine();
        var update = new BotUpdate(5, 5, "user", "ignored text", null, "chosen-node");

        await engine.HandleAsync(update, CancellationToken.None);

        Assert.Single(responseSender.Responses);
        Assert.Equal("echo:chosen-node", responseSender.Responses[0].Text);
        Assert.Equal("chosen-node", localization.LastEchoPayload);
    }

    [Fact]
    public async Task EmptyPayload_DoesNothing()
    {
        var engine = CreateEngine();
        var update = new BotUpdate(10, 10, "user", "   ", null, null);

        await engine.HandleAsync(update, CancellationToken.None);

        Assert.Empty(responseSender.Responses);
    }

    private sealed class FakeResponseSender : IBotResponseSender
    {
        public List<BotResponse> Responses { get; } = new();

        public Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken)
        {
            Responses.Add(response);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLocalizationService : ILocalizationService
    {
        public string? LastEchoPayload { get; private set; }

        public string WelcomeMessage { get; set; } = "welcome";

        public string FormatEcho(string message, ConversationState state)
        {
            LastEchoPayload = message;
            return $"echo:{message}";
        }
    }
}
