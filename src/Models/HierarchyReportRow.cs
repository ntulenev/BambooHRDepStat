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
        IReadOnlyList<TimeOffEntry> unavailabilityEntries,
        string? workEmail = null,
        string? team = null,
        IReadOnlyList<EmployeePhone>? phones = null,
        VacationLeaveBalance? vacationLeaveBalance = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(unavailabilityEntries);

        Level = level;
        EmployeeId = employeeId;
        DisplayName = displayName;
        Department = string.IsNullOrWhiteSpace(department) ? null : department;
        Team = string.IsNullOrWhiteSpace(team) ? null : team;
        JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle;
        Location = string.IsNullOrWhiteSpace(location) ? null : location;
        DateOfBirth = dateOfBirth;
        EmploymentStartDate = employmentStartDate;
        ManagerName = string.IsNullOrWhiteSpace(managerName) ? null : managerName;
        WorkEmail = string.IsNullOrWhiteSpace(workEmail) ? null : workEmail;
        Phones = phones ?? [];
        VacationLeaveBalance = vacationLeaveBalance;
        UnavailabilityEntries = unavailabilityEntries;
    }

    /// <summary>
    /// Creates a report row from an employee profile.
    /// </summary>
    public static HierarchyReportRow FromProfile(
        int level,
        EmployeeProfile profile,
        string? managerName,
        IReadOnlyList<TimeOffEntry> unavailabilityEntries)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(unavailabilityEntries);

        return new HierarchyReportRow(
            level,
            profile.EmployeeId,
            profile.DisplayName,
            profile.Department,
            profile.JobTitle,
            profile.Location,
            profile.DateOfBirth,
            profile.HireDate,
            managerName,
            unavailabilityEntries,
            profile.WorkEmail,
            profile.Team,
            profile.Phones,
            profile.VacationLeaveBalance);
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
    /// Gets employee work email.
    /// </summary>
    public string? WorkEmail { get; }

    /// <summary>
    /// Gets employee phone numbers.
    /// </summary>
    public IReadOnlyList<EmployeePhone> Phones { get; }

    /// <summary>
    /// Gets employee phone numbers formatted for report output.
    /// </summary>
    public string? PhoneNumbers => Phones.Count == 0
        ? null
        : string.Join(
            " | ",
            Phones.Select(phone => $"{phone.Label}: {phone.Number}"));

    /// <summary>
    /// Gets the available vacation leave balance.
    /// </summary>
    public VacationLeaveBalance? VacationLeaveBalance { get; }

    /// <summary>
    /// Gets the available vacation leave balance formatted for report output.
    /// </summary>
    public string? VacationLeaveAvailable => VacationLeaveBalance.HasValue
        ? string.Create(
            CultureInfo.InvariantCulture,
            $"{decimal.Round(VacationLeaveBalance.Value.Days, 1, MidpointRounding.AwayFromZero):0.0} days")
        : null;

    /// <summary>
    /// Gets employee unavailability entries for the report week.
    /// </summary>
    public IReadOnlyList<TimeOffEntry> UnavailabilityEntries { get; }
}
