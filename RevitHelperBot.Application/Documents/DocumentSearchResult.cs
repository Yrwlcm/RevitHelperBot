namespace RevitHelperBot.Application.Documents;

public sealed record DocumentSearchResult(
    string Query,
    DocumentSearchStatus Status,
    IReadOnlyList<DocumentSearchHit> Hits,
    int TotalFound,
    bool IsTruncated,
    string RootPath,
    int IndexedDocumentCount,
    string? ErrorMessage);

