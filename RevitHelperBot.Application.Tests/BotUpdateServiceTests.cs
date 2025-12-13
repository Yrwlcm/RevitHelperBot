using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RevitHelperBot.Application.Conversation;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Localization;
using RevitHelperBot.Application.Messaging;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Application.Services;
using RevitHelperBot.Core.Entities;
using NUnit.Framework;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RevitHelperBot.Application.Tests;

public class BotUpdateServiceTests
{
    private FakeResponseSender responseSender = null!;
    private IConversationStateStore stateStore = null!;
    private LocalizationService localization = null!;
    private FakeScenarioService scenarioService = null!;
    private FakeDocumentSearchService documentSearchService = null!;
    private FakeDocumentSearchResultFormatter formatter = null!;

    [SetUp]
    public void SetUp()
    {
        responseSender = new FakeResponseSender();
        stateStore = new InMemoryConversationStateStore();
        localization = new LocalizationService();
        scenarioService = new FakeScenarioService();
        documentSearchService = new FakeDocumentSearchService();
        formatter = new FakeDocumentSearchResultFormatter();
    }

    private BotUpdateService CreateService(IEnumerable<long>? admins = null)
    {
        var engine = new ConversationEngine(stateStore, localization, responseSender, scenarioService, documentSearchService, formatter);
        var options = OptionsFactory.Create(new AdminOptions { AllowedUserIds = admins?.ToList() ?? new List<long>() });
        return new BotUpdateService(engine, scenarioService, documentSearchService, responseSender, options, NullLogger<BotUpdateService>.Instance);
    }

    [Test]
    public async Task StartCommand_SendsWelcomeWithTopics()
    {
        var service = CreateService();
        var update = new BotUpdate(1, 1, "user", "/start", "/start", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        responseSender.Responses.Should().ContainSingle();
        responseSender.Responses[0].Text.Should().Be(localization.WelcomeMessage);
        (await stateStore.GetStateAsync(1, CancellationToken.None)).Should().Be(ConversationState.TopicSelection);
    }

    [Test]
    public async Task Reload_ByAdmin_ReloadsAndResponds()
    {
        var service = CreateService(new[] { 42L });
        var update = new BotUpdate(10, 42, "admin", "/reload", "/reload", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        scenarioService.ReloadCalled.Should().BeTrue();
        documentSearchService.ReloadCalled.Should().BeTrue();
        responseSender.LastMessageText.Should().Contain("✅");
    }

    [Test]
    public async Task Reload_ByNonAdmin_Denied()
    {
        var service = CreateService(new[] { 99L });
        var update = new BotUpdate(10, 1, "user", "/reload", "/reload", null);

        await service.HandleUpdateAsync(update, CancellationToken.None);

        scenarioService.ReloadCalled.Should().BeFalse();
        documentSearchService.ReloadCalled.Should().BeFalse();
        responseSender.LastMessageText.Should().Be("⛔ Доступ запрещён.");
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

        public DialogueNode? FindByKeyword(string searchText) => null;

        public Task ReloadData(CancellationToken cancellationToken)
        {
            ReloadCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentSearchService : IDocumentSearchService
    {
        public bool ReloadCalled { get; private set; }

        public Task<DocumentSearchResult> SearchAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult(new DocumentSearchResult(query, DocumentSearchStatus.Ok, Array.Empty<DocumentSearchHit>(), 0, false, "root", 0, null));

        public Task ReloadAsync(CancellationToken cancellationToken)
        {
            ReloadCalled = true;
            return Task.CompletedTask;
        }

        public DocumentIndexStatus GetStatus() => new("root", true, 0, 0, null, null);
    }

    private sealed class FakeDocumentSearchResultFormatter : IDocumentSearchResultFormatter
    {
        public string Format(DocumentSearchResult result) => "formatted";
    }
}
