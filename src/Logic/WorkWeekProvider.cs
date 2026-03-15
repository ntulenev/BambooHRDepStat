using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves current or next Monday-Friday work week.
/// </summary>
public sealed class WorkWeekProvider : IWorkWeekProvider
{
    /// <inheritdoc/>
    public WorkWeek GetWorkWeek(DateTimeOffset currentDate)
    {
        var today = DateOnly.FromDateTime(currentDate.Date);
        var weekday = currentDate.DayOfWeek;
        var start = IsWorkingDay(weekday)
            ? GetMonday(today, weekday)
            : GetNextMonday(today, weekday);

        return new WorkWeek(start, start.AddDays(4));
    }

    private static DateOnly GetMonday(DateOnly today, DayOfWeek weekday)
    {
        var offset = weekday switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => -1,
            DayOfWeek.Wednesday => -2,
            DayOfWeek.Thursday => -3,
            DayOfWeek.Friday => -4,
            DayOfWeek.Saturday => throw new InvalidOperationException("Working day was expected."),
            DayOfWeek.Sunday => throw new InvalidOperationException("Working day was expected."),
            _ => throw new InvalidOperationException("Unknown day of week.")
        };

        return today.AddDays(offset);
    }

    private static DateOnly GetNextMonday(DateOnly today, DayOfWeek weekday)
    {
        var offset = weekday switch
        {
            DayOfWeek.Monday => throw new InvalidOperationException("Weekend day was expected."),
            DayOfWeek.Tuesday => throw new InvalidOperationException("Weekend day was expected."),
            DayOfWeek.Wednesday => throw new InvalidOperationException("Weekend day was expected."),
            DayOfWeek.Thursday => throw new InvalidOperationException("Weekend day was expected."),
            DayOfWeek.Friday => throw new InvalidOperationException("Weekend day was expected."),
            DayOfWeek.Saturday => 2,
            DayOfWeek.Sunday => 1,
            _ => throw new InvalidOperationException("Unknown day of week.")
        };

        return today.AddDays(offset);
    }

    private static bool IsWorkingDay(DayOfWeek weekday) => weekday is >= DayOfWeek.Monday
        and <= DayOfWeek.Friday;
}
