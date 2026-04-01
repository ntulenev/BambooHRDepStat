namespace Abstractions;

/// <summary>
/// Availability state used by report renderers.
/// </summary>
public enum ReportAvailabilityState
{
    /// <summary>
    /// Employee is available today.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Employee has upcoming absence.
    /// </summary>
    Upcoming = 1,

    /// <summary>
    /// Employee is unavailable today.
    /// </summary>
    UnavailableToday = 2
}
