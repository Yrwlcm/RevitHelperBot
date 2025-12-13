using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RevitHelperBot.Application.Documents;
using RevitHelperBot.Application.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace RevitHelperBot.Application.Tests;

public class DocumentSearchResultFormatterTests
{
    [Test]
    public void Format_Ok_OutputsFolderHierarchy()
    {
        var options = OptionsFactory.Create(new DocumentsOptions { RootPath = "data/docs" });
        var formatter = new DocumentSearchResultFormatter(options);

        var result = new DocumentSearchResult(
            "q",
            DocumentSearchStatus.Ok,
            new List<DocumentSearchHit>
            {
                new("Folder/Sub/File1.docx", false, new[] { "Para 1" }),
                new("Folder/File2.docx", false, new[] { "Para 2" }),
                new("Other.docx", false, new[] { "Root para" })
            },
            3,
            false,
            "root",
            3,
            null);

        var text = formatter.Format(result).ReplaceLineEndings("\n");

        text.Should().Contain("Folder");
        text.Should().Contain("Sub");
        text.Should().Contain("- File1.docx");
        text.Should().Contain("Para 1");
        text.Should().Contain("- File2.docx");
        text.Should().Contain("Para 2");
        text.Should().Contain("- Other.docx");
        text.Should().Contain("Root para");
    }

    [Test]
    public void Format_QueryTooShort_UsesConfiguredMinLength()
    {
        var options = OptionsFactory.Create(new DocumentsOptions { MinQueryLength = 5 });
        var formatter = new DocumentSearchResultFormatter(options);

        var result = new DocumentSearchResult(
            "q",
            DocumentSearchStatus.QueryTooShort,
            Array.Empty<DocumentSearchHit>(),
            0,
            false,
            "root",
            0,
            null);

        formatter.Format(result).Should().Contain("5");
    }
}
