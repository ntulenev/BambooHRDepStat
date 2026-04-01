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
        EmployeeId? employeeId,
        string name,
        DateOnly start,
        DateOnly end)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                "Time-off end date must be on or after the start date.");
        }

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
    public EmployeeId? EmployeeId { get; }

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

    /// <summary>
    /// Determines whether the entry affects the provided date.
    /// </summary>
    public bool Includes(DateOnly date)
    {
        return Start <= date && End >= date;
    }
}
