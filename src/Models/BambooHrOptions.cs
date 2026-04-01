namespace Models;

/// <summary>
/// BambooHR connection settings.
/// </summary>
public sealed class BambooHrOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BambooHR";

    /// <summary>
    /// Gets or sets BambooHR company subdomain.
    /// </summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets BambooHR API token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets root employee identifier.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the number of days used to highlight recent hires in the report.
    /// </summary>
    public int RecentHirePeriodDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets HTML report output options.
    /// </summary>
    public HtmlReportOptions Html { get; set; } = new();

    /// <summary>
    /// Gets or sets PDF report output options.
    /// </summary>
    public PdfReportOptions Pdf { get; set; } = new();

    /// <summary>
    /// Validates configuration values.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Organization))
        {
            throw new InvalidOperationException(
                $"{SectionName}:Organization is required.");
        }

        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new InvalidOperationException($"{SectionName}:Token is required.");
        }

        if (EmployeeId <= 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:EmployeeId must be greater than zero.");
        }

        if (RecentHirePeriodDays <= 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:RecentHirePeriodDays must be greater than zero.");
        }
    }
}
