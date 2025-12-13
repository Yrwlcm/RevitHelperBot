using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Options;

var arguments = Arguments.Parse(args);
if (arguments.ShowHelp)
{
    Console.WriteLine(Arguments.HelpText);
    return;
}

var rootPath = arguments.RootPath;
var isTempRoot = false;
if (string.IsNullOrWhiteSpace(rootPath))
{
    rootPath = Path.Combine(Path.GetTempPath(), $"revithelperbot-perf-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}");
    isTempRoot = true;
}

rootPath = Path.GetFullPath(rootPath);

if (arguments.Generate)
{
    if (Directory.Exists(rootPath) && Directory.EnumerateFileSystemEntries(rootPath).Any())
    {
        throw new InvalidOperationException($"RootPath is not empty: {rootPath}. Use an empty folder or omit --root to auto-create a temp folder.");
    }

    Directory.CreateDirectory(rootPath);

    Console.WriteLine($"Generating {arguments.Documents} .docx files into: {rootPath}");
    var generation = Stopwatch.StartNew();
    var generatedBytes = DocxGenerator.Generate(rootPath, arguments);
    generation.Stop();
    Console.WriteLine($"Generated: {arguments.Documents} files, {FormatBytes(generatedBytes)} on disk, in {generation.ElapsedMilliseconds} ms");
}
else
{
    Console.WriteLine($"Using existing documents folder: {rootPath}");
}

var documentsOptions = new DocumentsOptions
{
    RootPath = rootPath,
    MinQueryLength = arguments.MinQueryLength,
    MinTokenLength = arguments.MinTokenLength,
    MaxResults = arguments.MaxResults,
    MaxDegreeOfParallelism = arguments.Parallelism
};

var repository = new FileSystemWordDocumentsRepository(Options.Create(documentsOptions));
var extractor = new DocxTextExtractor();
var searchService = new DocumentSearchService(repository, extractor, Options.Create(documentsOptions), NullLogger<DocumentSearchService>.Instance);

Console.WriteLine();
Console.WriteLine("Indexing...");

CollectGarbage();
var memBefore = GetMemorySnapshot();
var indexing = Stopwatch.StartNew();
await searchService.ReloadAsync(CancellationToken.None);
indexing.Stop();
var memAfter = GetMemorySnapshot();

var status = searchService.GetStatus();
Console.WriteLine($"Indexed documents: {status.DocumentCount}, failed: {status.FailedDocuments}");
Console.WriteLine($"Index time: {indexing.ElapsedMilliseconds} ms ({(status.DocumentCount == 0 ? 0 : status.DocumentCount / Math.Max(0.001, indexing.Elapsed.TotalSeconds)):0.0} docs/s)");
Console.WriteLine($"Memory (GC): {FormatBytes(memBefore.ManagedBytes)} -> {FormatBytes(memAfter.ManagedBytes)} (Δ {FormatBytes(memAfter.ManagedBytes - memBefore.ManagedBytes)})");
Console.WriteLine($"Memory (WorkingSet): {FormatBytes(memBefore.WorkingSetBytes)} -> {FormatBytes(memAfter.WorkingSetBytes)} (Δ {FormatBytes(memAfter.WorkingSetBytes - memBefore.WorkingSetBytes)})");

Console.WriteLine();
Console.WriteLine("Search benchmark...");

var query = arguments.Query ?? arguments.Needle;
if (string.IsNullOrWhiteSpace(query))
{
    query = "test";
}

for (var i = 0; i < arguments.WarmupRuns; i++)
{
    await searchService.SearchAsync(query, CancellationToken.None);
}

var durations = new List<long>(arguments.SearchRuns);
DocumentSearchResult? lastResult = null;

for (var i = 0; i < arguments.SearchRuns; i++)
{
    var sw = Stopwatch.StartNew();
    lastResult = await searchService.SearchAsync(query, CancellationToken.None);
    sw.Stop();
    durations.Add(sw.ElapsedTicks);
}

durations.Sort();
var avgMs = durations.Count == 0 ? 0 : durations.Average(t => t * 1000.0 / Stopwatch.Frequency);
var p50Ms = PercentileMs(durations, 0.50);
var p95Ms = PercentileMs(durations, 0.95);
var maxMs = durations.Count == 0 ? 0 : durations[^1] * 1000.0 / Stopwatch.Frequency;

Console.WriteLine($"Query: \"{query}\"");
Console.WriteLine($"Runs: {arguments.SearchRuns}, warmup: {arguments.WarmupRuns}");
if (lastResult is not null)
{
    Console.WriteLine($"Found files: {lastResult.TotalFound} (returned: {lastResult.Hits.Count})");
}

Console.WriteLine($"Latency ms: avg {avgMs:0.###}, p50 {p50Ms:0.###}, p95 {p95Ms:0.###}, max {maxMs:0.###}");

if (isTempRoot && !arguments.KeepTemp)
{
    try
    {
        Directory.Delete(rootPath, recursive: true);
        Console.WriteLine();
        Console.WriteLine($"Deleted temp folder: {rootPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Failed to delete temp folder: {rootPath}. Error: {ex.Message}");
    }
}
else
{
    Console.WriteLine();
    Console.WriteLine($"Documents folder: {rootPath}");
}

static string FormatBytes(long bytes)
{
    var abs = Math.Abs(bytes);
    const double kb = 1024.0;
    const double mb = kb * 1024.0;
    const double gb = mb * 1024.0;

    return abs switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / kb:0.##} KB",
        < 1024L * 1024 * 1024 => $"{bytes / mb:0.##} MB",
        _ => $"{bytes / gb:0.##} GB"
    };
}

static void CollectGarbage()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}

static MemorySnapshot GetMemorySnapshot()
{
    var process = Process.GetCurrentProcess();
    return new MemorySnapshot(
        GC.GetTotalMemory(forceFullCollection: false),
        process.WorkingSet64);
}

static double PercentileMs(IReadOnlyList<long> sortedTicks, double percentile)
{
    if (sortedTicks.Count == 0)
    {
        return 0;
    }

    var index = (int)Math.Round((sortedTicks.Count - 1) * percentile, MidpointRounding.AwayFromZero);
    index = Math.Clamp(index, 0, sortedTicks.Count - 1);
    return sortedTicks[index] * 1000.0 / Stopwatch.Frequency;
}

sealed record MemorySnapshot(long ManagedBytes, long WorkingSetBytes);

sealed record Arguments(
    string? RootPath,
    bool Generate,
    int Documents,
    int ApproxTextKbPerDocument,
    int Folders,
    int ParagraphsPerDocument,
    string Needle,
    int NeedleEvery,
    int MatchesPerNeedleDocument,
    string? Query,
    int WarmupRuns,
    int SearchRuns,
    int MinQueryLength,
    int MinTokenLength,
    int MaxResults,
    int Parallelism,
    bool KeepTemp,
    bool ShowHelp)
{
    public static string HelpText =>
        """
        RevitHelperBot.Perf — простая нагрузка для индексации/поиска .docx

        Usage:
          dotnet run --project RevitHelperBot.Perf -- [options]

        Options:
          --root <path>            Folder with .docx (or where to generate)
          --generate              Generate synthetic .docx files into --root (or temp if --root omitted)
          --docs <n>              Number of documents to generate (default: 1000)
          --kb <n>                Approx text size per document (KB, default: 16)
          --folders <n>           Number of top-level folders (default: 20)
          --paragraphs <n>        Paragraphs per document (default: 30)
          --needle <text>         Token to plant into some docs (default: "тз бим")
          --needleEvery <n>       Put needle into every Nth document (default: 3)
          --matches <n>           Matching paragraphs per needle document (default: 3)
          --query <text>          Query to benchmark (default: needle)
          --warmup <n>            Warmup searches (default: 5)
          --runs <n>              Measured search runs (default: 50)
          --minQuery <n>          Documents:MinQueryLength (default: 3)
          --minToken <n>          Documents:MinTokenLength (default: 2)
          --maxResults <n>        Documents:MaxResults (default: 50)
          --parallel <n>          Documents:MaxDegreeOfParallelism (default: CPU count)
          --keep                  Do not delete temp folder (when --root omitted)
          -h|--help               Show help

        Examples:
          dotnet run --project RevitHelperBot.Perf -- --generate --docs 5000 --kb 32 --query "тз бим"
          dotnet run --project RevitHelperBot.Perf -- --root ./data/docs --query "тз бим" --runs 200
        """;

    public static Arguments Parse(string[] args)
    {
        var root = (string?)null;
        var generate = false;
        var docs = 1000;
        var kb = 16;
        var folders = 20;
        var paragraphs = 30;
        var needle = "тз бим";
        var needleEvery = 3;
        var matches = 3;
        var query = (string?)null;
        var warmup = 5;
        var runs = 50;
        var minQuery = 3;
        var minToken = 2;
        var maxResults = 50;
        var parallel = Environment.ProcessorCount;
        var keep = false;
        var help = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    help = true;
                    break;
                case "--root":
                    root = ReadValue(args, ref i, "--root");
                    break;
                case "--generate":
                    generate = true;
                    break;
                case "--docs":
                    docs = ReadInt(args, ref i, "--docs");
                    break;
                case "--kb":
                    kb = ReadInt(args, ref i, "--kb");
                    break;
                case "--folders":
                    folders = ReadInt(args, ref i, "--folders");
                    break;
                case "--paragraphs":
                    paragraphs = ReadInt(args, ref i, "--paragraphs");
                    break;
                case "--needle":
                    needle = ReadValue(args, ref i, "--needle");
                    break;
                case "--needleEvery":
                    needleEvery = ReadInt(args, ref i, "--needleEvery");
                    break;
                case "--matches":
                    matches = ReadInt(args, ref i, "--matches");
                    break;
                case "--query":
                    query = ReadValue(args, ref i, "--query");
                    break;
                case "--warmup":
                    warmup = ReadInt(args, ref i, "--warmup");
                    break;
                case "--runs":
                    runs = ReadInt(args, ref i, "--runs");
                    break;
                case "--minQuery":
                    minQuery = ReadInt(args, ref i, "--minQuery");
                    break;
                case "--minToken":
                    minToken = ReadInt(args, ref i, "--minToken");
                    break;
                case "--maxResults":
                    maxResults = ReadInt(args, ref i, "--maxResults");
                    break;
                case "--parallel":
                    parallel = ReadInt(args, ref i, "--parallel");
                    break;
                case "--keep":
                    keep = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}. Use --help.");
            }
        }

        docs = Math.Max(0, docs);
        kb = Math.Max(1, kb);
        folders = Math.Max(1, folders);
        paragraphs = Math.Max(1, paragraphs);
        needleEvery = Math.Max(1, needleEvery);
        matches = Math.Max(1, matches);
        warmup = Math.Max(0, warmup);
        runs = Math.Max(1, runs);
        minQuery = Math.Max(1, minQuery);
        minToken = Math.Max(1, minToken);
        maxResults = Math.Max(1, maxResults);
        parallel = Math.Max(1, parallel);

        return new Arguments(
            root,
            generate,
            docs,
            kb,
            folders,
            paragraphs,
            needle,
            needleEvery,
            matches,
            query,
            warmup,
            runs,
            minQuery,
            minToken,
            maxResults,
            parallel,
            keep,
            help);
    }

    private static string ReadValue(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}");
        }

        i++;
        return args[i];
    }

    private static int ReadInt(string[] args, ref int i, string name)
    {
        var value = ReadValue(args, ref i, name);
        if (!int.TryParse(value, out var result))
        {
            throw new ArgumentException($"Invalid int for {name}: {value}");
        }

        return result;
    }
}

static class DocxGenerator
{
    public static long Generate(string rootPath, Arguments args)
    {
        var random = new Random(42);
        var totalBytes = 0L;

        for (var i = 0; i < args.Documents; i++)
        {
            var folderIndex = args.Folders == 0 ? 0 : i % args.Folders;
            var folder = Path.Combine(rootPath, $"Group-{folderIndex:D3}");
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"Doc-{i:D6}.docx");
            var includeNeedle = i % args.NeedleEvery == 0;
            var paragraphs = BuildParagraphs(args, includeNeedle, random);

            WriteDocx(filePath, paragraphs);
            totalBytes += new FileInfo(filePath).Length;
        }

        return totalBytes;
    }

    private static List<string> BuildParagraphs(Arguments args, bool includeNeedle, Random random)
    {
        var paragraphs = new List<string>(args.ParagraphsPerDocument);
        var wordsPerParagraph = Math.Max(5, (args.ApproxTextKbPerDocument * 1024) / Math.Max(1, args.ParagraphsPerDocument * 8));

        var needleParagraphIndexes = includeNeedle
            ? PickDistinctParagraphs(args.ParagraphsPerDocument, args.MatchesPerNeedleDocument, random)
            : Array.Empty<int>();

        for (var p = 0; p < args.ParagraphsPerDocument; p++)
        {
            var sb = new StringBuilder();
            for (var w = 0; w < wordsPerParagraph; w++)
            {
                if (w > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(RandomWord(random));
            }

            if (needleParagraphIndexes.Contains(p))
            {
                sb.Append(' ');
                sb.Append(args.Needle);
            }

            paragraphs.Add(sb.ToString());
        }

        return paragraphs;
    }

    private static int[] PickDistinctParagraphs(int paragraphCount, int take, Random random)
    {
        take = Math.Clamp(take, 1, paragraphCount);
        var set = new HashSet<int>();
        while (set.Count < take)
        {
            set.Add(random.Next(paragraphCount));
        }

        return set.OrderBy(i => i).ToArray();
    }

    private static string RandomWord(Random random)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        var length = random.Next(4, 12);
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(chars);
    }

    private static void WriteDocx(string filePath, IReadOnlyList<string> paragraphs)
    {
        using var stream = File.Create(filePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false);

        var entry = archive.CreateEntry("word/document.xml");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.Write("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
            """);

        foreach (var paragraph in paragraphs)
        {
            var safeText = EscapeXml(paragraph);
            writer.Write("<w:p><w:r><w:t>");
            writer.Write(safeText);
            writer.Write("</w:t></w:r></w:p>");
        }

        writer.Write("""
              </w:body>
            </w:document>
            """);
    }

    private static string EscapeXml(string text) =>
        text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
}

