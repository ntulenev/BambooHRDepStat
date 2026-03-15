namespace Models;

/// <summary>
/// Who's out item.
/// </summary>
public sealed class TimeOffEntry
{
    /// <summary>
    /// Creates time-off or holiday entry.
    /// </summary>
    public TimeOffEntry(
        int id,
        TimeOffEntryType type,
        int? employeeId,
        string name,
        DateOnly start,
        DateOnly end)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Type = type;
        EmployeeId = employeeId;
        Name = name;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets entry identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets entry type.
    /// </summary>
    public TimeOffEntryType Type { get; }

    /// <summary>
    /// Gets employee identifier when entry is time off.
    /// </summary>
    public int? EmployeeId { get; }

    /// <summary>
    /// Gets entry name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets end date.
    /// </summary>
    public DateOnly End { get; }
}
