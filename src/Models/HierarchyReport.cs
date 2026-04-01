namespace Models;

/// <summary>
/// Completed hierarchy report.
/// </summary>
public sealed class HierarchyReport
{
    /// <summary>
    /// Creates hierarchy report.
    /// </summary>
    public HierarchyReport(
        DateTimeOffset generatedAt,
        AvailabilityWindow availabilityWindow,
        string rootEmployeeName,
        HierarchyRelationshipField relationshipField,
        IReadOnlyList<HolidayReportItem> holidays,
        IReadOnlyList<HierarchyReportRow> rows,
        IReadOnlyList<HierarchyReportRow> recentHires,
        int recentHirePeriodDays,
        IReadOnlyList<HierarchyTeam> teams,
        IReadOnlyDictionary<string, int> locationCounts,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts,
        IReadOnlyDictionary<string, int> ageCounts,
        IReadOnlyDictionary<string, int> tenureCounts)
    {
        ArgumentNullException.ThrowIfNull(availabilityWindow);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootEmployeeName);
        ArgumentNullException.ThrowIfNull(relationshipField);
        ArgumentNullException.ThrowIfNull(holidays);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(recentHires);
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentNullException.ThrowIfNull(locationCounts);
        ArgumentNullException.ThrowIfNull(countryCityCounts);
        ArgumentNullException.ThrowIfNull(ageCounts);
        ArgumentNullException.ThrowIfNull(tenureCounts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recentHirePeriodDays);

        GeneratedAt = generatedAt;
        AvailabilityWindow = availabilityWindow;
        RootEmployeeName = rootEmployeeName;
        RelationshipField = relationshipField;
        Holidays = holidays;
        Rows = rows;
        RecentHires = recentHires;
        RecentHirePeriodDays = recentHirePeriodDays;
        Teams = teams;
        LocationCounts = locationCounts;
        CountryCityCounts = countryCityCounts;
        AgeCounts = ageCounts;
        TenureCounts = tenureCounts;
    }

    /// <summary>
    /// Gets report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Gets report availability window.
    /// </summary>
    public AvailabilityWindow AvailabilityWindow { get; }

    /// <summary>
    /// Gets root employee display name.
    /// </summary>
    public string RootEmployeeName { get; }

    /// <summary>
    /// Gets hierarchy relationship field.
    /// </summary>
    public HierarchyRelationshipField RelationshipField { get; }

    /// <summary>
    /// Gets holidays returned by BambooHR for the availability window.
    /// </summary>
    public IReadOnlyList<HolidayReportItem> Holidays { get; }

    /// <summary>
    /// Gets flattened hierarchy rows.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> Rows { get; }

    /// <summary>
    /// Gets employees whose employment start date falls within the configured recent-hire period.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> RecentHires { get; }

    /// <summary>
    /// Gets the configured recent-hire period length in days.
    /// </summary>
    public int RecentHirePeriodDays { get; }

    /// <summary>
    /// Gets manager-led teams built from leaf direct reports.
    /// </summary>
    public IReadOnlyList<HierarchyTeam> Teams { get; }

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
