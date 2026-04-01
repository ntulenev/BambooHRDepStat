using System.Globalization;
using System.Text;

using Models;

namespace Infrastructure;

/// <summary>
/// Shared formatting helpers for report outputs.
/// </summary>
internal static class ReportPresentationFormatter
{
    public static string FormatDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    }

    public static string FormatAvailability(IReadOnlyList<TimeOffEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count == 0)
        {
            return "Available";
        }

        return string.Join("; ", entries.Select(FormatEntry));
    }

    public static IReadOnlyList<KeyValuePair<string, int>> GetJobTitleCounts(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return
        [
            .. report.Rows
                .GroupBy(row => string.IsNullOrWhiteSpace(row.JobTitle) ? "(No title)" : row.JobTitle)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, int>(group.Key ?? "(No title)", group.Count()))
        ];
    }

    public static IReadOnlyList<KeyValuePair<string, int>> OrderCounts(
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

    public static string FormatGradeCounts(IReadOnlyDictionary<string, int> gradeCounts)
    {
        ArgumentNullException.ThrowIfNull(gradeCounts);

        return string.Join(
            ", ",
            gradeCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}: {pair.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    public static string BuildHierarchyDisplayName(HierarchyReportRow row)
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

    public static string BuildRecentHireSectionTitle(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"New Joiners (Last {report.RecentHirePeriodDays} Days)");
    }

    public static string FormatAge(DateOnly? dateOfBirth, DateOnly referenceDate)
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

    public static string FormatDaysWithUs(DateOnly? employmentStartDate, DateOnly referenceDate)
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
