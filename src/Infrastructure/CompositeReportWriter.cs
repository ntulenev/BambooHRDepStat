using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// Writes the report to all configured output targets.
/// </summary>
public sealed class CompositeReportWriter : IReportWriter
{
    /// <summary>
    /// Creates report writer.
    /// </summary>
    public CompositeReportWriter(
        IConsoleReportRenderer consoleReportRenderer,
        IHtmlReportRenderer htmlReportRenderer,
        IPdfReportRenderer pdfReportRenderer)
    {
        ArgumentNullException.ThrowIfNull(consoleReportRenderer);
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);

        _consoleReportRenderer = consoleReportRenderer;
        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
    }

    /// <inheritdoc/>
    public void Write(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        _consoleReportRenderer.Render(report);
        _htmlReportRenderer.Render(report);
        _pdfReportRenderer.Render(report);
    }

    private readonly IConsoleReportRenderer _consoleReportRenderer;
    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
}
