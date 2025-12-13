using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Options;

namespace RevitHelperBot.Application.Documents;

public sealed class DocumentSearchResultFormatter : IDocumentSearchResultFormatter
{
    private readonly DocumentsOptions options;

    public DocumentSearchResultFormatter(IOptions<DocumentsOptions> options)
    {
        this.options = options.Value;
    }

    public string Format(DocumentSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            DocumentSearchStatus.QueryTooShort =>
                $"Запрос слишком короткий. Введите минимум {options.MinQueryLength} символа(ов).",
            DocumentSearchStatus.IndexEmpty =>
                $"Документы не найдены. Положите файлы .docx в папку \"{options.RootPath}\" и выполните /reindex.",
            DocumentSearchStatus.Error =>
                string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Ошибка поиска." : $"Ошибка поиска: {result.ErrorMessage}",
            _ => FormatOk(result)
        };
    }

    private static string FormatOk(DocumentSearchResult result)
    {
        if (result.TotalFound == 0)
        {
            return "Ничего не найдено. Попробуйте уточнить запрос.";
        }

        var root = DirectoryNode.Root();
        foreach (var hit in result.Hits)
        {
            AddPath(root, hit.RelativePath, hit.Contexts);
        }

        var lines = new List<string>
        {
            $"Найдено файлов: {result.TotalFound}" + (result.IsTruncated ? $" (показаны первые {result.Hits.Count})" : string.Empty),
            string.Empty
        };

        Render(root, lines, 0);

        return string.Join("\n", lines).TrimEnd();
    }

    private static void AddPath(DirectoryNode root, string relativePath, IReadOnlyList<string> contexts)
    {
        var normalized = relativePath.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var node = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            node = node.GetOrAddDirectory(parts[i]);
        }

        node.Files[parts[^1]] = contexts;
    }

    private static void Render(DirectoryNode node, List<string> lines, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);

        var printedAny = false;
        foreach (var directory in node.Directories.Values.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (indentLevel == 0 && printedAny)
            {
                lines.Add(string.Empty);
            }

            lines.Add($"{indent}{directory.Name}");
            Render(directory, lines, indentLevel + 1);
            printedAny = true;
        }

        foreach (var file in node.Files.OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"{indent}- {file.Key}");
            foreach (var context in file.Value)
            {
                if (string.IsNullOrWhiteSpace(context))
                {
                    continue;
                }

                lines.Add($"{indent}  {context}");
            }
        }
    }

    private sealed class DirectoryNode
    {
        private DirectoryNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Dictionary<string, DirectoryNode> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, IReadOnlyList<string>> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public static DirectoryNode Root() => new(string.Empty);

        public DirectoryNode GetOrAddDirectory(string name)
        {
            if (!Directories.TryGetValue(name, out var node))
            {
                node = new DirectoryNode(name);
                Directories[name] = node;
            }

            return node;
        }
    }
}
