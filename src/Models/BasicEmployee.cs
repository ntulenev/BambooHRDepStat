namespace Models;

/// <summary>
/// Summary employee data returned by BambooHR employee list endpoint.
/// </summary>
public sealed class BasicEmployee
{
    /// <summary>
    /// Creates employee summary.
    /// </summary>
    public BasicEmployee(
        EmployeeId employeeId,
        string displayName,
        string firstName,
        string lastName,
        string preferredName,
        string? jobTitle,
        string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredName);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        EmployeeId = employeeId;
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        Status = status;
    }

    /// <summary>
    /// Gets employee identifier.
    /// </summary>
    public EmployeeId EmployeeId { get; }

    /// <summary>
    /// Gets display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets first name.
    /// </summary>
    public string FirstName { get; }

    /// <summary>
    /// Gets last name.
    /// </summary>
    public string LastName { get; }

    /// <summary>
    /// Gets preferred name.
    /// </summary>
    public string PreferredName { get; }

    /// <summary>
    /// Gets current job title.
    /// </summary>
    public string? JobTitle { get; }

    /// <summary>
    /// Gets current employee status.
    /// </summary>
    public string Status { get; }
}
