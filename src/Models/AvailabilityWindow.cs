namespace Models;

/// <summary>
/// Inclusive report date range used for availability and summary calculations.
/// </summary>
public sealed class AvailabilityWindow
{
    /// <summary>
    /// Creates report date range.
    /// </summary>
    public AvailabilityWindow(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                "Range end date must be on or after the start date.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets range start date.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets range end date.
    /// </summary>
    public DateOnly End { get; }
}
