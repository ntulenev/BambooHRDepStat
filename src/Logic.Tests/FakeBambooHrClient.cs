using Abstractions;

using Models;

namespace Logic.Tests;

internal sealed class FakeBambooHrClient : IBambooHrClient
{
    public FakeBambooHrClient(
        IReadOnlyList<BambooHrField> fields,
        IReadOnlyList<BasicEmployee> employees,
        IReadOnlyDictionary<int, IReadOnlyDictionary<string, string?>> employeeFields,
        IReadOnlyList<TimeOffEntry> timeOffEntries)
    {
        _fields = fields;
        _employees = employees;
        _employeeFields = employeeFields;
        _timeOffEntries = timeOffEntries;
    }

    public Task<IReadOnlyList<BambooHrField>> GetFieldsAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(_fields);
    }

    public Task<IReadOnlyList<BasicEmployee>> GetEmployeesAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(_employees);
    }

    public Task<EmployeeFieldValues> GetEmployeeFieldsAsync(
        EmployeeId employeeId,
        IReadOnlyCollection<string> fieldKeys,
        CancellationToken ct)
    {
        _ = fieldKeys;
        _ = ct;
        return Task.FromResult(new EmployeeFieldValues(employeeId, _employeeFields[employeeId.Value]));
    }

    public Task<IReadOnlyList<TimeOffEntry>> GetWhosOutAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        _ = startDate;
        _ = endDate;
        _ = ct;
        return Task.FromResult(_timeOffEntries);
    }

    private readonly IReadOnlyList<BambooHrField> _fields;
    private readonly IReadOnlyList<BasicEmployee> _employees;
    private readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string?>> _employeeFields;
    private readonly IReadOnlyList<TimeOffEntry> _timeOffEntries;
}
