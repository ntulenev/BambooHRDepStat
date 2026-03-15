using Abstractions;

using Models;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// HTML implementation for hierarchy report rendering.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    private static readonly string DefaultOutputPath =
        Path.Combine("reports", "bamboohr-hierarchy-report.html");

    private readonly BambooHrOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly HtmlReportFileStore _htmlReportFileStore;
    private readonly HtmlContentComposer _htmlContentComposer;
    private readonly HtmlReportLauncher _htmlReportLauncher;

    /// <summary>
    /// Creates HTML renderer.
    /// </summary>
    public HtmlReportRenderer(
        BambooHrOptions options,
        TimeProvider timeProvider,
        HtmlReportFileStore htmlReportFileStore,
        HtmlContentComposer htmlContentComposer,
        HtmlReportLauncher htmlReportLauncher)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(htmlReportFileStore);
        ArgumentNullException.ThrowIfNull(htmlContentComposer);
        ArgumentNullException.ThrowIfNull(htmlReportLauncher);

        _options = options;
        _timeProvider = timeProvider;
        _htmlReportFileStore = htmlReportFileStore;
        _htmlContentComposer = htmlContentComposer;
        _htmlReportLauncher = htmlReportLauncher;
    }

    /// <inheritdoc/>
    public void Render(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!_options.Html.Enabled)
        {
            return;
        }

        var outputPath = ReportOutputPathResolver.Resolve(
            _options.Html.OutputPath,
            DefaultOutputPath,
            _timeProvider);
        var html = _htmlContentComposer.Compose(report);

        _htmlReportFileStore.Save(outputPath, html);
        AnsiConsole.MarkupLine($"[grey]HTML report saved:[/] {Markup.Escape(outputPath)}");

        if (!_options.Html.OpenInBrowser)
        {
            return;
        }

        _htmlReportLauncher.Open(outputPath);
        AnsiConsole.MarkupLine("[grey]HTML report opened in default browser.[/]");
    }
}
