using Abstractions;

using FluentAssertions;

using Models;

namespace Logic.Tests;

public sealed class AvailabilityWindowProviderTests
{
    [Fact(DisplayName = "The constructor throws when the hierarchy report settings are null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSettingsAreNull()
    {
        // Arrange

        // Act
        var action = () => new AvailabilityWindowProvider(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Theory(DisplayName = "The provider starts the availability window on the current local date and applies the configured lookahead days.")]
    [Trait("Category", "Unit")]
    [InlineData(2026, 3, 12, 7, 2026, 3, 12, 2026, 3, 19)]
    [InlineData(2026, 3, 14, 7, 2026, 3, 14, 2026, 3, 21)]
    [InlineData(2026, 4, 1, 3, 2026, 4, 1, 2026, 4, 4)]
    public void GetAvailabilityWindowShouldReturnWindowUsingCurrentDateAndConfiguredLookahead(
        int year,
        int month,
        int day,
        int lookaheadDays,
        int expectedStartYear,
        int expectedStartMonth,
        int expectedStartDay,
        int expectedEndYear,
        int expectedEndMonth,
        int expectedEndDay)
    {
        // Arrange
        var provider = new AvailabilityWindowProvider(CreateSettings(lookaheadDays));
        var currentDate = new DateTimeOffset(year, month, day, 10, 0, 0, TimeSpan.Zero);

        // Act
        var availabilityWindow = provider.GetAvailabilityWindow(currentDate);

        // Assert
        availabilityWindow.Should().BeEquivalentTo(new AvailabilityWindow(
            new DateOnly(expectedStartYear, expectedStartMonth, expectedStartDay),
            new DateOnly(expectedEndYear, expectedEndMonth, expectedEndDay)));
    }

    private static HierarchyReportSettings CreateSettings(int availabilityLookaheadDays)
        => new(
            new EmployeeId(1),
            availabilityLookaheadDays,
            recentHirePeriodDays: 30,
            new Dictionary<string, string[]>());
}
