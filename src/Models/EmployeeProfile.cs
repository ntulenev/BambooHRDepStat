namespace Models;

/// <summary>
/// Employee data used for hierarchy and report generation.
/// </summary>
public sealed class EmployeeProfile
{
    /// <summary>
    /// Creates employee profile.
    /// </summary>
    public EmployeeProfile(
        int employeeId,
        string displayName,
        string firstName,
        string lastName,
        string preferredName,
        string? department,
        string? jobTitle,
        string? location,
        string? country,
        string? city,
        DateOnly? dateOfBirth,
        DateOnly? hireDate,
        string? workEmail,
        string? managerLookupValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredName);

        EmployeeId = employeeId;
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName;
        Department = string.IsNullOrWhiteSpace(department) ? null : department;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        Location = string.IsNullOrWhiteSpace(location) ? null : location;
        Country = string.IsNullOrWhiteSpace(country) ? null : country;
        City = string.IsNullOrWhiteSpace(city) ? null : city;
        DateOfBirth = dateOfBirth;
        HireDate = hireDate;
        WorkEmail = string.IsNullOrWhiteSpace(workEmail) ? null : workEmail;
        ManagerLookupValue = string.IsNullOrWhiteSpace(managerLookupValue)
            ? null
            : managerLookupValue;
    }

    /// <summary>
    /// Gets employee identifier.
    /// </summary>
    public int EmployeeId { get; }

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
    /// Gets country.
    /// </summary>
    public string? Country { get; }

    /// <summary>
    /// Gets city.
    /// </summary>
    public string? City { get; }

    /// <summary>
    /// Gets date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; }

    /// <summary>
    /// Gets hire date.
    /// </summary>
    public DateOnly? HireDate { get; }

    /// <summary>
    /// Gets work email.
    /// </summary>
    public string? WorkEmail { get; }

    /// <summary>
    /// Gets raw manager lookup value.
    /// </summary>
    public string? ManagerLookupValue { get; }
}
