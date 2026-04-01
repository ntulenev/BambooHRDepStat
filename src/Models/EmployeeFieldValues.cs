namespace Models;

/// <summary>
/// Dynamic employee field values returned from BambooHR.
/// </summary>
public sealed class EmployeeFieldValues
{
    /// <summary>
    /// Creates a field value container.
    /// </summary>
    public EmployeeFieldValues(
        EmployeeId employeeId,
        IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        EmployeeId = employeeId;
        Values = values;
    }

    /// <summary>
    /// Gets employee identifier.
    /// </summary>
    public EmployeeId EmployeeId { get; }

    /// <summary>
    /// Gets requested field values.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Values { get; }
}
