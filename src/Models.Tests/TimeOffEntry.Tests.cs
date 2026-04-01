using FluentAssertions;

namespace Models.Tests;

public sealed class TimeOffEntryTests
{
    [Fact(DisplayName = "The time-off entry constructor throws when the end date is earlier than the start date.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenEndDatePrecedesStartDate()
    {
        // Arrange

        // Act
        var action = () => new TimeOffEntry(
            1,
            TimeOffEntryType.TimeOff,
            new EmployeeId(1),
            "Alice Smith",
            new DateOnly(2026, 4, 3),
            new DateOnly(2026, 4, 2));

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Time-off end date must be on or after the start date.*")
            .And.ParamName.Should().Be("end");
    }

    [Fact(DisplayName = "The inclusion check returns true for dates inside the time-off range and false outside it.")]
    [Trait("Category", "Unit")]
    public void IncludesShouldReturnWhetherDateFallsInsideRange()
    {
        // Arrange
        var entry = new TimeOffEntry(
            1,
            TimeOffEntryType.TimeOff,
            new EmployeeId(1),
            "Alice Smith",
            new DateOnly(2026, 4, 2),
            new DateOnly(2026, 4, 4));

        // Act
        var startsIncluded = entry.Includes(new DateOnly(2026, 4, 2));
        var middleIncluded = entry.Includes(new DateOnly(2026, 4, 3));
        var endsIncluded = entry.Includes(new DateOnly(2026, 4, 4));
        var outsideIncluded = entry.Includes(new DateOnly(2026, 4, 5));

        // Assert
        startsIncluded.Should().BeTrue();
        middleIncluded.Should().BeTrue();
        endsIncluded.Should().BeTrue();
        outsideIncluded.Should().BeFalse();
    }
}
