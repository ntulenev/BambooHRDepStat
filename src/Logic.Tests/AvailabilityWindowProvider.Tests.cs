namespace Logic.Tests;

public sealed class AvailabilityWindowProviderTests
{
    [Fact]
    public void GetAvailabilityWindowReturnsTodayThroughConfiguredLookaheadWhenTodayIsWorkingDay()
    {
        var provider = new AvailabilityWindowProvider(CreateSettings(availabilityLookaheadDays: 7));

        var week = provider.GetAvailabilityWindow(new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 12), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 19), week.End);
    }

    [Fact]
    public void GetAvailabilityWindowReturnsTodayThroughConfiguredLookaheadWhenTodayIsWeekend()
    {
        var provider = new AvailabilityWindowProvider(CreateSettings(availabilityLookaheadDays: 7));

        var week = provider.GetAvailabilityWindow(new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 14), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 21), week.End);
    }

    [Fact]
    public void GetAvailabilityWindowUsesConfiguredAvailabilityLookaheadDays()
    {
        var provider = new AvailabilityWindowProvider(CreateSettings(availabilityLookaheadDays: 3));

        var week = provider.GetAvailabilityWindow(new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 4, 1), week.Start);
        Assert.Equal(new DateOnly(2026, 4, 4), week.End);
    }

    private static Abstractions.HierarchyReportSettings CreateSettings(int availabilityLookaheadDays)
    {
        return new Abstractions.HierarchyReportSettings(
            new Models.EmployeeId(1),
            availabilityLookaheadDays,
            recentHirePeriodDays: 30,
            new Dictionary<string, string[]>());
    }
}
