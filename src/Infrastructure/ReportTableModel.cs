namespace Infrastructure;

/// <summary>
/// Represents a table-shaped report section that can be rendered by multiple outputs.
/// </summary>
internal sealed record ReportTableModel
{
    /// <summary>
    /// Creates a shared report table model.
    /// </summary>
    public ReportTableModel(
        IReadOnlyList<ReportTableColumn> columns,
        IReadOnlyList<ReportTableRow> rows,
        string emptyText)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentException.ThrowIfNullOrWhiteSpace(emptyText);

        Columns = columns;
        Rows = rows;
        EmptyText = emptyText;
    }

    /// <summary>
    /// Gets the table columns.
    /// </summary>
    public IReadOnlyList<ReportTableColumn> Columns { get; }

    /// <summary>
    /// Gets the table rows.
    /// </summary>
    public IReadOnlyList<ReportTableRow> Rows { get; }

    /// <summary>
    /// Gets the text shown when the table has no rows.
    /// </summary>
    public string EmptyText { get; }
}
