using Abstractions;

using Models;

namespace Logic.Tests;

internal sealed class StubAvailabilityWindowProvider : IAvailabilityWindowProvider
{
    public StubAvailabilityWindowProvider(AvailabilityWindow availabilityWindow)
    {
        _availabilityWindow = availabilityWindow;
    }

    public AvailabilityWindow GetAvailabilityWindow(DateTimeOffset currentDate)
    {
        _ = currentDate;
        return _availabilityWindow;
    }

    private readonly AvailabilityWindow _availabilityWindow;
}
