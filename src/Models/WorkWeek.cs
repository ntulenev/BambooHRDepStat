namespace Models;

/// <summary>
/// Monday-Friday work week.
/// </summary>
public sealed class WorkWeek
{
    /// <summary>
    /// Creates work week range.
    /// </summary>
    public WorkWeek(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                "Work week end date must be on or after the start date.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets week start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets week end date.
    /// </summary>
    public DateOnly End { get; }
}
