using Microsoft.Extensions.Options;
using RevitHelperBot.Application.Options;

namespace RevitHelperBot.Application.Documents;

public sealed class FileSystemWordDocumentsRepository : IWordDocumentsRepository
{
    public string RootPath { get; }

    public FileSystemWordDocumentsRepository(IOptions<DocumentsOptions> options)
    {
        RootPath = ResolveDirectoryPath(options.Value.RootPath);
    }

    public Task<IReadOnlyList<WordDocumentFile>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(RootPath))
        {
            return Task.FromResult<IReadOnlyList<WordDocumentFile>>(Array.Empty<WordDocumentFile>());
        }

        var files = Directory
            .EnumerateFiles(RootPath, "*.docx", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .Select(fileInfo =>
            {
                var relativePath = Path.GetRelativePath(RootPath, fileInfo.FullName);
                return new WordDocumentFile(
                    fileInfo.FullName,
                    NormalizeRelativePath(relativePath),
                    fileInfo.LastWriteTimeUtc,
                    fileInfo.Length);
            })
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<WordDocumentFile>>(files);
    }

    public Task<Stream> OpenReadAsync(WordDocumentFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = File.OpenRead(file.FullPath);
        return Task.FromResult(stream);
    }

    internal static string ResolveDirectoryPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var baseDirectoryCandidate = Path.Combine(AppContext.BaseDirectory, configuredPath);
        if (Directory.Exists(baseDirectoryCandidate))
        {
            return baseDirectoryCandidate;
        }

        var currentDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
        if (Directory.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        return baseDirectoryCandidate;
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
}

