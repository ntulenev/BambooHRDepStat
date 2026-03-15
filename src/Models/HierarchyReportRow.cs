namespace Models;

/// <summary>
/// A flattened hierarchy row.
/// </summary>
public sealed class HierarchyReportRow
{
    /// <summary>
    /// Creates report row.
    /// </summary>
    public HierarchyReportRow(
        int level,
        int employeeId,
        string displayName,
        string? department,
        string? jobTitle,
        string? managerName,
        IReadOnlyList<TimeOffEntry> unavailabilityEntries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(unavailabilityEntries);

        Level = level;
        EmployeeId = employeeId;
        DisplayName = displayName;
        Department = string.IsNullOrWhiteSpace(department) ? null : department;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        ManagerName = string.IsNullOrWhiteSpace(managerName) ? null : managerName;
        UnavailabilityEntries = unavailabilityEntries;
    }

    /// <summary>
    /// Gets hierarchy depth.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets employee identifier.
    /// </summary>
    public int EmployeeId { get; }

    /// <summary>
    /// Gets employee display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets department.
    /// </summary>
    public string? Department { get; }

    /// <summary>
    /// Gets job title.
    /// </summary>
    public string? JobTitle { get; }

    /// <summary>
    /// Gets manager name.
    /// </summary>
    public string? ManagerName { get; }

    /// <summary>
    /// Gets employee unavailability entries for the report week.
    /// </summary>
    public IReadOnlyList<TimeOffEntry> UnavailabilityEntries { get; }
}
