using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Tests;

public class ConversationEngineTests
{
    private InMemoryConversationStateStore stateStore = null!;
    private FakeResponseSender responseSender = null!;
    private FakeLocalizationService localization = null!;
    private FakeScenarioService scenario = null!;

    [SetUp]
    public void SetUp()
    {
        stateStore = new InMemoryConversationStateStore();
        responseSender = new FakeResponseSender();
        localization = new FakeLocalizationService();
        scenario = new FakeScenarioService();
    }

    private ConversationEngine CreateEngine() =>
        new(stateStore, localization, responseSender, scenario);

    [Test]
    public async Task StartCommand_UsesRootNodeWhenPresent()
    {
        scenario.WithNode(new DialogueNode("start", "Root menu", null, new List<string>(), new List<ButtonOption>
        {
            new("Go", "step2")
        }));
        var engine = CreateEngine();
        var update = new BotUpdate(1, 2, "user", "/start", "/start", null);

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        var response = responseSender.Responses.Single();
        response.Text.Should().Be("Root menu");
        response.Buttons.Should().NotBeNull();
        response.Buttons![0].Text.Should().Be("Go");
        (await stateStore.GetStateAsync(1, CancellationToken.None)).Should().Be(ConversationState.TopicSelection);
    }

    [Test]
    public async Task StartCommand_FallsBackToWelcomeWithoutRoot()
    {
        const string welcome = "System Online";
        localization.WelcomeMessage = welcome;
        var engine = CreateEngine();
        var update = new BotUpdate(5, 6, "user", "/start", "/start", null);

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        responseSender.Responses[0].Text.Should().Be(welcome);
        (await stateStore.GetStateAsync(5, CancellationToken.None)).Should().Be(ConversationState.TopicSelection);
    }

    [Test]
    public async Task CallbackData_LoadsNodeById()
    {
        scenario.WithNode(new DialogueNode("next", "Next step", "https://img", new List<string>(), new List<ButtonOption>()));
        var engine = CreateEngine();
        var update = new BotUpdate(10, 10, "user", "ignored", null, "next");

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        responseSender.Responses[0].Text.Should().Be("Next step");
        responseSender.Responses[0].ImageUrl.Should().Be("https://img");
        (await stateStore.GetStateAsync(10, CancellationToken.None)).Should().Be(ConversationState.InDialogue);
    }

    [Test]
    public async Task Text_SearchesByKeywordsWhenNoExactNode()
    {
        scenario.WithNode(new DialogueNode("disk", "Check disk space", null, new List<string> { "диск", "ssd" }, new List<ButtonOption>()));
        var engine = CreateEngine();
        var update = new BotUpdate(11, 11, "user", "Проблема с диск", null, null);

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        responseSender.Responses[0].Text.Should().Be("Check disk space");
    }

    [Test]
    public async Task UnknownText_EchoesBack()
    {
        var engine = CreateEngine();
        var update = new BotUpdate(12, 12, "user", "unknown text", null, null);

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        responseSender.Responses[0].Text.Should().Be("echo:unknown text");
    }

    [Test]
    public async Task EmptyPayload_DoesNothing()
    {
        var engine = CreateEngine();
        var update = new BotUpdate(13, 13, "user", "   ", null, null);

        await engine.HandleAsync(update, CancellationToken.None);

        responseSender.Responses.Should().BeEmpty();
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
        public string WelcomeMessage { get; set; } = "welcome";

        public string FormatEcho(string message, ConversationState state) => $"echo:{message}";
    }

    private sealed class FakeScenarioService : IScenarioService
    {
        private readonly Dictionary<string, DialogueNode> nodes = new(StringComparer.OrdinalIgnoreCase);

        public DialogueNode? GetNode(string id) => nodes.TryGetValue(id, out var node) ? node : null;

        public DialogueNode? FindByKeyword(string searchText)
        {
            var normalized = searchText.ToLowerInvariant();
            return nodes.Values.FirstOrDefault(n => n.Keywords.Any(k => normalized.Contains(k.ToLowerInvariant())));
        }

        public Task ReloadData(CancellationToken cancellationToken) => Task.CompletedTask;

        public void WithNode(DialogueNode node) => nodes[node.Id] = node;
    }
}
