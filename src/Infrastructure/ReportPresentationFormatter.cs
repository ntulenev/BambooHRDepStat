using System.Globalization;
using System.Text;

using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// Shared formatting helpers for report outputs.
/// </summary>
public sealed class ReportPresentationFormatter : IReportPresentationFormatter
{
    /// <inheritdoc />
    public string FormatPhones(IReadOnlyList<EmployeePhone> phones)
    {
        ArgumentNullException.ThrowIfNull(phones);

        return phones.Count == 0
            ? "-"
            : string.Join(
                " | ",
                phones.Select(phone =>
                    $"{phone.Label}: {phone.Number}"));
    }

    /// <inheritdoc />
    public string FormatVacationLeaveBalance(VacationLeaveBalance? balance)
    {
        return balance.HasValue
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{decimal.Round(balance.Value.Days, 1, MidpointRounding.AwayFromZero):0.0} days")
            : "-";
    }

    /// <inheritdoc />
    public string FormatDate(DateOnly? dateValue)
    {
        return dateValue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    }

    /// <inheritdoc />
    public string FormatAvailability(IReadOnlyList<TimeOffEntry> entries, DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return "Available";
        }

        var details = string.Join("; ", entries.Select(FormatEntry));
        return GetAvailabilityState(entries, referenceDate) == ReportAvailabilityState.Upcoming
            ? $"Upcoming: {details}"
            : details;
    }

    /// <inheritdoc />
    public ReportAvailabilityState GetAvailabilityState(
        IReadOnlyList<TimeOffEntry> entries,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return ReportAvailabilityState.Available;
        }

        return entries.Any(entry => entry.Start <= referenceDate && entry.End >= referenceDate)
            ? ReportAvailabilityState.UnavailableToday
            : ReportAvailabilityState.Upcoming;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, int>> GetJobTitleCounts(
        IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return
        [
            .. rows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.JobTitle) ? "(No title)" : row.JobTitle)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, int>(group.Key ?? "(No title)", group.Count()))
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, int>> OrderCounts(
        IReadOnlyDictionary<string, int> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return
        [
            .. counts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        ];
    }

    /// <inheritdoc />
    public string FormatGradeCounts(IReadOnlyDictionary<string, int> gradeCounts)
    {
        ArgumentNullException.ThrowIfNull(gradeCounts);

        return string.Join(
            ", ",
            gradeCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}: {pair.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    /// <inheritdoc />
    public string BuildHierarchyDisplayName(HierarchyReportRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var builder = new StringBuilder();
        if (row.Level > 0)
        {
            _ = builder.Append(new string(' ', row.Level * 2));
            _ = builder.Append("|- ");
        }

        _ = builder.Append(row.DisplayName);
        _ = builder.Append(" (#");
        _ = builder.Append(row.EmployeeId.ToString(CultureInfo.InvariantCulture));
        _ = builder.Append(')');

        return builder.ToString();
    }

    /// <inheritdoc />
    public string BuildRecentHireSectionTitle(int recentHirePeriodDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recentHirePeriodDays);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"New Joiners (Last {recentHirePeriodDays} Days)");
    }

    /// <inheritdoc />
    public string BuildHolidaySectionTitle(AvailabilityWindow availabilityWindow)
    {
        ArgumentNullException.ThrowIfNull(availabilityWindow);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Holidays ({availabilityWindow.Start:yyyy-MM-dd} to {availabilityWindow.End:yyyy-MM-dd})");
    }

    /// <inheritdoc />
    public string FormatAssociatedCountries(IReadOnlyList<string> associatedCountries)
    {
        ArgumentNullException.ThrowIfNull(associatedCountries);

        return associatedCountries.Count == 0
            ? "-"
            : string.Join(", ", associatedCountries);
    }

    /// <inheritdoc />
    public string FormatAge(DateOnly? dateOfBirth, DateOnly referenceDate)
    {
        if (dateOfBirth is null)
        {
            return "-";
        }

        var age = referenceDate.Year - dateOfBirth.Value.Year;
        if (referenceDate < dateOfBirth.Value.AddYears(age))
        {
            age--;
        }

        return age.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public string FormatDaysWithUs(DateOnly? employmentStartDate, DateOnly referenceDate)
    {
        if (employmentStartDate is null)
        {
            return "-";
        }

        var days = referenceDate.DayNumber - employmentStartDate.Value.DayNumber + 1;
        if (days < 1)
        {
            days = 1;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{days} days");
    }

    private static string FormatEntry(TimeOffEntry entry)
    {
        var label = entry.Type == TimeOffEntryType.Holiday
            ? $"Holiday ({entry.Name})"
            : "Time off";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: {entry.Start:yyyy-MM-dd} - {entry.End:yyyy-MM-dd}");
    }
}
