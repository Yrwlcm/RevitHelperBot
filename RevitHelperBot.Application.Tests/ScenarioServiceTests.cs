using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RevitHelperBot.Application.Scenario;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Tests;

public class ScenarioServiceTests
{
    [Test]
    public async Task ReloadData_ReplacesCachedNodes()
    {
        var repository = new FakeScenarioRepository(new Dictionary<string, DialogueNode>
        {
            { "start", new DialogueNode("start", "old", null, new List<string>(), new List<ButtonOption>()) }
        });

        var service = new ScenarioService(repository);
        service.GetNode("start")!.Text.Should().Be("old");

        repository.SetData(new Dictionary<string, DialogueNode>
        {
            { "start", new DialogueNode("start", "new", null, new List<string>(), new List<ButtonOption>()) }
        });

        await service.ReloadData(CancellationToken.None);

        service.GetNode("start")!.Text.Should().Be("new");
    }

    private sealed class FakeScenarioRepository : IScenarioRepository
    {
        private Dictionary<string, DialogueNode> data;

        public FakeScenarioRepository(Dictionary<string, DialogueNode> data)
        {
            this.data = data;
        }

        public Dictionary<string, DialogueNode> LoadScenario() => new(data, StringComparer.OrdinalIgnoreCase);

        public void SetData(Dictionary<string, DialogueNode> newData) => data = newData;
    }
}
