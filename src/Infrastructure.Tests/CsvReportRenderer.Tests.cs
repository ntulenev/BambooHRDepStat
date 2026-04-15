using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class CsvReportRendererTests
{
    [Fact(DisplayName = "The CSV renderer does nothing when export is disabled.")]
    [Trait("Category", "Unit")]
    public void RenderShouldSkipWhenDisabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = CreateOutputDirectory();
        var configuredPath = Path.Combine(outputDirectory, "employees.csv");
        var renderer = new CsvReportRenderer(
            new ExportOptions
            {
                Enabled = false,
                OutputPath = configuredPath
            },
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            new CsvReportFileStore());

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

    [Fact(DisplayName = "The CSV renderer writes a timestamped export file when export is enabled.")]
    [Trait("Category", "Integration")]
    public void RenderShouldWriteTimestampedCsvWhenEnabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = CreateOutputDirectory();
        var configuredPath = Path.Combine(outputDirectory, "employees.csv");
        var expectedOutputPath = Path.Combine(outputDirectory, "employees_20260401_093000.csv");
        var renderer = new CsvReportRenderer(
            new ExportOptions
            {
                Enabled = true,
                OutputPath = configuredPath
            },
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            new CsvReportFileStore());

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(expectedOutputPath).Should().BeTrue();
            File.ReadAllText(expectedOutputPath).Should().Contain("Employee Name,Email,Team");
            File.ReadAllText(expectedOutputPath).Should().Contain("Alice & Smith,alice@example.com,Leadership Team");
            File.ReadAllText(expectedOutputPath).Should().Contain("Bob Jones,bob@example.com,ADF Processing Team");
        }
        finally
        {
            Cleanup(outputDirectory);
        }
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
