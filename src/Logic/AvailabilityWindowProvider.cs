using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves the rolling availability window starting today and ending seven days later.
/// </summary>
public sealed class AvailabilityWindowProvider : IAvailabilityWindowProvider
{
    public AvailabilityWindowProvider(HierarchyReportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <inheritdoc/>
    public AvailabilityWindow GetAvailabilityWindow(DateTimeOffset currentDate)
    {
        var today = DateOnly.FromDateTime(currentDate.Date);
        return new AvailabilityWindow(today, today.AddDays(_settings.AvailabilityLookaheadDays));
    }

    private readonly HierarchyReportSettings _settings;
}
