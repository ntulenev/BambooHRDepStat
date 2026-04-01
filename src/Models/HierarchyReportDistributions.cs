namespace Models;

/// <summary>
/// People distributions for the report.
/// </summary>
public sealed class HierarchyReportDistributions
{
    /// <summary>
    /// Creates distribution content.
    /// </summary>
    public HierarchyReportDistributions(
        IReadOnlyDictionary<string, int> locationCounts,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts,
        IReadOnlyDictionary<string, int> ageCounts,
        IReadOnlyDictionary<string, int> tenureCounts)
    {
        ArgumentNullException.ThrowIfNull(locationCounts);
        ArgumentNullException.ThrowIfNull(countryCityCounts);
        ArgumentNullException.ThrowIfNull(ageCounts);
        ArgumentNullException.ThrowIfNull(tenureCounts);

        LocationCounts = locationCounts;
        CountryCityCounts = countryCityCounts;
        AgeCounts = ageCounts;
        TenureCounts = tenureCounts;
    }

    /// <summary>
    /// Gets people counts grouped by location.
    /// </summary>
    public IReadOnlyDictionary<string, int> LocationCounts { get; }

    /// <summary>
    /// Gets city counts grouped by country.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> CountryCityCounts { get; }

    /// <summary>
    /// Gets people counts grouped by age.
    /// </summary>
    public IReadOnlyDictionary<string, int> AgeCounts { get; }

    /// <summary>
    /// Gets people counts grouped by tenure.
    /// </summary>
    public IReadOnlyDictionary<string, int> TenureCounts { get; }
}
