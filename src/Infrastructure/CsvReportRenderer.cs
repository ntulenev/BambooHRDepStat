using Abstractions;

using Models;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// CSV implementation for employee export rendering.
/// </summary>
public sealed class CsvReportRenderer : ICsvReportRenderer
{
    /// <summary>
    /// Creates CSV renderer.
    /// </summary>
    public CsvReportRenderer(
        ExportOptions options,
        TimeProvider timeProvider,
        CsvReportFileStore csvReportFileStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(csvReportFileStore);

        _options = options;
        _timeProvider = timeProvider;
        _csvReportFileStore = csvReportFileStore;
    }

    /// <inheritdoc/>
    public void Render(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!_options.Enabled)
        {
            return;
        }

        var outputPath = ReportOutputPathResolver.Resolve(
            _options.OutputPath,
            DefaultOutputPath,
            _timeProvider);

        _csvReportFileStore.Save(outputPath, report.Rows);
        AnsiConsole.MarkupLine($"[grey]CSV export saved:[/] {Markup.Escape(outputPath)}");
    }

    private static readonly string DefaultOutputPath =
        Path.Combine("reports", "bamboohr-employee-export.csv");

    private readonly ExportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly CsvReportFileStore _csvReportFileStore;
}
