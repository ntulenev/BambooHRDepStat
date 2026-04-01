using FluentAssertions;

using Models;

namespace Logic.Tests;

public sealed class HierarchyAnalyticsTests
{
    [Fact(DisplayName = "The recent-hire builder keeps only hires inside the configured period and sorts them by start date descending, then by name.")]
    [Trait("Category", "Unit")]
    public void BuildRecentHiresShouldFilterAndSortRowsWithinConfiguredWindow()
    {
        // Arrange
        var analytics = new HierarchyAnalytics();
        IReadOnlyList<HierarchyReportRow> rows =
        [
            CreateRow(1, "Alice Smith", employmentStartDate: new DateOnly(2026, 4, 1)),
            CreateRow(2, "Bob Jones", employmentStartDate: new DateOnly(2026, 3, 28)),
            CreateRow(3, "Carol Brown", employmentStartDate: new DateOnly(2026, 3, 28)),
            CreateRow(4, "Diana White", employmentStartDate: new DateOnly(2026, 3, 20)),
            CreateRow(5, "Evan Black", employmentStartDate: null)
        ];

        // Act
        var recentHires = analytics.BuildRecentHires(
            rows,
            new DateOnly(2026, 4, 1),
            recentHirePeriodDays: 5);

        // Assert
        recentHires.Select(row => row.DisplayName).Should().Equal(
            "Alice Smith",
            "Bob Jones",
            "Carol Brown");
    }

    [Fact(DisplayName = "The team builder includes only leaf direct reports and derives grade counts from the manager and included leaf members.")]
    [Trait("Category", "Unit")]
    public void BuildTeamsShouldCreateTeamsFromLeafDirectReportsOnly()
    {
        // Arrange
        var analytics = new HierarchyAnalytics();
        var aliceEmployeeId = new EmployeeId(1);
        var bobEmployeeId = new EmployeeId(2);
        var carolEmployeeId = new EmployeeId(3);
        var dianaEmployeeId = new EmployeeId(4);
        var rows = new[]
        {
            CreateRow(1, "Alice Smith", jobTitle: "Director"),
            CreateRow(2, "Bob Jones", jobTitle: "Engineering Manager"),
            CreateRow(3, "Carol Brown", jobTitle: "Senior Engineer"),
            CreateRow(4, "Diana White", jobTitle: "Junior Engineer")
        };
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId = new Dictionary<EmployeeId, EmployeeProfile>
        {
            [aliceEmployeeId] = CreateProfile(1, "Alice Smith", "Director"),
            [bobEmployeeId] = CreateProfile(2, "Bob Jones", "Engineering Manager", managerEmployeeId: 1),
            [carolEmployeeId] = CreateProfile(3, "Carol Brown", "Senior Engineer", managerEmployeeId: 1),
            [dianaEmployeeId] = CreateProfile(4, "Diana White", "Junior Engineer", managerEmployeeId: 2)
        };
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager = new Dictionary<EmployeeId, IReadOnlyList<EmployeeId>>
        {
            [aliceEmployeeId] = [bobEmployeeId, carolEmployeeId],
            [bobEmployeeId] = [dianaEmployeeId]
        };

        // Act
        var teams = analytics.BuildTeams(rows, profilesByEmployeeId, childrenByManager);

        // Assert
        teams.Should().HaveCount(2);
        teams[0].ManagerDisplayName.Should().Be("Alice Smith");
        teams[0].MemberDisplayNames.Should().Equal("Carol Brown");
        teams[0].GradeCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Director"] = 1,
            ["Senior"] = 1
        });
        teams[1].ManagerDisplayName.Should().Be("Bob Jones");
        teams[1].MemberDisplayNames.Should().Equal("Diana White");
        teams[1].GradeCounts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Junior"] = 1,
            ["Manager"] = 1
        });
    }

    [Fact(DisplayName = "The distribution builders resolve fallback country and city labels and bucket age and tenure values deterministically.")]
    [Trait("Category", "Unit")]
    public void DistributionBuildersShouldResolveFallbackLabelsAndBucketValues()
    {
        // Arrange
        var analytics = new HierarchyAnalytics();
        var referenceDate = new DateOnly(2026, 4, 1);
        var profiles = new[]
        {
            CreateProfile(1, "Alice Smith", "Director", country: "Germany", city: "Berlin", dateOfBirth: new DateOnly(1995, 4, 1), hireDate: new DateOnly(2025, 7, 1)),
            CreateProfile(2, "Bob Jones", "Engineer", location: "Valletta, Malta", dateOfBirth: new DateOnly(1988, 3, 30), hireDate: new DateOnly(2022, 4, 1)),
            CreateProfile(3, "Carol Brown", "Engineer", location: "Remote", dateOfBirth: null, hireDate: null)
        };

        // Act
        var locationCounts = analytics.BuildLocationCounts(profiles);
        var countryCityCounts = analytics.BuildCountryCityCounts(profiles);
        var ageCounts = analytics.BuildAgeCounts(profiles, referenceDate);
        var tenureCounts = analytics.BuildTenureCounts(profiles, referenceDate);

        // Assert
        locationCounts.Should().BeEquivalentTo(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = 1,
            ["Malta"] = 1,
            ["Remote"] = 1
        });
        countryCityCounts.Should().BeEquivalentTo(new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Berlin"] = 1
            },
            ["Malta"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Valletta"] = 1
            }
        });
        ageCounts.Keys.Should().Equal("25-34", "35-44", "Unknown");
        ageCounts["25-34"].Should().Be(1);
        ageCounts["35-44"].Should().Be(1);
        ageCounts["Unknown"].Should().Be(1);
        tenureCounts.Keys.Should().Equal("<1 year", "3-5 years", "Unknown");
        tenureCounts["<1 year"].Should().Be(1);
        tenureCounts["3-5 years"].Should().Be(1);
        tenureCounts["Unknown"].Should().Be(1);
    }

    private static HierarchyReportRow CreateRow(
        int employeeId,
        string displayName,
        string? jobTitle = null,
        DateOnly? employmentStartDate = null)
        => new(
            level: employeeId == 1 ? 0 : 1,
            new EmployeeId(employeeId),
            displayName,
            department: "Engineering",
            jobTitle,
            location: null,
            dateOfBirth: null,
            employmentStartDate,
            managerName: employeeId == 1 ? null : "Alice Smith",
            []);

    private static EmployeeProfile CreateProfile(
        int employeeId,
        string displayName,
        string jobTitle,
        int? managerEmployeeId = null,
        string? country = null,
        string? city = null,
        string? location = null,
        DateOnly? dateOfBirth = null,
        DateOnly? hireDate = null)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(employeeId),
            displayName,
            names[0],
            names[^1],
            names[0],
            department: "Engineering",
            jobTitle,
            location,
            country,
            city,
            dateOfBirth,
            hireDate,
            workEmail: null,
            manager: new ManagerReference(
                managerEmployeeId is null ? null : new EmployeeId(managerEmployeeId.Value),
                null));
    }
}
