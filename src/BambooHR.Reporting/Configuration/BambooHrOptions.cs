using Models;

namespace BambooHR.Reporting.Configuration;

/// <summary>
/// Bindable BambooHR application configuration.
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
    /// Gets or sets how many days ahead availability should be checked, starting from today.
    /// </summary>
    public int AvailabilityLookaheadDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the number of days used to highlight recent hires in the report.
    /// </summary>
    public int RecentHirePeriodDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets explicit holiday to country mappings used for availability.
    /// </summary>
    public Dictionary<string, string[]> HolidayCountryMappings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets HTML report output options.
    /// </summary>
    public HtmlReportOptions Html { get; set; } = new();

    /// <summary>
    /// Gets or sets PDF report output options.
    /// </summary>
    public PdfReportOptions Pdf { get; set; } = new();

    /// <summary>
    /// Gets or sets CSV export output options.
    /// </summary>
    public ExportOptions Export { get; set; } = new();

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

        if (AvailabilityLookaheadDays < 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:AvailabilityLookaheadDays must be greater than or equal to zero.");
        }

        if (RecentHirePeriodDays <= 0)
        {
            throw new InvalidOperationException(
                $"{SectionName}:RecentHirePeriodDays must be greater than zero.");
        }

        if (Export.Enabled && string.IsNullOrWhiteSpace(Export.OutputPath))
        {
            throw new InvalidOperationException(
                $"{SectionName}:Export:OutputPath is required when export is enabled.");
        }

        foreach (var mapping in HolidayCountryMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Key))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:HolidayCountryMappings keys must be non-empty.");
            }

            if (mapping.Value is null)
            {
                throw new InvalidOperationException(
                    $"{SectionName}:HolidayCountryMappings '{mapping.Key}' must contain a country array.");
            }

            if (mapping.Value.Any(country => string.IsNullOrWhiteSpace(country)))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:HolidayCountryMappings '{mapping.Key}' contains an empty country value.");
            }
        }
    }
}
