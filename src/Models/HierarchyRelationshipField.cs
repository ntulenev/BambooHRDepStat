namespace Models;

/// <summary>
/// Field used to connect employees to their manager.
/// </summary>
public sealed class HierarchyRelationshipField
{
    /// <summary>
    /// Creates hierarchy relationship field metadata.
    /// </summary>
    public HierarchyRelationshipField(
        string requestKey,
        string displayName,
        bool usesEmployeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        RequestKey = requestKey;
        DisplayName = displayName;
        UsesEmployeeId = usesEmployeeId;
    }

    /// <summary>
    /// Gets API request key.
    /// </summary>
    public string RequestKey { get; }

    /// <summary>
    /// Gets field display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether the relationship stores employee IDs.
    /// </summary>
    public bool UsesEmployeeId { get; }
}
