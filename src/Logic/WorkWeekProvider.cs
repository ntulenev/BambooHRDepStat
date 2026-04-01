using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves the rolling availability window starting today and ending seven days later.
/// </summary>
public sealed class WorkWeekProvider : IWorkWeekProvider
{
    /// <inheritdoc/>
    public WorkWeek GetWorkWeek(DateTimeOffset currentDate)
    {
        var today = DateOnly.FromDateTime(currentDate.Date);
        return new WorkWeek(today, today.AddDays(7));
    }
}
