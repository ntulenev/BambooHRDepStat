namespace Models;

/// <summary>
/// HTML report output settings.
/// </summary>
public sealed class HtmlReportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether HTML report generation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the output path for the HTML report.
    /// </summary>
    public string OutputPath { get; set; } = Path.Combine("reports", "bamboohr-hierarchy-report.html");
}
