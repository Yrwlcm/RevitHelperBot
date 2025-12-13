namespace RevitHelperBot.Application.Documents;

public interface IDocumentSearchService
{
    Task<DocumentSearchResult> SearchAsync(string query, CancellationToken cancellationToken);

    Task ReloadAsync(CancellationToken cancellationToken);

    DocumentIndexStatus GetStatus();
}

