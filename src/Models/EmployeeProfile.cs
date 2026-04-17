using System.Globalization;

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
        EmployeeId employeeId,
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
        ManagerReference manager,
        string? team = null,
        string? phoneNumbers = null,
        string? vacationLeaveAvailable = null)
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
        Team = string.IsNullOrWhiteSpace(team) ? null : team;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        Location = string.IsNullOrWhiteSpace(location) ? null : location;
        Country = string.IsNullOrWhiteSpace(country) ? null : country;
        City = string.IsNullOrWhiteSpace(city) ? null : city;
        DateOfBirth = dateOfBirth;
        HireDate = hireDate;
        WorkEmail = string.IsNullOrWhiteSpace(workEmail) ? null : workEmail;
        Manager = manager;
        PhoneNumbers = string.IsNullOrWhiteSpace(phoneNumbers) ? null : phoneNumbers;
        VacationLeaveAvailable = string.IsNullOrWhiteSpace(vacationLeaveAvailable) ? null : vacationLeaveAvailable;
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
    /// Gets department.
    /// </summary>
    public string? Department { get; }

    /// <summary>
    /// Gets team.
    /// </summary>
    public string? Team { get; }

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
    /// Gets manager relationship reference.
    /// </summary>
    public ManagerReference Manager { get; }

    /// <summary>
    /// Gets employee phone numbers formatted for report output.
    /// </summary>
    public string? PhoneNumbers { get; }

    /// <summary>
    /// Gets the available vacation leave balance for report output.
    /// </summary>
    public string? VacationLeaveAvailable { get; }

    /// <summary>
    /// Gets raw manager lookup value for diagnostic and display scenarios.
    /// </summary>
    public string? ManagerLookupValue => Manager.DisplayName ?? Manager.EmployeeId?.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets candidate names that BambooHR may use in manager lookup fields.
    /// </summary>
    public IEnumerable<string> CandidateNames => EnumerateCandidateNames();

    /// <summary>
    /// Tries to resolve manager identifier when BambooHR relationship field stores IDs.
    /// </summary>
    public bool TryGetManagerEmployeeId(out EmployeeId managerEmployeeId)
    {
        if (Manager.EmployeeId.HasValue)
        {
            managerEmployeeId = Manager.EmployeeId.Value;
            return true;
        }

        managerEmployeeId = default;
        return false;
    }

    /// <summary>
    /// Resolves the best country label for report grouping.
    /// </summary>
    public string ResolveCountryLabel()
    {
        if (!string.IsNullOrWhiteSpace(Country))
        {
            return Country;
        }

        if (TryParseLocation(Location, out _, out var country))
        {
            return country!;
        }

        return string.IsNullOrWhiteSpace(Location)
            ? "Unknown"
            : Location!;
    }

    /// <summary>
    /// Resolves the best city label for report grouping.
    /// </summary>
    public string? ResolveCityLabel()
    {
        if (!string.IsNullOrWhiteSpace(City))
        {
            return City;
        }

        return TryParseLocation(Location, out var city, out _)
            ? city
            : null;
    }

    /// <summary>
    /// Resolves manager display name using loaded profiles when possible.
    /// </summary>
    public string? ResolveManagerDisplayName(
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId)
    {
        ArgumentNullException.ThrowIfNull(profilesByEmployeeId);

        if (string.IsNullOrWhiteSpace(ManagerLookupValue))
        {
            return null;
        }

        return Manager.ResolveDisplayName(profilesByEmployeeId);
    }

    private static bool TryParseLocation(
        string? location,
        out string? city,
        out string? country)
    {
        city = null;
        country = null;
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var parts = location
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        city = parts[0];
        country = parts[^1];
        return !string.IsNullOrWhiteSpace(city)
            && !string.IsNullOrWhiteSpace(country);
    }

    private IEnumerable<string> EnumerateCandidateNames()
    {
        yield return DisplayName;
        yield return $"{FirstName} {LastName}";
        yield return $"{PreferredName} {LastName}";
    }
}
