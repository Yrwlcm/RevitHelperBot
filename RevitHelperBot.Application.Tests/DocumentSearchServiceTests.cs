using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RevitHelperBot.Application.Tests;

public class DocumentSearchServiceTests
{
    [Test]
    public async Task SearchAsync_ReturnsMatchingDocuments_ByTokens()
    {
        var repository = new FakeRepository(new Dictionary<string, string>
        {
            ["A/Регламент.docx"] = "Регламент ТЗ на БИМ",
            ["B/Sub/Приложение.docx"] = "Приложение к ТЗ"
        });

        var options = OptionsFactory.Create(new DocumentsOptions
        {
            RootPath = repository.RootPath,
            MinQueryLength = 2,
            MinTokenLength = 2,
            MaxResults = 50,
            MaxDegreeOfParallelism = 1
        });

        var service = new DocumentSearchService(
            repository,
            new PlainTextExtractor(),
            options,
            NullLogger<DocumentSearchService>.Instance);

        var result = await service.SearchAsync("ТЗ БИМ", CancellationToken.None);

        result.Status.Should().Be(DocumentSearchStatus.Ok);
        result.TotalFound.Should().Be(1);
        result.Hits.Should().ContainSingle();
        result.Hits[0].RelativePath.Should().Be("A/Регламент.docx");
        result.Hits[0].Contexts.Should().ContainSingle();
        result.Hits[0].Contexts[0].Should().Be("Регламент ТЗ на БИМ");
    }

    [Test]
    public async Task SearchAsync_ReturnsParagraphContext()
    {
        var repository = new FakeRepository(new Dictionary<string, string>
        {
            ["Doc.docx"] = "Первый абзац\nВторой абзац ТЗ БИМ\nТретий абзац"
        });

        var options = OptionsFactory.Create(new DocumentsOptions
        {
            RootPath = repository.RootPath,
            MinQueryLength = 2,
            MinTokenLength = 2,
            MaxResults = 50,
            MaxDegreeOfParallelism = 1
        });

        var service = new DocumentSearchService(
            repository,
            new PlainTextExtractor(),
            options,
            NullLogger<DocumentSearchService>.Instance);

        var result = await service.SearchAsync("ТЗ БИМ", CancellationToken.None);

        result.Hits.Should().ContainSingle();
        result.Hits[0].Contexts.Should().ContainSingle();
        result.Hits[0].Contexts[0].Should().Be("Второй абзац ТЗ БИМ");
    }

    [Test]
    public async Task SearchAsync_ReturnsAllMatchingParagraphs_ForSingleTokenQuery()
    {
        var repository = new FakeRepository(new Dictionary<string, string>
        {
            ["Doc.docx"] = "Первый абзац ТЗ\nВторой абзац ТЗ\nТретий абзац"
        });

        var options = OptionsFactory.Create(new DocumentsOptions
        {
            RootPath = repository.RootPath,
            MinQueryLength = 2,
            MinTokenLength = 2,
            MaxResults = 50,
            MaxDegreeOfParallelism = 1
        });

        var service = new DocumentSearchService(
            repository,
            new PlainTextExtractor(),
            options,
            NullLogger<DocumentSearchService>.Instance);

        var result = await service.SearchAsync("ТЗ", CancellationToken.None);

        result.Hits.Should().ContainSingle();
        result.Hits[0].Contexts.Should().BeEquivalentTo(new[] { "Первый абзац ТЗ", "Второй абзац ТЗ" }, o => o.WithStrictOrdering());
    }

    [Test]
    public async Task SearchAsync_ReturnsQueryTooShort_WhenTooShort()
    {
        var repository = new FakeRepository(new Dictionary<string, string>
        {
            ["Doc.docx"] = "Any text"
        });

        var options = OptionsFactory.Create(new DocumentsOptions
        {
            RootPath = repository.RootPath,
            MinQueryLength = 3,
            MinTokenLength = 2,
            MaxResults = 50,
            MaxDegreeOfParallelism = 1
        });

        var service = new DocumentSearchService(
            repository,
            new PlainTextExtractor(),
            options,
            NullLogger<DocumentSearchService>.Instance);

        var result = await service.SearchAsync("ТЗ", CancellationToken.None);

        result.Status.Should().Be(DocumentSearchStatus.QueryTooShort);
    }

    [Test]
    public async Task ReloadAsync_RebuildsIndex()
    {
        var repository = new FakeRepository(new Dictionary<string, string>
        {
            ["A/Doc.docx"] = "old text"
        });

        var options = OptionsFactory.Create(new DocumentsOptions
        {
            RootPath = repository.RootPath,
            MinQueryLength = 3,
            MinTokenLength = 2,
            MaxResults = 50,
            MaxDegreeOfParallelism = 1
        });

        var service = new DocumentSearchService(
            repository,
            new PlainTextExtractor(),
            options,
            NullLogger<DocumentSearchService>.Instance);

        (await service.SearchAsync("old", CancellationToken.None)).TotalFound.Should().Be(1);

        repository.SetFile("A/Doc.docx", "new text");
        await service.ReloadAsync(CancellationToken.None);

        (await service.SearchAsync("old", CancellationToken.None)).TotalFound.Should().Be(0);
        (await service.SearchAsync("new", CancellationToken.None)).TotalFound.Should().Be(1);
    }

    private sealed class FakeRepository : IWordDocumentsRepository
    {
        private readonly Dictionary<string, byte[]> files;

        public FakeRepository(Dictionary<string, string> files)
        {
            this.files = files.ToDictionary(kvp => kvp.Key, kvp => Encoding.UTF8.GetBytes(kvp.Value));
        }

        public string RootPath => "root";

        public Task<IReadOnlyList<WordDocumentFile>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<WordDocumentFile> list = files
                .Select(kvp => new WordDocumentFile(kvp.Key, kvp.Key, DateTimeOffset.UtcNow, kvp.Value.Length))
                .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult(list);
        }

        public Task<Stream> OpenReadAsync(WordDocumentFile file, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = files[file.RelativePath];
            Stream stream = new MemoryStream(bytes);
            return Task.FromResult(stream);
        }

        public void SetFile(string relativePath, string text)
        {
            files[relativePath] = Encoding.UTF8.GetBytes(text);
        }
    }

    private sealed class PlainTextExtractor : IDocxTextExtractor
    {
        public async Task<string> ExtractTextAsync(Stream docxStream, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(docxStream, Encoding.UTF8, true, leaveOpen: false);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
