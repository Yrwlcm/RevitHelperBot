namespace RevitHelperBot.Application.Documents;

public interface IDocxTextExtractor
{
    Task<string> ExtractTextAsync(Stream docxStream, CancellationToken cancellationToken);
}

