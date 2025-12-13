using System.IO.Compression;
using System.Text;
using System.Xml;

namespace RevitHelperBot.Application.Documents;

public sealed class DocxTextExtractor : IDocxTextExtractor
{
    private const string WordprocessingMlNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public async Task<string> ExtractTextAsync(Stream docxStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(docxStream);
        cancellationToken.ThrowIfCancellationRequested();

        if (!docxStream.CanSeek)
        {
            var buffered = new MemoryStream();
            await docxStream.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;
            docxStream = buffered;
        }

        using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read, leaveOpen: true);
        var builder = new StringBuilder();

        AppendPartTextIfPresent(archive, "word/document.xml", builder, cancellationToken);
        AppendPartTextIfPresent(archive, "word/footnotes.xml", builder, cancellationToken);
        AppendPartTextIfPresent(archive, "word/endnotes.xml", builder, cancellationToken);

        return builder
            .ToString()
            .ReplaceLineEndings("\n")
            .Trim();
    }

    private static void AppendPartTextIfPresent(
        ZipArchive archive,
        string entryName,
        StringBuilder builder,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            return;
        }

        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        });

        AppendWordXmlText(reader, builder, cancellationToken);

        if (builder.Length > 0 && builder[^1] != '\n')
        {
            builder.Append('\n');
        }
    }

    private static void AppendWordXmlText(XmlReader reader, StringBuilder builder, CancellationToken cancellationToken)
    {
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType == XmlNodeType.Element)
            {
                if (IsWordElement(reader, "t"))
                {
                    var text = reader.ReadElementContentAsString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        builder.Append(text);
                    }

                    continue;
                }

                if (IsWordElement(reader, "tab"))
                {
                    builder.Append('\t');
                    continue;
                }

                if (IsWordElement(reader, "br") || IsWordElement(reader, "cr"))
                {
                    builder.Append('\n');
                    continue;
                }

                continue;
            }

            if (reader.NodeType == XmlNodeType.EndElement && IsWordElement(reader, "p"))
            {
                if (builder.Length > 0 && builder[^1] != '\n')
                {
                    builder.Append('\n');
                }
            }
        }
    }

    private static bool IsWordElement(XmlReader reader, string localName) =>
        reader.LocalName == localName && reader.NamespaceURI == WordprocessingMlNamespace;
}

