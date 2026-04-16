using Abstractions;

using Models;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// HTML implementation for hierarchy report rendering.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    /// <summary>
    /// Creates HTML renderer.
    /// </summary>
    public HtmlReportRenderer(
        HtmlReportOptions options,
        TimeProvider timeProvider,
        HtmlReportFileStore htmlReportFileStore,
        HtmlContentComposer htmlContentComposer,
        IReportFileLauncher reportFileLauncher)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(htmlReportFileStore);
        ArgumentNullException.ThrowIfNull(htmlContentComposer);
        ArgumentNullException.ThrowIfNull(reportFileLauncher);

        _options = options;
        _timeProvider = timeProvider;
        _htmlReportFileStore = htmlReportFileStore;
        _htmlContentComposer = htmlContentComposer;
        _reportFileLauncher = reportFileLauncher;
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
        var html = _htmlContentComposer.Compose(report);

        _htmlReportFileStore.Save(outputPath, html);
        AnsiConsole.MarkupLine($"[grey]HTML report saved:[/] {Markup.Escape(outputPath)}");

        if (!_options.OpenInBrowser)
        {
            return;
        }

        _reportFileLauncher.Open(outputPath);
        AnsiConsole.MarkupLine("[grey]HTML report opened in default browser.[/]");
    }

    private static readonly string DefaultOutputPath =
        Path.Combine("reports", "bamboohr-hierarchy-report.html");

    private readonly HtmlReportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly HtmlReportFileStore _htmlReportFileStore;
    private readonly HtmlContentComposer _htmlContentComposer;
    private readonly IReportFileLauncher _reportFileLauncher;
}
