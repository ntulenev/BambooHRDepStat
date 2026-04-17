using Models;

namespace Abstractions;

/// <summary>
/// Formats report data for console, HTML, and PDF outputs.
/// </summary>
public interface IReportPresentationFormatter
{
    /// <summary>
    /// Formats employee phones for display.
    /// </summary>
    string FormatPhones(IReadOnlyList<EmployeePhone> phones);

    /// <summary>
    /// Formats a vacation leave balance for display.
    /// </summary>
    string FormatVacationLeaveBalance(VacationLeaveBalance? balance);

    /// <summary>
    /// Formats a date for display.
    /// </summary>
    string FormatDate(DateOnly? dateValue);

    /// <summary>
    /// Formats availability entries for display.
    /// </summary>
    string FormatAvailability(IReadOnlyList<TimeOffEntry> entries, DateOnly referenceDate);

    /// <summary>
    /// Resolves the availability state for rendering.
    /// </summary>
    ReportAvailabilityState GetAvailabilityState(
        IReadOnlyList<TimeOffEntry> entries,
        DateOnly referenceDate);

    /// <summary>
    /// Gets job title counts from the report rows.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, int>> GetJobTitleCounts(
        IReadOnlyList<HierarchyReportRow> rows);

    /// <summary>
    /// Orders counts for display.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, int>> OrderCounts(
        IReadOnlyDictionary<string, int> counts);

    /// <summary>
    /// Formats grade counts for display.
    /// </summary>
    string FormatGradeCounts(IReadOnlyDictionary<string, int> gradeCounts);

    /// <summary>
    /// Builds a hierarchy node label.
    /// </summary>
    string BuildHierarchyDisplayName(HierarchyReportRow row);

    /// <summary>
    /// Builds the recent hire section title.
    /// </summary>
    string BuildRecentHireSectionTitle(int recentHirePeriodDays);

    /// <summary>
    /// Builds the holiday section title.
    /// </summary>
    string BuildHolidaySectionTitle(AvailabilityWindow availabilityWindow);

    /// <summary>
    /// Formats associated countries for display.
    /// </summary>
    string FormatAssociatedCountries(IReadOnlyList<string> associatedCountries);

    /// <summary>
    /// Formats age for display.
    /// </summary>
    string FormatAge(DateOnly? dateOfBirth, DateOnly referenceDate);

    /// <summary>
    /// Formats days with us for display.
    /// </summary>
    string FormatDaysWithUs(DateOnly? employmentStartDate, DateOnly referenceDate);
}
