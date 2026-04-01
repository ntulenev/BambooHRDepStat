namespace Infrastructure.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    public FakeTimeProvider(DateTimeOffset localNow)
    {
        _localNow = localNow;
    }

    public override DateTimeOffset GetUtcNow() => _localNow.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    private readonly DateTimeOffset _localNow;
}
