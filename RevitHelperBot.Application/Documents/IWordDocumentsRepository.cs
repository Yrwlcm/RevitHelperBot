namespace RevitHelperBot.Application.Documents;

public interface IWordDocumentsRepository
{
    string RootPath { get; }

    Task<IReadOnlyList<WordDocumentFile>> ListAsync(CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(WordDocumentFile file, CancellationToken cancellationToken);
}

