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
        WorkWeek workWeek,
        string rootEmployeeName,
        HierarchyRelationshipField relationshipField,
        IReadOnlyList<HierarchyReportRow> rows,
        IReadOnlyList<HierarchyTeam> teams,
        IReadOnlyDictionary<string, int> locationCounts,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts,
        IReadOnlyDictionary<string, int> ageCounts,
        IReadOnlyDictionary<string, int> tenureCounts)
    {
        ArgumentNullException.ThrowIfNull(workWeek);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootEmployeeName);
        ArgumentNullException.ThrowIfNull(relationshipField);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentNullException.ThrowIfNull(locationCounts);
        ArgumentNullException.ThrowIfNull(countryCityCounts);
        ArgumentNullException.ThrowIfNull(ageCounts);
        ArgumentNullException.ThrowIfNull(tenureCounts);

        WorkWeek = workWeek;
        RootEmployeeName = rootEmployeeName;
        RelationshipField = relationshipField;
        Rows = rows;
        Teams = teams;
        LocationCounts = locationCounts;
        CountryCityCounts = countryCityCounts;
        AgeCounts = ageCounts;
        TenureCounts = tenureCounts;
    }

    /// <summary>
    /// Gets report work week.
    /// </summary>
    public WorkWeek WorkWeek { get; }

    /// <summary>
    /// Gets root employee display name.
    /// </summary>
    public string RootEmployeeName { get; }

    /// <summary>
    /// Gets hierarchy relationship field.
    /// </summary>
    public HierarchyRelationshipField RelationshipField { get; }

    /// <summary>
    /// Gets flattened hierarchy rows.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> Rows { get; }

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
