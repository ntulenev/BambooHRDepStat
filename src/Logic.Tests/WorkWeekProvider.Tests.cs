namespace Logic.Tests;

public sealed class WorkWeekProviderTests
{
    [Fact]
    public void GetWorkWeekReturnsCurrentWeekWhenTodayIsWorkingDay()
    {
        var provider = new WorkWeekProvider();

        var week = provider.GetWorkWeek(new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 9), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 13), week.End);
    }

    [Fact]
    public void GetWorkWeekReturnsNextWeekWhenTodayIsWeekend()
    {
        var provider = new WorkWeekProvider();

        var week = provider.GetWorkWeek(new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 16), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 20), week.End);
    }
}
