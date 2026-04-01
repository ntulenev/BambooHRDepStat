using Models;

namespace Abstractions;

/// <summary>
/// Resolves which availability window should be reported.
/// </summary>
public interface IWorkWeekProvider
{
    /// <summary>
    /// Gets the rolling availability range starting on the provided date and ending seven days later.
    /// </summary>
    WorkWeek GetWorkWeek(DateTimeOffset currentDate);
}
