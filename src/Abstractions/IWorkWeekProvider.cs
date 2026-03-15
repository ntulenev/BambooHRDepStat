using Models;

namespace Abstractions;

/// <summary>
/// Resolves which work week should be reported.
/// </summary>
public interface IWorkWeekProvider
{
    /// <summary>
    /// Gets current work week when today is Monday-Friday, otherwise next week.
    /// </summary>
    WorkWeek GetWorkWeek(DateTimeOffset currentDate);
}
