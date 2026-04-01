using Models;

namespace Abstractions;

/// <summary>
/// Resolves employee availability and holiday projections for the report.
/// </summary>
public interface IEmployeeAvailabilityResolver
{
    /// <summary>
    /// Builds normalized holiday-to-country mappings from configuration.
    /// </summary>
    Dictionary<string, IReadOnlyList<string>> BuildHolidayCountryMappings(
        IReadOnlyDictionary<string, string[]> configuredMappings);

    /// <summary>
    /// Builds holiday entries for the report summary.
    /// </summary>
    IReadOnlyList<HolidayReportItem> BuildHolidayEntries(
        IReadOnlyList<TimeOffEntry> whoIsOut,
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings);

    /// <summary>
    /// Builds availability entries per employee for the included hierarchy.
    /// </summary>
    IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> BuildEmployeeEntries(
        IReadOnlyCollection<EmployeeProfile> includedProfiles,
        IReadOnlyList<TimeOffEntry> whoIsOut,
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings);
}
