using System.Globalization;

using Abstractions;

using Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

using Spectre.Console;

using QLicenseType = QuestPDF.Infrastructure.LicenseType;

namespace Infrastructure;

/// <summary>
/// QuestPDF implementation for hierarchy report rendering.
/// </summary>
public sealed class PdfReportRenderer : IPdfReportRenderer
{
    private static readonly string DefaultOutputPath =
        Path.Combine("reports", "bamboohr-hierarchy-report.pdf");

    private readonly BambooHrOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly PdfReportFileStore _pdfReportFileStore;
    private readonly PdfContentComposer _pdfContentComposer;

    /// <summary>
    /// Creates PDF renderer.
    /// </summary>
    public PdfReportRenderer(
        BambooHrOptions options,
        TimeProvider timeProvider,
        PdfReportFileStore pdfReportFileStore,
        PdfContentComposer pdfContentComposer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(pdfReportFileStore);
        ArgumentNullException.ThrowIfNull(pdfContentComposer);

        _options = options;
        _timeProvider = timeProvider;
        _pdfReportFileStore = pdfReportFileStore;
        _pdfContentComposer = pdfContentComposer;
    }

    /// <inheritdoc/>
    public void Render(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!_options.Pdf.Enabled)
        {
            return;
        }

        var outputPath = ReportOutputPathResolver.Resolve(
            _options.Pdf.OutputPath,
            DefaultOutputPath,
            _timeProvider);

        QuestPDF.Settings.License = QLicenseType.Community;

        var document = Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(static style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Spacing(3);
                    _ = column.Item().Text("BambooHR Hierarchy Report").Bold().FontSize(18);
                    _ = column.Item().Text("Root: " + report.RootEmployeeName);
                    _ = column.Item().Text(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss zzz}"));
                    _ = column.Item().Text(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Availability window: {report.AvailabilityWindow.Start:yyyy-MM-dd} to {report.AvailabilityWindow.End:yyyy-MM-dd}"));
                    _ = column.Item().Text(
                        $"Hierarchy link: {report.RelationshipField.DisplayName} ({(report.RelationshipField.UsesEmployeeId ? "employee ID" : "employee name")})");
                });

                page.Content()
                    .PaddingTop(10)
                    .Column(column => _pdfContentComposer.ComposeContent(column, report));

                page.Footer().AlignRight().Text(text =>
                {
                    _ = text.Span("Page ");
                    _ = text.CurrentPageNumber();
                    _ = text.Span(" / ");
                    _ = text.TotalPages();
                });
            });
        });

        _pdfReportFileStore.Save(outputPath, document);
        AnsiConsole.MarkupLine($"[grey]PDF report saved:[/] {Markup.Escape(outputPath)}");
    }
}
