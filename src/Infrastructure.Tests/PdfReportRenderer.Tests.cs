using FluentAssertions;

using Models;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure.Tests;

[Collection("Pdf stack")]
public sealed class PdfReportRendererTests
{
    [Fact(DisplayName = "The PDF renderer does nothing when PDF output is disabled.")]
    [Trait("Category", "Unit")]
    public void RenderShouldSkipWhenDisabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = CreateOutputDirectory();
        var configuredPath = Path.Combine(outputDirectory, "report.pdf");
        var renderer = CreateRenderer(
            enabled: false,
            configuredPath);

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(configuredPath).Should().BeFalse();
            Directory.Exists(outputDirectory).Should().BeFalse();
        }
        finally
        {
            Cleanup(outputDirectory);
        }
    }

    [Fact(DisplayName = "The PDF renderer writes a timestamped PDF file when PDF output is enabled.")]
    [Trait("Category", "Integration")]
    public void RenderShouldWriteTimestampedPdfWhenEnabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = CreateOutputDirectory();
        var configuredPath = Path.Combine(outputDirectory, "report.pdf");
        var expectedOutputPath = Path.Combine(outputDirectory, "report_20260401_093000.pdf");
        var renderer = CreateRenderer(
            enabled: true,
            configuredPath);

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(expectedOutputPath).Should().BeTrue();
            File.Exists(configuredPath).Should().BeFalse();
            new FileInfo(expectedOutputPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            Cleanup(outputDirectory);
        }
    }

    private static PdfReportRenderer CreateRenderer(
        bool enabled,
        string configuredPath)
    {
        EnsureQuestPdfLicense();

        var options = new PdfReportOptions
        {
            Enabled = enabled,
            OutputPath = configuredPath
        };
        var store = new PdfReportFileStore();
        var composer = new PdfContentComposer(new ReportPresentationFormatter());

        return new PdfReportRenderer(
            options,
            new FixedTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            store,
            composer);
    }

    private static void EnsureQuestPdfLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string CreateOutputDirectory()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "BambooHRDepStat.Tests",
            Guid.NewGuid().ToString("N"));
    }

    private static void Cleanup(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
