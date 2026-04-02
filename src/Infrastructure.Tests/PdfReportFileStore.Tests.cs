using FluentAssertions;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure.Tests;

[Collection("Pdf stack")]
public sealed class PdfReportFileStoreTests
{
    [Fact(DisplayName = "The PDF file store creates the output directory and writes the generated document bytes.")]
    [Trait("Category", "Integration")]
    public void SaveShouldCreateDirectoryAndWritePdfBytes()
    {
        // Arrange
        EnsureQuestPdfLicense();
        var store = new PdfReportFileStore();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "BambooHRDepStat.Tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputDirectory, "report.pdf");
        var document = Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                _ = page.Content().Text("PDF document");
            });
        });

        try
        {
            // Act
            store.Save(outputPath, document);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
            new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static void EnsureQuestPdfLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
