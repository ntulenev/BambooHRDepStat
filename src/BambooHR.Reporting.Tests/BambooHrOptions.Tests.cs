using BambooHR.Reporting.Configuration;

using FluentAssertions;

namespace BambooHR.Reporting.Tests;

public sealed class BambooHrOptionsTests
{
    [Fact(DisplayName = "The validator throws when the organization is missing.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenOrganizationIsMissing()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Organization = string.Empty;

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:Organization is required.");
    }

    [Fact(DisplayName = "The validator throws when the token is missing.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenTokenIsMissing()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Token = " ";

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:Token is required.");
    }

    [Fact(DisplayName = "The validator throws when the employee identifier is not positive.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenEmployeeIdIsNotPositive()
    {
        // Arrange
        var options = CreateValidOptions();
        options.EmployeeId = 0;

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:EmployeeId must be greater than zero.");
    }

    [Fact(DisplayName = "The validator throws when the availability lookahead is negative.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenAvailabilityLookaheadDaysIsNegative()
    {
        // Arrange
        var options = CreateValidOptions();
        options.AvailabilityLookaheadDays = -1;

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:AvailabilityLookaheadDays must be greater than or equal to zero.");
    }

    [Fact(DisplayName = "The validator throws when the recent-hire period is not positive.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenRecentHirePeriodDaysIsNotPositive()
    {
        // Arrange
        var options = CreateValidOptions();
        options.RecentHirePeriodDays = 0;

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:RecentHirePeriodDays must be greater than zero.");
    }

    [Fact(DisplayName = "The validator throws when a holiday mapping key is empty.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenHolidayMappingKeyIsEmpty()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HolidayCountryMappings[string.Empty] = ["Germany"];

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:HolidayCountryMappings keys must be non-empty.");
    }

    [Fact(DisplayName = "The validator throws when a holiday mapping contains a null country array.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenHolidayMappingValueIsNull()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HolidayCountryMappings["Founders Day"] = null!;

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:HolidayCountryMappings 'Founders Day' must contain a country array.");
    }

    [Fact(DisplayName = "The validator throws when a holiday mapping contains an empty country value.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldThrowWhenHolidayMappingContainsEmptyCountryValue()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HolidayCountryMappings["Founders Day"] = ["Germany", " "];

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("BambooHR:HolidayCountryMappings 'Founders Day' contains an empty country value.");
    }

    [Fact(DisplayName = "The validator accepts a valid configuration.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldAcceptValidConfiguration()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HolidayCountryMappings["Founders Day"] = ["Germany", "Malta"];

        // Act
        Action action = options.Validate;

        // Assert
        action.Should().NotThrow();
    }

    private static BambooHrOptions CreateValidOptions()
        => new()
        {
            Organization = "acme",
            Token = "secret-token",
            EmployeeId = 42,
            AvailabilityLookaheadDays = 7,
            RecentHirePeriodDays = 30
        };
}
