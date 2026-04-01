namespace Models;

/// <summary>
/// Manager reference resolved either by employee identifier or by display name.
/// </summary>
public readonly record struct ManagerReference
{
    /// <summary>
    /// Creates a manager reference.
    /// </summary>
    public ManagerReference(EmployeeId? employeeId, string? displayName)
    {
        EmployeeId = employeeId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    /// <summary>
    /// Gets the manager employee identifier when BambooHR stores it explicitly.
    /// </summary>
    public EmployeeId? EmployeeId { get; }

    /// <summary>
    /// Gets the manager display name when BambooHR stores the relationship by text.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether the reference contains any manager data.
    /// </summary>
    public bool HasValue => EmployeeId.HasValue || !string.IsNullOrWhiteSpace(DisplayName);

    /// <summary>
    /// Parses a BambooHR manager lookup value.
    /// </summary>
    public static ManagerReference Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var trimmed = value.Trim();
        return Models.EmployeeId.TryParse(trimmed, out var employeeId)
            ? new ManagerReference(employeeId, null)
            : new ManagerReference(null, trimmed);
    }

    /// <summary>
    /// Resolves the best display name for the manager.
    /// </summary>
    public string? ResolveDisplayName(
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId)
    {
        ArgumentNullException.ThrowIfNull(profilesByEmployeeId);

        return EmployeeId.HasValue
            && profilesByEmployeeId.TryGetValue(EmployeeId.Value, out var profile)
                ? profile.DisplayName
                : DisplayName;
    }
}
