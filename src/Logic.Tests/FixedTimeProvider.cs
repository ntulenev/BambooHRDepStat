namespace Logic.Tests;

/// <summary>
/// Fixed time provider used by tests to keep report timestamps deterministic.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset currentDate) : TimeProvider
{
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => currentDate.ToUniversalTime();

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
