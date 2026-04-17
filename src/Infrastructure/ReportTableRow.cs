namespace Infrastructure;

/// <summary>
/// Represents one row in a shared report table.
/// </summary>
internal sealed record ReportTableRow
{
    /// <summary>
    /// Creates a report table row.
    /// </summary>
    public ReportTableRow(IReadOnlyList<ReportTableCell> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        Cells = cells;
    }

    /// <summary>
    /// Gets the row cells.
    /// </summary>
    public IReadOnlyList<ReportTableCell> Cells { get; }
}
