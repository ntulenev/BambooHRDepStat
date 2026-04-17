namespace Infrastructure;

/// <summary>
/// Represents one rendered table cell in a shared report table.
/// </summary>
internal sealed record ReportTableCell
{
    /// <summary>
    /// Creates a shared report table cell.
    /// </summary>
    public ReportTableCell(
        string text,
        string? secondaryText = null,
        bool IsEmphasized = false,
        int IndentLevel = 0,
        Abstractions.ReportAvailabilityState? AvailabilityState = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        SecondaryText = secondaryText;
        this.IsEmphasized = IsEmphasized;
        this.IndentLevel = IndentLevel;
        this.AvailabilityState = AvailabilityState;
    }

    /// <summary>
    /// Gets the primary cell text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets optional secondary text rendered next to the primary text.
    /// </summary>
    public string? SecondaryText { get; }

    /// <summary>
    /// Gets a value indicating whether the cell should be emphasized.
    /// </summary>
    public bool IsEmphasized { get; }

    /// <summary>
    /// Gets the indentation level used by hierarchical table rows.
    /// </summary>
    public int IndentLevel { get; }

    /// <summary>
    /// Gets the availability state used for availability-specific styling.
    /// </summary>
    public Abstractions.ReportAvailabilityState? AvailabilityState { get; }
}
