namespace Infrastructure.Tests;

internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset currentDate)
    {
        _currentDate = currentDate;
    }

    public override DateTimeOffset GetUtcNow() => _currentDate.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    private readonly DateTimeOffset _currentDate;
}
