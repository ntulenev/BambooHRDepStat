namespace Models;

/// <summary>
/// Hierarchy rows and holiday entries for the report.
/// </summary>
public sealed class HierarchyReportHierarchy
{
    /// <summary>
    /// Creates hierarchy content.
    /// </summary>
    public HierarchyReportHierarchy(
        IReadOnlyList<HolidayReportItem> holidays,
        IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        ArgumentNullException.ThrowIfNull(rows);

        Holidays = holidays;
        Rows = rows;
    }

    /// <summary>
    /// Gets holidays returned by BambooHR for the availability window.
    /// </summary>
    public IReadOnlyList<HolidayReportItem> Holidays { get; }

    /// <summary>
    /// Gets flattened hierarchy rows.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> Rows { get; }
}
