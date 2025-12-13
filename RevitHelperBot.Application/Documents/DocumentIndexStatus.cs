namespace RevitHelperBot.Application.Documents;

public sealed record DocumentIndexStatus(
    string RootPath,
    bool IsReady,
    int DocumentCount,
    int FailedDocuments,
    DateTimeOffset? LastIndexedAtUtc,
    string? LastError);

