using FluentAssertions;

using Moq;

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
        var reportFileLauncher = new Mock<Abstractions.IReportFileLauncher>(MockBehavior.Strict);
        var renderer = new CsvReportRenderer(
            new ExportOptions
            {
                Enabled = false,
                OutputPath = configuredPath
            },
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            new CsvReportFileStore(),
            reportFileLauncher.Object);

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(configuredPath).Should().BeFalse();
            Directory.Exists(outputDirectory).Should().BeFalse();
            reportFileLauncher.Verify(
                launcher => launcher.Open(It.IsAny<string>()),
                Times.Never);
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
        var reportFileLauncher = new Mock<Abstractions.IReportFileLauncher>(MockBehavior.Strict);
        var renderer = new CsvReportRenderer(
            new ExportOptions
            {
                Enabled = true,
                OutputPath = configuredPath
            },
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            new CsvReportFileStore(),
            reportFileLauncher.Object);

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            File.Exists(expectedOutputPath).Should().BeTrue();
            var csv = File.ReadAllText(expectedOutputPath);
            csv.Should().Contain("Employee Name,Email,Phone Numbers,Team");
            csv.Should().Contain("Alice & Smith,alice@example.com,Mobile Phone: +49 111 1111,Leadership Team");
            csv.Should().Contain("Bob Jones,bob@example.com,Mobile Phone: +49 222 2222 | Work Phone: +49 333 3333,ADF Processing Team");
            reportFileLauncher.Verify(
                launcher => launcher.Open(It.IsAny<string>()),
                Times.Never);
        }
        finally
        {
            Cleanup(outputDirectory);
        }
    }

    [Fact(DisplayName = "The CSV renderer opens the generated export when process launch is enabled.")]
    [Trait("Category", "Unit")]
    public void RenderShouldOpenCsvWhenProcessLaunchIsEnabled()
    {
        // Arrange
        var report = ReportTestData.CreateReport();
        var outputDirectory = CreateOutputDirectory();
        var configuredPath = Path.Combine(outputDirectory, "employees.csv");
        var expectedOutputPath = Path.Combine(outputDirectory, "employees_20260401_093000.csv");
        var reportFileLauncher = new Mock<Abstractions.IReportFileLauncher>(MockBehavior.Strict);
        reportFileLauncher
            .Setup(launcher => launcher.Open(expectedOutputPath));
        var renderer = new CsvReportRenderer(
            new ExportOptions
            {
                Enabled = true,
                OpenByProcess = true,
                OutputPath = configuredPath
            },
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)),
            new CsvReportFileStore(),
            reportFileLauncher.Object);

        try
        {
            // Act
            renderer.Render(report);

            // Assert
            reportFileLauncher.Verify(launcher => launcher.Open(expectedOutputPath), Times.Once);
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
