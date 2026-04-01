namespace Logic.Tests;

public sealed class WorkWeekProviderTests
{
    [Fact]
    public void GetWorkWeekReturnsTodayThroughNextSevenDaysWhenTodayIsWorkingDay()
    {
        var provider = new WorkWeekProvider();

        var week = provider.GetWorkWeek(new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 12), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 19), week.End);
    }

    [Fact]
    public void GetWorkWeekReturnsTodayThroughNextSevenDaysWhenTodayIsWeekend()
    {
        var provider = new WorkWeekProvider();

        var week = provider.GetWorkWeek(new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 3, 14), week.Start);
        Assert.Equal(new DateOnly(2026, 3, 21), week.End);
    }
}
