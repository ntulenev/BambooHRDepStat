using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class HtmlReportRendererTests
{
    [Fact(DisplayName = "The HTML renderer writes a timestamped report file when HTML output is enabled.")]
    [Trait("Category", "Integration")]
    public void RenderShouldWriteTimestampedHtmlFileWhenEnabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "BambooHRDepStat.Tests", Guid.NewGuid().ToString("N"));
        var configuredPath = Path.Combine(outputDirectory, "report.html");
        var localNow = new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero);
        var options = new HtmlReportOptions
        {
            Enabled = true,
            OpenInBrowser = false,
            OutputPath = configuredPath
        };
        var renderer = new HtmlReportRenderer(
            options,
            new FakeTimeProvider(localNow),
            new HtmlReportFileStore(),
            new HtmlContentComposer(new ReportPresentationFormatter()),
            new HtmlReportLauncher());
        var expectedOutputPath = Path.Combine(outputDirectory, "report_20260401_093000.html");

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(expectedOutputPath).Should().BeTrue();
            File.ReadAllText(expectedOutputPath).Should().Contain("Alice &amp; Smith");
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
