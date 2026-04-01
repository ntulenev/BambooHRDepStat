using System.Globalization;

using FluentAssertions;

namespace Models.Tests;

public sealed class EmployeeIdTests
{
    [Fact(DisplayName = "The employee identifier constructor throws when the value is zero.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenValueIsZero()
    {
        // Arrange

        // Act
        var action = () => new EmployeeId(0);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "The employee identifier constructor throws when the value is negative.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenValueIsNegative()
    {
        // Arrange

        // Act
        var action = () => new EmployeeId(-1);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "The parser returns an employee identifier when the value is a positive integer.")]
    [Trait("Category", "Unit")]
    public void ParseShouldReturnEmployeeIdForPositiveInteger()
    {
        // Arrange

        // Act
        var employeeId = EmployeeId.Parse("42");

        // Assert
        employeeId.Value.Should().Be(42);
    }

    [Fact(DisplayName = "The employee identifier formats its value using the supplied culture-invariant representation.")]
    [Trait("Category", "Unit")]
    public void ToStringShouldReturnInvariantRepresentation()
    {
        // Arrange
        var employeeId = new EmployeeId(42);

        // Act
        var formatted = employeeId.ToString(CultureInfo.InvariantCulture);

        // Assert
        formatted.Should().Be("42");
    }

    [Fact(DisplayName = "The employee identifier supports custom numeric formatting.")]
    [Trait("Category", "Unit")]
    public void ToStringShouldSupportCustomFormatStrings()
    {
        // Arrange
        var employeeId = new EmployeeId(42);

        // Act
        var formatted = employeeId.ToString("D4", null);

        // Assert
        formatted.Should().Be("0042");
    }

    [Fact(DisplayName = "The parser rejects null values.")]
    [Trait("Category", "Unit")]
    public void TryParseShouldReturnFalseWhenInputIsNull()
    {
        // Arrange

        // Act
        var result = EmployeeId.TryParse(null, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "The parser rejects non-numeric values.")]
    [Trait("Category", "Unit")]
    public void TryParseShouldReturnFalseWhenInputIsNonNumeric()
    {
        // Arrange

        // Act
        var result = EmployeeId.TryParse("abc", out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "The parser rejects zero values.")]
    [Trait("Category", "Unit")]
    public void TryParseShouldReturnFalseWhenInputIsZero()
    {
        // Arrange

        // Act
        var result = EmployeeId.TryParse("0", out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "The less-than operator compares employee identifiers by their numeric values.")]
    [Trait("Category", "Unit")]
    public void LessThanOperatorShouldCompareUnderlyingValues()
    {
        // Arrange
        var lower = new EmployeeId(10);
        var higher = new EmployeeId(20);

        // Act
        var result = lower < higher;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "The greater-than operator compares employee identifiers by their numeric values.")]
    [Trait("Category", "Unit")]
    public void GreaterThanOperatorShouldCompareUnderlyingValues()
    {
        // Arrange
        var lower = new EmployeeId(10);
        var higher = new EmployeeId(20);

        // Act
        var result = higher > lower;

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "The comparer returns a negative value when the left employee identifier is lower.")]
    [Trait("Category", "Unit")]
    public void CompareToShouldReturnNegativeValueWhenLeftIdentifierIsLower()
    {
        // Arrange
        var lower = new EmployeeId(10);
        var higher = new EmployeeId(20);

        // Act
        var result = lower.CompareTo(higher);

        // Assert
        result.Should().BeLessThan(0);
    }

    [Fact(DisplayName = "The comparer returns a positive value when the left employee identifier is higher.")]
    [Trait("Category", "Unit")]
    public void CompareToShouldReturnPositiveValueWhenLeftIdentifierIsHigher()
    {
        // Arrange
        var lower = new EmployeeId(10);
        var higher = new EmployeeId(20);

        // Act
        var result = higher.CompareTo(lower);

        // Assert
        result.Should().BeGreaterThan(0);
    }
}
