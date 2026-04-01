namespace Logic.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    public FakeTimeProvider(DateTimeOffset currentDate)
    {
        _currentDate = currentDate;
    }

    public override DateTimeOffset GetUtcNow() => _currentDate.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    private readonly DateTimeOffset _currentDate;
}
