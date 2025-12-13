using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Options;

namespace RevitHelperBot.Application.Documents;

public sealed class DocumentSearchService : IDocumentSearchService
{
    private readonly IWordDocumentsRepository repository;
    private readonly IDocxTextExtractor textExtractor;
    private readonly DocumentsOptions options;
    private readonly ILogger<DocumentSearchService> logger;
    private readonly SemaphoreSlim reloadLock = new(1, 1);

    private DocumentIndex index = DocumentIndex.Empty;
    private DocumentIndexStatus status;

    public DocumentSearchService(
        IWordDocumentsRepository repository,
        IDocxTextExtractor textExtractor,
        IOptions<DocumentsOptions> options,
        ILogger<DocumentSearchService> logger)
    {
        this.repository = repository;
        this.textExtractor = textExtractor;
        this.options = options.Value;
        this.logger = logger;

        status = new DocumentIndexStatus(repository.RootPath, false, 0, 0, null, null);
    }

    public DocumentIndexStatus GetStatus() => status;

    public async Task<DocumentSearchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedQuery = SearchTextNormalizer.Normalize(query);
        if (normalizedQuery.Length < options.MinQueryLength)
        {
            return new DocumentSearchResult(
                query,
                DocumentSearchStatus.QueryTooShort,
                Array.Empty<DocumentSearchHit>(),
                0,
                false,
                repository.RootPath,
                status.DocumentCount,
                null);
        }

        var localIndex = await EnsureIndexAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(status.LastError) && localIndex.Documents.Count == 0)
        {
            return new DocumentSearchResult(
                query,
                DocumentSearchStatus.Error,
                Array.Empty<DocumentSearchHit>(),
                0,
                false,
                repository.RootPath,
                0,
                status.LastError);
        }

        if (localIndex.Documents.Count == 0)
        {
            return new DocumentSearchResult(
                query,
                DocumentSearchStatus.IndexEmpty,
                Array.Empty<DocumentSearchHit>(),
                0,
                false,
                repository.RootPath,
                0,
                null);
        }

        var queryTokens = SplitTokens(normalizedQuery, options.MinTokenLength);
        if (queryTokens.Count == 0)
        {
            return new DocumentSearchResult(
                query,
                DocumentSearchStatus.QueryTooShort,
                Array.Empty<DocumentSearchHit>(),
                0,
                false,
                repository.RootPath,
                localIndex.Documents.Count,
                null);
        }

        var candidates = ResolveCandidates(localIndex, queryTokens);
        if (candidates.Count == 0)
        {
            return new DocumentSearchResult(
                query,
                DocumentSearchStatus.Ok,
                Array.Empty<DocumentSearchHit>(),
                0,
                false,
                repository.RootPath,
                localIndex.Documents.Count,
                null);
        }

        var orderedMatches = candidates
            .Select(docId =>
            {
                var doc = localIndex.Documents[docId];
                var phraseMatch = doc.NormalizedText.Contains(normalizedQuery, StringComparison.Ordinal);
                return new OrderedMatch(docId, doc.RelativePath, phraseMatch);
            })
            .OrderByDescending(h => h.PhraseMatch)
            .ThenBy(h => h.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalFound = orderedMatches.Count;
        var truncated = orderedMatches.Count > options.MaxResults;
        if (truncated)
        {
            orderedMatches = orderedMatches.Take(options.MaxResults).ToList();
        }

        var hits = orderedMatches
            .Select(m =>
            {
                var doc = localIndex.Documents[m.DocumentId];
                var contexts = FindParagraphContexts(doc.Paragraphs, normalizedQuery, queryTokens);
                return new DocumentSearchHit(doc.RelativePath, m.PhraseMatch, contexts);
            })
            .ToList();

        return new DocumentSearchResult(
            query,
            DocumentSearchStatus.Ok,
            hits,
            totalFound,
            truncated,
            repository.RootPath,
            localIndex.Documents.Count,
            null);
    }

    public Task ReloadAsync(CancellationToken cancellationToken) => ReloadInternalAsync(forceReload: true, cancellationToken);

    private async Task<DocumentIndex> EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (status.IsReady)
        {
            return index;
        }

        await ReloadInternalAsync(forceReload: false, cancellationToken);
        return index;
    }

    private async Task ReloadInternalAsync(bool forceReload, CancellationToken cancellationToken)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceReload && status.IsReady)
            {
                return;
            }

            var builtAt = DateTimeOffset.UtcNow;
            var files = await repository.ListAsync(cancellationToken);

            if (files.Count == 0)
            {
                index = DocumentIndex.Empty;
                status = new DocumentIndexStatus(repository.RootPath, true, 0, 0, builtAt, null);
                return;
            }

            var normalizedTexts = new ConcurrentBag<(string RelativePath, string NormalizedText, IReadOnlyList<ParagraphEntry> Paragraphs)>();
            var failedDocuments = new ConcurrentBag<string>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, options.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
            {
                try
                {
                    await using var stream = await repository.OpenReadAsync(file, ct);
                    var extractedText = await textExtractor.ExtractTextAsync(stream, ct);
                    var normalized = SearchTextNormalizer.Normalize(extractedText);
                    var paragraphs = SplitParagraphs(extractedText);

                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        normalizedTexts.Add((file.RelativePath, normalized, paragraphs));
                    }
                }
                catch (Exception ex)
                {
                    failedDocuments.Add(file.RelativePath);
                    logger.LogWarning(ex, "Failed to index document {Path}", file.RelativePath);
                }
            });

            var documents = normalizedTexts
                .OrderBy(d => d.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(d => new DocumentEntry(d.RelativePath, d.NormalizedText, d.Paragraphs))
                .ToList();

            var tokenToDocuments = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            for (var docId = 0; docId < documents.Count; docId++)
            {
                var tokens = SplitTokens(documents[docId].NormalizedText, options.MinTokenLength);
                foreach (var token in tokens)
                {
                    if (!tokenToDocuments.TryGetValue(token, out var docIds))
                    {
                        docIds = new HashSet<int>();
                        tokenToDocuments[token] = docIds;
                    }

                    docIds.Add(docId);
                }
            }

            index = new DocumentIndex(documents, tokenToDocuments);
            status = new DocumentIndexStatus(
                repository.RootPath,
                true,
                documents.Count,
                failedDocuments.Count,
                builtAt,
                null);
        }
        catch (Exception ex)
        {
            status = new DocumentIndexStatus(
                repository.RootPath,
                true,
                index.Documents.Count,
                status.FailedDocuments,
                status.LastIndexedAtUtc,
                ex.Message);
            logger.LogError(ex, "Failed to rebuild document index");
        }
        finally
        {
            reloadLock.Release();
        }
    }

    private static HashSet<int> ResolveCandidates(DocumentIndex localIndex, IReadOnlyList<string> queryTokens)
    {
        List<HashSet<int>> sets = new(queryTokens.Count);
        foreach (var token in queryTokens)
        {
            if (!localIndex.TokenToDocuments.TryGetValue(token, out var docIds))
            {
                return new HashSet<int>();
            }

            sets.Add(docIds);
        }

        var ordered = sets.OrderBy(s => s.Count).ToList();
        var candidates = new HashSet<int>(ordered[0]);

        for (var i = 1; i < ordered.Count; i++)
        {
            candidates.IntersectWith(ordered[i]);
            if (candidates.Count == 0)
            {
                break;
            }
        }

        return candidates;
    }

    private static List<string> SplitTokens(string normalizedText, int minTokenLength)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return new List<string>();
        }

        var tokens = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var unique = new HashSet<string>(StringComparer.Ordinal);

        foreach (var token in tokens)
        {
            if (token.Length < minTokenLength)
            {
                continue;
            }

            unique.Add(token);
        }

        return unique.ToList();
    }

    private static IReadOnlyList<ParagraphEntry> SplitParagraphs(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Array.Empty<ParagraphEntry>();
        }

        var lines = extractedText
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var paragraphs = new List<ParagraphEntry>(lines.Length);
        foreach (var line in lines)
        {
            var paragraph = line.Trim();
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                continue;
            }

            paragraphs.Add(new ParagraphEntry(paragraph, SearchTextNormalizer.Normalize(paragraph)));
        }

        return paragraphs;
    }

    private static IReadOnlyList<string> FindParagraphContexts(
        IReadOnlyList<ParagraphEntry> paragraphs,
        string normalizedQuery,
        IReadOnlyList<string> queryTokens)
    {
        if (paragraphs.Count == 0)
        {
            return Array.Empty<string>();
        }

        var strongMatches = new List<string>();
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bestScore = -1;
        string? bestText = null;

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph.NormalizedText))
            {
                continue;
            }

            var phraseMatch = paragraph.NormalizedText.Contains(normalizedQuery, StringComparison.Ordinal);
            var matchedTokens = 0;
            foreach (var token in queryTokens)
            {
                if (paragraph.NormalizedText.Contains(token, StringComparison.Ordinal))
                {
                    matchedTokens++;
                }
            }

            if (phraseMatch || matchedTokens == queryTokens.Count)
            {
                if (unique.Add(paragraph.Text))
                {
                    strongMatches.Add(paragraph.Text);
                }

                continue;
            }

            var score = (phraseMatch ? 1000 : 0) + matchedTokens;
            if (score > bestScore)
            {
                bestScore = score;
                bestText = paragraph.Text;
            }
        }

        if (strongMatches.Count > 0)
        {
            return strongMatches;
        }

        return string.IsNullOrWhiteSpace(bestText) ? Array.Empty<string>() : new[] { bestText };
    }

    private sealed record DocumentEntry(string RelativePath, string NormalizedText, IReadOnlyList<ParagraphEntry> Paragraphs);

    private sealed record ParagraphEntry(string Text, string NormalizedText);

    private sealed record OrderedMatch(int DocumentId, string RelativePath, bool PhraseMatch);

    private sealed record DocumentIndex(
        IReadOnlyList<DocumentEntry> Documents,
        Dictionary<string, HashSet<int>> TokenToDocuments)
    {
        public static readonly DocumentIndex Empty = new(
            new List<DocumentEntry>(),
            new Dictionary<string, HashSet<int>>(StringComparer.Ordinal));
    }
}
