namespace Models;

/// <summary>
/// Completed hierarchy report.
/// </summary>
public sealed class HierarchyReport
{
    /// <summary>
    /// Creates hierarchy report.
    /// </summary>
    public HierarchyReport(
        WorkWeek workWeek,
        string rootEmployeeName,
        HierarchyRelationshipField relationshipField,
        IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(workWeek);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootEmployeeName);
        ArgumentNullException.ThrowIfNull(relationshipField);
        ArgumentNullException.ThrowIfNull(rows);

        WorkWeek = workWeek;
        RootEmployeeName = rootEmployeeName;
        RelationshipField = relationshipField;
        Rows = rows;
    }

    /// <summary>
    /// Gets report work week.
    /// </summary>
    public WorkWeek WorkWeek { get; }

    /// <summary>
    /// Gets root employee display name.
    /// </summary>
    public string RootEmployeeName { get; }

    /// <summary>
    /// Gets hierarchy relationship field.
    /// </summary>
    public HierarchyRelationshipField RelationshipField { get; }

    /// <summary>
    /// Gets flattened hierarchy rows.
    /// </summary>
    public IReadOnlyList<HierarchyReportRow> Rows { get; }
}
