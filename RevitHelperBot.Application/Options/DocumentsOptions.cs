namespace RevitHelperBot.Application.Options;

public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    public string RootPath { get; init; } = "data/docs";

    public int MinQueryLength { get; init; } = 3;

    public int MinTokenLength { get; init; } = 2;

    public int MaxResults { get; init; } = 50;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

