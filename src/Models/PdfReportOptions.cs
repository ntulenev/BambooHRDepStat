namespace Models;

/// <summary>
/// PDF report output settings.
/// </summary>
public sealed class PdfReportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether PDF report generation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the output path for the PDF report.
    /// </summary>
    public string OutputPath { get; set; } = Path.Combine("reports", "bamboohr-hierarchy-report.pdf");
}
