using FluentAssertions;

namespace Infrastructure.Tests;

public sealed class HtmlReportFileStoreTests
{
    [Fact(DisplayName = "The file store creates the output directory and writes the HTML document using UTF-8.")]
    [Trait("Category", "Integration")]
    public void SaveShouldCreateDirectoryAndWriteHtml()
    {
        // Arrange
        var store = new HtmlReportFileStore();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "BambooHRDepStat.Tests", Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputDirectory, "report.html");
        const string html = "<html><body>report</body></html>";

        try
        {
            // Act
            store.Save(outputPath, html);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
            File.ReadAllText(outputPath).Should().Be(html);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
