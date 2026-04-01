namespace Models;

/// <summary>
/// Highlights and team summaries for the report.
/// </summary>
public sealed class HierarchyReportSummaries
{
    /// <summary>
    /// Creates summary content.
    /// </summary>
    public HierarchyReportSummaries(
        IReadOnlyList<HierarchyReportRow> recentHires,
        int recentHirePeriodDays,
        IReadOnlyList<HierarchyTeam> teams)
    {
        ArgumentNullException.ThrowIfNull(recentHires);
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recentHirePeriodDays);

        RecentHires = recentHires;
        RecentHirePeriodDays = recentHirePeriodDays;
        Teams = teams;
    }

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
}
