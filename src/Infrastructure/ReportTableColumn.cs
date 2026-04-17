namespace Infrastructure;

/// <summary>
/// Describes one report table column for shared report rendering.
/// </summary>
internal sealed record ReportTableColumn
{
    /// <summary>
    /// Creates a report table column descriptor.
    /// </summary>
    public ReportTableColumn(
        string header,
        float width,
        bool IsConstantWidth = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);

        Header = header;
        Width = width;
        this.IsConstantWidth = IsConstantWidth;
    }

    /// <summary>
    /// Gets the column header text.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets the relative or constant width value used by renderers.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Width"/> is a constant width.
    /// </summary>
    public bool IsConstantWidth { get; }
}
