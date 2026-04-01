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
        HierarchyReportOverview overview,
        HierarchyReportHierarchy hierarchy,
        HierarchyReportSummaries summaries,
        HierarchyReportDistributions distributions)
    {
        ArgumentNullException.ThrowIfNull(overview);
        ArgumentNullException.ThrowIfNull(hierarchy);
        ArgumentNullException.ThrowIfNull(summaries);
        ArgumentNullException.ThrowIfNull(distributions);

        Overview = overview;
        Hierarchy = hierarchy;
        Summaries = summaries;
        Distributions = distributions;
    }

    /// <summary>
    /// Gets report overview.
    /// </summary>
    public HierarchyReportOverview Overview { get; }

    /// <summary>
    /// Gets hierarchy rows and holidays.
    /// </summary>
    public HierarchyReportHierarchy Hierarchy { get; }

    /// <summary>
    /// Gets summary highlights and teams.
    /// </summary>
    public HierarchyReportSummaries Summaries { get; }

    /// <summary>
    /// Gets people distributions.
    /// </summary>
    public HierarchyReportDistributions Distributions { get; }

    /// <summary>
    /// Gets report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt => Overview.GeneratedAt;

    /// <summary>
    /// Gets report availability window.
    /// </summary>
    public AvailabilityWindow AvailabilityWindow => Overview.AvailabilityWindow;

    /// <summary>
    /// Gets root employee display name.
    /// </summary>
    public string RootEmployeeName => Overview.RootEmployeeName;

    /// <summary>
    /// Gets hierarchy relationship field.
    /// </summary>
    public HierarchyRelationshipField RelationshipField => Overview.RelationshipField;

    /// <summary>
    /// Gets holidays returned by BambooHR for the availability window.
    /// </summary>
    public IReadOnlyList<HolidayReportItem> Holidays => Hierarchy.Holidays;

    /// <summary>
    /// Gets flattened hierarchy rows.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> Rows => Hierarchy.Rows;

    /// <summary>
    /// Gets employees whose employment start date falls within the configured recent-hire period.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> RecentHires => Summaries.RecentHires;

    /// <summary>
    /// Gets the configured recent-hire period length in days.
    /// </summary>
    public int RecentHirePeriodDays => Summaries.RecentHirePeriodDays;

    /// <summary>
    /// Gets manager-led teams built from leaf direct reports.
    /// </summary>
    public IReadOnlyList<HierarchyTeam> Teams => Summaries.Teams;

    /// <summary>
    /// Gets people counts grouped by location.
    /// </summary>
    public IReadOnlyDictionary<string, int> LocationCounts => Distributions.LocationCounts;

    /// <summary>
    /// Gets city counts grouped by country.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> CountryCityCounts => Distributions.CountryCityCounts;

    /// <summary>
    /// Gets people counts grouped by age.
    /// </summary>
    public IReadOnlyDictionary<string, int> AgeCounts => Distributions.AgeCounts;

    /// <summary>
    /// Gets people counts grouped by tenure.
    /// </summary>
    public IReadOnlyDictionary<string, int> TenureCounts => Distributions.TenureCounts;
}
