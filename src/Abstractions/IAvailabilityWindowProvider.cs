using Models;

namespace Abstractions;

/// <summary>
/// Resolves which availability window should be reported.
/// </summary>
public interface IAvailabilityWindowProvider
{
    /// <summary>
    /// Gets the rolling availability range starting on the provided date and ending seven days later.
    /// </summary>
    AvailabilityWindow GetAvailabilityWindow(DateTimeOffset currentDate);
}
