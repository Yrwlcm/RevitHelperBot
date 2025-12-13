using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RevitHelperBot.Application.Documents;

namespace RevitHelperBot.Application.Tests;

public class DocxTextExtractorTests
{
    [Test]
    public async Task ExtractTextAsync_ReadsTextFromDocumentXml()
    {
        var docxBytes = CreateDocxWithDocumentXml("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p><w:r><w:t>Привет</w:t></w:r></w:p>
                <w:p><w:r><w:t>мир</w:t></w:r></w:p>
              </w:body>
            </w:document>
            """);

        await using var stream = new MemoryStream(docxBytes);
        var extractor = new DocxTextExtractor();

        var text = await extractor.ExtractTextAsync(stream, CancellationToken.None);

        text.ReplaceLineEndings("\n").Should().Be("Привет\nмир");
    }

    [Test]
    public async Task ExtractTextAsync_HandlesTabsAndLineBreaks()
    {
        var docxBytes = CreateDocxWithDocumentXml("""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p>
                  <w:r><w:t>A</w:t></w:r>
                  <w:r><w:tab/></w:r>
                  <w:r><w:t>B</w:t></w:r>
                  <w:r><w:br/></w:r>
                  <w:r><w:t>C</w:t></w:r>
                </w:p>
              </w:body>
            </w:document>
            """);

        await using var stream = new MemoryStream(docxBytes);
        var extractor = new DocxTextExtractor();

        var text = await extractor.ExtractTextAsync(stream, CancellationToken.None);

        text.ReplaceLineEndings("\n").Should().Be("A\tB\nC");
    }

    private static byte[] CreateDocxWithDocumentXml(string documentXml)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(documentXml);
            entryStream.Write(bytes, 0, bytes.Length);
        }

        return memory.ToArray();
    }
}
