using Models;

namespace Abstractions;

/// <summary>
/// BambooHR API client.
/// </summary>
public interface IBambooHrClient
{
    /// <summary>
    /// Gets available employee fields.
    /// </summary>
    Task<IReadOnlyList<BambooHrField>> GetFieldsAsync(CancellationToken ct);

    /// <summary>
    /// Gets active employees.
    /// </summary>
    Task<IReadOnlyList<BasicEmployee>> GetEmployeesAsync(CancellationToken ct);

    /// <summary>
    /// Gets dynamic fields for one employee.
    /// </summary>
    Task<EmployeeFieldValues> GetEmployeeFieldsAsync(
        int employeeId,
        IReadOnlyCollection<string> fieldKeys,
        CancellationToken ct);

    /// <summary>
    /// Gets who's out entries for a date range.
    /// </summary>
    Task<IReadOnlyList<TimeOffEntry>> GetWhosOutAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct);
}
