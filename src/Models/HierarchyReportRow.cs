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
        EmployeeId employeeId,
        string displayName,
        string? department,
        string? jobTitle,
        string? location,
        DateOnly? dateOfBirth,
        DateOnly? employmentStartDate,
        string? managerName,
        IReadOnlyList<TimeOffEntry> unavailabilityEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(unavailabilityEntries);

        Level = level;
        EmployeeId = employeeId;
        DisplayName = displayName;
        Department = string.IsNullOrWhiteSpace(department) ? null : department;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        Location = string.IsNullOrWhiteSpace(location) ? null : location;
        DateOfBirth = dateOfBirth;
        EmploymentStartDate = employmentStartDate;
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
    public EmployeeId EmployeeId { get; }

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
    /// Gets location.
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// Gets date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; }

    /// <summary>
    /// Gets employment start date.
    /// </summary>
    public DateOnly? EmploymentStartDate { get; }

    /// <summary>
    /// Gets manager name.
    /// </summary>
    public string? ManagerName { get; }

    /// <summary>
    /// Gets employee unavailability entries for the report week.
    /// </summary>
    public IReadOnlyList<TimeOffEntry> UnavailabilityEntries { get; }
}
