namespace RevitHelperBot.Application.Documents;

public sealed record WordDocumentFile(
    string FullPath,
    string RelativePath,
    DateTimeOffset LastWriteTimeUtc,
    long LengthBytes);

