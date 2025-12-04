using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Application.Scenario;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RevitHelperBot.Application.Tests;

public class JsonScenarioRepositoryTests
{
    private string tempFilePath = null!;

    [SetUp]
    public void SetUp()
    {
        tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }

    [Test]
    public void LoadScenario_ReadsNodesWithButtonsAndKeywords()
    {
        var json = """
        [
          {
            "id": "start",
            "text": "Hello",
            "imageUrl": "https://img",
            "keywords": ["hello", "start"],
            "buttons": [{ "text": "Next", "nextNodeId": "step1" }]
          },
          {
            "id": "step1",
            "text": "Step 1",
            "keywords": [],
            "buttons": []
          }
        ]
        """;

        File.WriteAllText(tempFilePath, json);
        var repo = CreateRepository(tempFilePath);

        var result = repo.LoadScenario();

        result.Should().HaveCount(2);
        result.Should().ContainKey("start");
        result["start"].Text.Should().Be("Hello");
        result["start"].ImageUrl.Should().Be("https://img");
        result["start"].Buttons.Should().ContainSingle();
        result["start"].Buttons[0].Text.Should().Be("Next");
        result["start"].Buttons[0].NextNodeId.Should().Be("step1");
    }

    [Test]
    public void LoadScenario_ThrowsWhenFileMissing()
    {
        var repo = CreateRepository(tempFilePath);

        var action = () => repo.LoadScenario();

        action.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void LoadScenario_UsesBaseDirectoryForRelativePath()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, $"testdata-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "scenario.json");
        File.WriteAllText(file, """
        [
          { "id": "start", "text": "Hello", "keywords": [], "buttons": [] }
        ]
        """);

        var repo = CreateRepository(Path.Combine(Path.GetFileName(folder), "scenario.json"));

        var result = repo.LoadScenario();

        result.Should().ContainKey("start");
        Directory.Delete(folder, true);
    }

    private static JsonScenarioRepository CreateRepository(string path)
    {
        var options = OptionsFactory.Create(new ScenarioOptions { FilePath = path });
        return new JsonScenarioRepository(options);
    }
}
