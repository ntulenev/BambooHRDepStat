using Models;

namespace Abstractions;

/// <summary>
/// Builds report analytics and summary projections for a resolved hierarchy.
/// </summary>
public interface IHierarchyAnalytics
{
    /// <summary>
    /// Builds manager-led teams from flattened hierarchy rows.
    /// </summary>
    IReadOnlyList<HierarchyTeam> BuildTeams(
        IReadOnlyList<HierarchyReportRow> rows,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager);

    /// <summary>
    /// Builds recent hire rows inside the configured time window.
    /// </summary>
    IReadOnlyList<HierarchyReportRow> BuildRecentHires(
        IReadOnlyList<HierarchyReportRow> rows,
        DateOnly referenceDate,
        int recentHirePeriodDays);

    /// <summary>
    /// Builds employee counts by location bucket.
    /// </summary>
    Dictionary<string, int> BuildLocationCounts(
        IEnumerable<EmployeeProfile> profiles);

    /// <summary>
    /// Builds city counts grouped by country.
    /// </summary>
    Dictionary<string, IReadOnlyDictionary<string, int>> BuildCountryCityCounts(
        IEnumerable<EmployeeProfile> profiles);

    /// <summary>
    /// Builds age distribution buckets.
    /// </summary>
    Dictionary<string, int> BuildAgeCounts(
        IEnumerable<EmployeeProfile> profiles,
        DateOnly referenceDate);

    /// <summary>
    /// Builds company tenure distribution buckets.
    /// </summary>
    Dictionary<string, int> BuildTenureCounts(
        IEnumerable<EmployeeProfile> profiles,
        DateOnly referenceDate);
}
