using System.Text.Json;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public class JsonScenarioRepository : IScenarioRepository
{
    private readonly string filePath;

    public JsonScenarioRepository(IOptions<ScenarioOptions> options)
    {
        var configuredPath = options.Value.FilePath;
        filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    public Dictionary<string, DialogueNode> LoadScenario()
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new InvalidOperationException("Scenario file path is not configured.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Scenario file not found.", filePath);
        }

        var json = File.ReadAllText(filePath);
        var nodes = JsonSerializer.Deserialize<List<DialogueNodeDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<DialogueNodeDto>();

        var data = new Dictionary<string, DialogueNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var dto in nodes)
        {
            if (string.IsNullOrWhiteSpace(dto.Id))
            {
                continue;
            }

            var buttons = dto.Buttons?
                .Where(b => !string.IsNullOrWhiteSpace(b.Text) && !string.IsNullOrWhiteSpace(b.NextNodeId))
                .Select(b => new ButtonOption(b.Text!, b.NextNodeId!))
                .ToList() ?? new List<ButtonOption>();

            var keywords = dto.Keywords?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>();
            var node = new DialogueNode(
                dto.Id!,
                dto.Text ?? string.Empty,
                dto.ImageUrl,
                keywords,
                buttons);
            data[dto.Id] = node;
        }

        return data;
    }

    private sealed class DialogueNodeDto
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public List<string>? Keywords { get; set; }
        public List<ButtonDto>? Buttons { get; set; }
    }

    private sealed class ButtonDto
    {
        public string? Text { get; set; }
        public string? NextNodeId { get; set; }
    }
}
