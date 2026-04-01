using FluentAssertions;

namespace Models.Tests;

public sealed class ManagerReferenceTests
{
    [Fact(DisplayName = "The parser returns an employee-id reference when the BambooHR manager value is numeric.")]
    [Trait("Category", "Unit")]
    public void ParseShouldCreateEmployeeIdReferenceWhenValueIsNumeric()
    {
        // Arrange

        // Act
        var managerReference = ManagerReference.Parse("42");

        // Assert
        managerReference.EmployeeId.Should().Be(new EmployeeId(42));
        managerReference.DisplayName.Should().BeNull();
        managerReference.HasValue.Should().BeTrue();
    }

    [Fact(DisplayName = "The parser trims and preserves a manager display name when the value is textual.")]
    [Trait("Category", "Unit")]
    public void ParseShouldCreateDisplayNameReferenceWhenValueIsText()
    {
        // Arrange

        // Act
        var managerReference = ManagerReference.Parse("  Alice Smith  ");

        // Assert
        managerReference.EmployeeId.Should().BeNull();
        managerReference.DisplayName.Should().Be("Alice Smith");
        managerReference.HasValue.Should().BeTrue();
    }

    [Fact(DisplayName = "The parser returns the default reference when the manager value is blank.")]
    [Trait("Category", "Unit")]
    public void ParseShouldReturnDefaultReferenceWhenValueIsBlank()
    {
        // Arrange

        // Act
        var managerReference = ManagerReference.Parse("   ");

        // Assert
        managerReference.HasValue.Should().BeFalse();
        managerReference.EmployeeId.Should().BeNull();
        managerReference.DisplayName.Should().BeNull();
    }

    [Fact(DisplayName = "The manager reference resolves the manager display name from the loaded profiles when an employee identifier is available.")]
    [Trait("Category", "Unit")]
    public void ResolveDisplayNameShouldPreferLoadedProfileWhenEmployeeIdExists()
    {
        // Arrange
        var managerEmployeeId = new EmployeeId(7);
        var profilesByEmployeeId = new Dictionary<EmployeeId, EmployeeProfile>
        {
            [managerEmployeeId] = CreateProfile(managerEmployeeId, "Alice Smith")
        };
        var managerReference = new ManagerReference(managerEmployeeId, "Fallback Name");

        // Act
        var displayName = managerReference.ResolveDisplayName(profilesByEmployeeId);

        // Assert
        displayName.Should().Be("Alice Smith");
    }

    private static EmployeeProfile CreateProfile(EmployeeId employeeId, string displayName)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            employeeId,
            displayName,
            names[0],
            names[^1],
            names[0],
            department: "Engineering",
            jobTitle: "Engineer",
            location: null,
            country: null,
            city: null,
            dateOfBirth: null,
            hireDate: null,
            workEmail: null,
            manager: default);
    }
}
