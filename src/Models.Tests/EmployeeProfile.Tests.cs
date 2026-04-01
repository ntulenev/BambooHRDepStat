using FluentAssertions;

namespace Models.Tests;

public sealed class EmployeeProfileTests
{
    [Fact(DisplayName = "The employee profile resolves the country label from the explicit country field.")]
    [Trait("Category", "Unit")]
    public void ResolveCountryLabelShouldPreferExplicitCountryValue()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Alice Smith",
            location: "Berlin, Germany",
            country: "Germany",
            city: "Berlin");

        // Act
        var countryLabel = profile.ResolveCountryLabel();

        // Assert
        countryLabel.Should().Be("Germany");
    }

    [Fact(DisplayName = "The employee profile resolves the city label from the explicit city field.")]
    [Trait("Category", "Unit")]
    public void ResolveCityLabelShouldPreferExplicitCityValue()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Alice Smith",
            location: "Berlin, Germany",
            country: "Germany",
            city: "Berlin");

        // Act
        var cityLabel = profile.ResolveCityLabel();

        // Assert
        cityLabel.Should().Be("Berlin");
    }

    [Fact(DisplayName = "The employee profile falls back to the parsed country part of the location string when the explicit country is missing.")]
    [Trait("Category", "Unit")]
    public void ResolveCountryLabelShouldFallbackToParsedLocationCountry()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Bob Jones",
            location: "Valletta, Malta");

        // Act
        var countryLabel = profile.ResolveCountryLabel();

        // Assert
        countryLabel.Should().Be("Malta");
    }

    [Fact(DisplayName = "The employee profile falls back to the parsed city part of the location string when the explicit city is missing.")]
    [Trait("Category", "Unit")]
    public void ResolveCityLabelShouldFallbackToParsedLocationCity()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Bob Jones",
            location: "Valletta, Malta");

        // Act
        var cityLabel = profile.ResolveCityLabel();

        // Assert
        cityLabel.Should().Be("Valletta");
    }

    [Fact(DisplayName = "The employee profile returns the raw location as the country label when the location cannot be parsed.")]
    [Trait("Category", "Unit")]
    public void ResolveCountryLabelShouldReturnRawLocationWhenLocationCannotBeParsed()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Carol Brown",
            location: "Remote");

        // Act
        var countryLabel = profile.ResolveCountryLabel();

        // Assert
        countryLabel.Should().Be("Remote");
    }

    [Fact(DisplayName = "The employee profile returns null for the city label when the location cannot be parsed.")]
    [Trait("Category", "Unit")]
    public void ResolveCityLabelShouldReturnNullWhenLocationCannotBeParsed()
    {
        // Arrange
        var profile = CreateProfile(
            displayName: "Carol Brown",
            location: "Remote");

        // Act
        var cityLabel = profile.ResolveCityLabel();

        // Assert
        cityLabel.Should().BeNull();
    }

    [Fact(DisplayName = "The employee profile exposes the display name as a BambooHR manager lookup candidate.")]
    [Trait("Category", "Unit")]
    public void CandidateNamesShouldContainDisplayName()
    {
        // Arrange
        var profile = CreateProfileWithPreferredName("Alice Smith", "Ally");

        // Act
        var candidateNames = profile.CandidateNames.ToArray();

        // Assert
        candidateNames.Should().ContainInOrder("Alice Smith");
    }

    [Fact(DisplayName = "The employee profile exposes the legal first and last name combination as a BambooHR manager lookup candidate.")]
    [Trait("Category", "Unit")]
    public void CandidateNamesShouldContainLegalNameVariant()
    {
        // Arrange
        var profile = CreateProfileWithPreferredName("Alice Smith", "Ally");

        // Act
        var candidateNames = profile.CandidateNames.ToArray();

        // Assert
        candidateNames.Should().Contain("Alice Smith");
    }

    [Fact(DisplayName = "The employee profile exposes the preferred first name and last name combination as a BambooHR manager lookup candidate.")]
    [Trait("Category", "Unit")]
    public void CandidateNamesShouldContainPreferredNameVariant()
    {
        // Arrange
        var profile = CreateProfileWithPreferredName("Alice Smith", "Ally");

        // Act
        var candidateNames = profile.CandidateNames.ToArray();

        // Assert
        candidateNames.Should().Contain("Ally Smith");
    }

    [Fact(DisplayName = "The employee profile resolves the manager display name from loaded profiles when the manager employee identifier is available.")]
    [Trait("Category", "Unit")]
    public void ResolveManagerDisplayNameShouldUseManagerProfileWhenEmployeeIdIsAvailable()
    {
        // Arrange
        var managerEmployeeId = new EmployeeId(7);
        var managerProfile = CreateProfile("Alice Smith");
        var employeeProfile = new EmployeeProfile(
            new EmployeeId(2),
            "Bob Jones",
            "Bob",
            "Jones",
            "Bob",
            department: "Engineering",
            jobTitle: "Engineer",
            location: null,
            country: null,
            city: null,
            dateOfBirth: null,
            hireDate: null,
            workEmail: null,
            manager: new ManagerReference(managerEmployeeId, null));
        var profilesByEmployeeId = new Dictionary<EmployeeId, EmployeeProfile>
        {
            [managerEmployeeId] = managerProfile
        };

        // Act
        var managerName = employeeProfile.ResolveManagerDisplayName(profilesByEmployeeId);

        // Assert
        managerName.Should().Be("Alice Smith");
    }

    private static EmployeeProfile CreateProfile(
        string displayName,
        string? location = null,
        string? country = null,
        string? city = null)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(1),
            displayName,
            names[0],
            names[^1],
            names[0],
            department: "Engineering",
            jobTitle: "Engineer",
            location,
            country,
            city,
            dateOfBirth: null,
            hireDate: null,
            workEmail: null,
            manager: default);
    }

    private static EmployeeProfile CreateProfileWithPreferredName(string displayName, string preferredName)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(1),
            displayName,
            names[0],
            names[^1],
            preferredName,
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
