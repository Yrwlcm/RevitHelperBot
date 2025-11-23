using System.Globalization;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using RevitHelperBot.Application.Options;
using RevitHelperBot.Core.Entities;

namespace RevitHelperBot.Application.Scenario;

public class ExcelScenarioRepository : IScenarioRepository
{
    private readonly string filePath;

    public ExcelScenarioRepository(IOptions<ScenarioOptions> options)
    {
        filePath = options.Value.FilePath;
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

        var data = new Dictionary<string, DialogueNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in MiniExcel.Query(filePath))
        {
            var dynamicRow = (IDictionary<string, object?>)row;

            var id = GetString(dynamicRow, "Id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var text = GetString(dynamicRow, "MessageText") ?? string.Empty;
            var imageUrl = GetString(dynamicRow, "ImageUrl");
            var keywords = ParseKeywords(GetString(dynamicRow, "Keywords"));
            var buttons = ParseButtons(GetString(dynamicRow, "Buttons"));

            data[id] = new DialogueNode(id, text, imageUrl, keywords, buttons);
        }

        return data;
    }

    private static string? GetString(IDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null)
        {
            return null;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> ParseKeywords(string? keywordsRaw)
    {
        if (string.IsNullOrWhiteSpace(keywordsRaw))
        {
            return Array.Empty<string>();
        }

        return keywordsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static IReadOnlyList<ButtonOption> ParseButtons(string? buttonsRaw)
    {
        if (string.IsNullOrWhiteSpace(buttonsRaw))
        {
            return Array.Empty<ButtonOption>();
        }

        var options = new List<ButtonOption>();
        var segments = buttonsRaw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            {
                options.Add(new ButtonOption(parts[0], parts[1]));
            }
        }

        return options;
    }
}
