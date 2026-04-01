using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves the rolling availability window starting today and ending seven days later.
/// </summary>
public sealed class AvailabilityWindowProvider : IAvailabilityWindowProvider
{
    private readonly BambooHrOptions _options;

    public AvailabilityWindowProvider(BambooHrOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public AvailabilityWindow GetAvailabilityWindow(DateTimeOffset currentDate)
    {
        var today = DateOnly.FromDateTime(currentDate.Date);
        return new AvailabilityWindow(today, today.AddDays(_options.AvailabilityLookaheadDays));
    }
}
