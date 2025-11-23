using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Entities;
using RevitHelperBot.Core.Enums;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RevitHelperBot.Application.Tests;

public class BotUpdateServiceTests
{
    private readonly FakeResponseSender responseSender = new();
    private readonly IConversationStateStore stateStore = new InMemoryConversationStateStore();
    private readonly LocalizationService localization = new();
    private readonly FakeScenarioService scenarioService = new();

    private BotUpdateService CreateService(IEnumerable<long>? admins = null)
    {
        var engine = new ConversationEngine(stateStore, localization, responseSender);
        var options = OptionsFactory.Create(new AdminOptions { AllowedUserIds = admins?.ToList() ?? new List<long>() });
        return new BotUpdateService(engine, scenarioService, responseSender, options, NullLogger<BotUpdateService>.Instance);
    }

    [Fact]
    public async Task StartCommand_SendsWelcomeWithTopics()
    {
        var service = CreateService();
        var update = new BotUpdate(1, 1, "user", "/start", "/start", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        Assert.Single(responseSender.Responses);
        Assert.Equal(localization.WelcomeMessage, responseSender.Responses[0].Text);
        Assert.Equal(ConversationState.TopicSelection, await stateStore.GetStateAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Reload_ByAdmin_ReloadsAndResponds()
    {
        var service = CreateService(new[] { 42L });
        var update = new BotUpdate(10, 42, "admin", "/reload", "/reload", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        Assert.True(scenarioService.ReloadCalled);
        Assert.Equal("✅ Configuration reloaded from disk.", responseSender.LastMessageText);
    }

    [Fact]
    public async Task Reload_ByNonAdmin_Denied()
    {
        var service = CreateService(new[] { 99L });
        var update = new BotUpdate(10, 1, "user", "/reload", "/reload", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        Assert.False(scenarioService.ReloadCalled);
        Assert.Equal("⛔ Access denied.", responseSender.LastMessageText);
    }

    private sealed class FakeResponseSender : IBotResponseSender
    {
        public List<BotResponse> Responses { get; } = new();
        public string? LastMessageText => Responses.LastOrDefault()?.Text;

        public Task SendAsync(long chatId, BotResponse response, CancellationToken cancellationToken)
        {
            Responses.Add(response);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeScenarioService : IScenarioService
    {
        public bool ReloadCalled { get; private set; }

        public DialogueNode? GetNode(string id) => null;

        public Task ReloadData(CancellationToken cancellationToken)
        {
            ReloadCalled = true;
            return Task.CompletedTask;
        }
    }
}
