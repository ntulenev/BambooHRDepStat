using Models;

namespace Abstractions;

/// <summary>
/// Immutable runtime settings used by hierarchy-report logic.
/// </summary>
public sealed class HierarchyReportSettings
{
    /// <summary>
    /// Creates report runtime settings.
    /// </summary>
    public HierarchyReportSettings(
        EmployeeId rootEmployeeId,
        int availabilityLookaheadDays,
        int recentHirePeriodDays,
        IReadOnlyDictionary<string, string[]> holidayCountryMappings,
        bool showTeamReports = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(availabilityLookaheadDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recentHirePeriodDays);
        ArgumentNullException.ThrowIfNull(holidayCountryMappings);

        RootEmployeeId = rootEmployeeId;
        AvailabilityLookaheadDays = availabilityLookaheadDays;
        RecentHirePeriodDays = recentHirePeriodDays;
        ShowTeamReports = showTeamReports;
        HolidayCountryMappings = holidayCountryMappings.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets report root employee identifier.
    /// </summary>
    public EmployeeId RootEmployeeId { get; }

    /// <summary>
    /// Gets the lookahead horizon used for availability.
    /// </summary>
    public int AvailabilityLookaheadDays { get; }

    /// <summary>
    /// Gets the recent-hire period in days.
    /// </summary>
    public int RecentHirePeriodDays { get; }

    /// <summary>
    /// Gets whether flat team report sections should be rendered.
    /// </summary>
    public bool ShowTeamReports { get; }

    /// <summary>
    /// Gets configured holiday-country mappings.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> HolidayCountryMappings { get; }
}
