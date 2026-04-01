using FluentAssertions;

using Models;

namespace Logic.Tests;

public sealed class EmployeeAvailabilityResolverTests
{
    [Fact(DisplayName = "The holiday-country mapping builder removes blank values, deduplicates entries, and sorts countries case-insensitively.")]
    [Trait("Category", "Unit")]
    public void BuildHolidayCountryMappingsShouldNormalizeConfiguredCountries()
    {
        // Arrange
        var resolver = new EmployeeAvailabilityResolver();
        Dictionary<string, string[]> configuredMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Europe Day"] = ["Germany", "", "malta", "Germany", "  ", "Austria"]
        };

        // Act
        var mappings = resolver.BuildHolidayCountryMappings(configuredMappings);

        // Assert
        mappings.Should().ContainSingle();
        mappings["Europe Day"].Should().Equal("Austria", "Germany", "malta");
    }

    [Fact(DisplayName = "The holiday entry builder deduplicates repeated BambooHR holidays and attaches configured countries.")]
    [Trait("Category", "Unit")]
    public void BuildHolidayEntriesShouldDeduplicateEntriesAndIncludeMappedCountries()
    {
        // Arrange
        var resolver = new EmployeeAvailabilityResolver();
        IReadOnlyList<TimeOffEntry> whoIsOut =
        [
            CreateHolidayEntry(20, "Founders Day", 2026, 3, 21, 2026, 3, 21),
            CreateHolidayEntry(10, "Founders Day", 2026, 3, 21, 2026, 3, 21),
            CreateHolidayEntry(30, "Unity Day", 2026, 3, 22, 2026, 3, 22),
            CreateTimeOffEntry(40, 2, "Alice Smith", 2026, 3, 20, 2026, 3, 20)
        ];
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Founders Day"] = ["Germany", "Malta"]
        };

        // Act
        var holidays = resolver.BuildHolidayEntries(whoIsOut, holidayCountryMappings);

        // Assert
        holidays.Should().HaveCount(2);
        holidays[0].Name.Should().Be("Founders Day");
        holidays[0].AssociatedCountries.Should().Equal("Germany", "Malta");
        holidays[1].Name.Should().Be("Unity Day");
        holidays[1].AssociatedCountries.Should().BeEmpty();
    }

    [Fact(DisplayName = "The employee entry builder combines personal time off with country-matched holidays in chronological order.")]
    [Trait("Category", "Unit")]
    public void BuildEmployeeEntriesShouldCombinePersonalAndHolidayEntriesForMatchingCountries()
    {
        // Arrange
        var resolver = new EmployeeAvailabilityResolver();
        var alice = CreateProfile(1, "Alice Smith", country: "Germany");
        var branko = CreateProfile(2, "Branko Borg", country: "Malta");
        IReadOnlyCollection<EmployeeProfile> includedProfiles = [alice, branko];
        IReadOnlyList<TimeOffEntry> whoIsOut =
        [
            CreateHolidayEntry(300, "Malta National Day", 2026, 9, 21, 2026, 9, 21),
            CreateHolidayEntry(200, "German Unity Day", 2026, 9, 20, 2026, 9, 20),
            CreateHolidayEntry(100, "Malta National Day", 2026, 9, 21, 2026, 9, 21),
            CreateTimeOffEntry(50, 2, "Branko Borg", 2026, 9, 19, 2026, 9, 19)
        ];
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Malta National Day"] = ["Malta"],
            ["German Unity Day"] = ["Germany"]
        };

        // Act
        var entriesByEmployee = resolver.BuildEmployeeEntries(
            includedProfiles,
            whoIsOut,
            holidayCountryMappings);

        // Assert
        entriesByEmployee.Should().HaveCount(2);
        entriesByEmployee[alice.EmployeeId]
            .Select(entry => (entry.Name, entry.Type, entry.Id))
            .Should()
            .Equal(("German Unity Day", TimeOffEntryType.Holiday, 200));
        entriesByEmployee[branko.EmployeeId]
            .Select(entry => (entry.Name, entry.Type, entry.Id))
            .Should()
            .Equal(
                ("Branko Borg", TimeOffEntryType.TimeOff, 50),
                ("Malta National Day", TimeOffEntryType.Holiday, 100));
    }

    private static TimeOffEntry CreateHolidayEntry(
        int id,
        string name,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
        => new(
            id,
            TimeOffEntryType.Holiday,
            employeeId: null,
            name,
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay));

    private static TimeOffEntry CreateTimeOffEntry(
        int id,
        int employeeId,
        string name,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
        => new(
            id,
            TimeOffEntryType.TimeOff,
            new EmployeeId(employeeId),
            name,
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay));

    private static EmployeeProfile CreateProfile(
        int employeeId,
        string displayName,
        string? country = null,
        string? location = null)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(employeeId),
            displayName,
            names[0],
            names[^1],
            names[0],
            department: "Engineering",
            jobTitle: "Engineer",
            location,
            country,
            city: null,
            dateOfBirth: null,
            hireDate: null,
            workEmail: null,
            manager: default);
    }
}
