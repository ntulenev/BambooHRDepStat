namespace Models;

/// <summary>
/// Summary metadata for the hierarchy report.
/// </summary>
public sealed class HierarchyReportOverview
{
    /// <summary>
    /// Creates report overview.
    /// </summary>
    public HierarchyReportOverview(
        DateTimeOffset generatedAt,
        AvailabilityWindow availabilityWindow,
        string rootEmployeeName,
        HierarchyRelationshipField relationshipField)
    {
        ArgumentNullException.ThrowIfNull(availabilityWindow);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootEmployeeName);
        ArgumentNullException.ThrowIfNull(relationshipField);

        GeneratedAt = generatedAt;
        AvailabilityWindow = availabilityWindow;
        RootEmployeeName = rootEmployeeName;
        RelationshipField = relationshipField;
    }

    /// <summary>
    /// Gets report generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Gets report availability window.
    /// </summary>
    public AvailabilityWindow AvailabilityWindow { get; }

    /// <summary>
    /// Gets root employee display name.
    /// </summary>
    public string RootEmployeeName { get; }

    /// <summary>
    /// Gets hierarchy relationship field.
    /// </summary>
    public HierarchyRelationshipField RelationshipField { get; }
}
