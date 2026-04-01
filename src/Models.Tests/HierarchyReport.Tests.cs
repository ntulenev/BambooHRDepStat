using FluentAssertions;

namespace Models.Tests;

public sealed class HierarchyReportTests
{
    [Fact(DisplayName = "The hierarchy report exposes the availability window from the overview.")]
    [Trait("Category", "Unit")]
    public void AvailabilityWindowShouldExposeOverviewValue()
    {
        // Arrange
        var (report, availabilityWindow, _, _, _, _) = CreateReport();

        // Act
        var value = report.AvailabilityWindow;

        // Assert
        value.Should().BeSameAs(availabilityWindow);
    }

    [Fact(DisplayName = "The hierarchy report exposes the relationship field from the overview.")]
    [Trait("Category", "Unit")]
    public void RelationshipFieldShouldExposeOverviewValue()
    {
        // Arrange
        var (report, _, relationshipField, _, _, _) = CreateReport();

        // Act
        var value = report.RelationshipField;

        // Assert
        value.Should().BeSameAs(relationshipField);
    }

    [Fact(DisplayName = "The hierarchy report exposes holidays from the hierarchy section.")]
    [Trait("Category", "Unit")]
    public void HolidaysShouldExposeHierarchyValue()
    {
        // Arrange
        var (report, _, _, holidays, _, _) = CreateReport();

        // Act
        var value = report.Holidays;

        // Assert
        value.Should().BeSameAs(holidays);
    }

    [Fact(DisplayName = "The hierarchy report exposes rows from the hierarchy section.")]
    [Trait("Category", "Unit")]
    public void RowsShouldExposeHierarchyValue()
    {
        // Arrange
        var (report, _, _, _, rows, _) = CreateReport();

        // Act
        var value = report.Rows;

        // Assert
        value.Should().BeSameAs(rows);
    }

    [Fact(DisplayName = "The hierarchy report exposes recent hires from the summaries section.")]
    [Trait("Category", "Unit")]
    public void RecentHiresShouldExposeSummariesValue()
    {
        // Arrange
        var (report, _, _, _, rows, _) = CreateReport();

        // Act
        var value = report.RecentHires;

        // Assert
        value.Should().BeSameAs(rows);
    }

    [Fact(DisplayName = "The hierarchy report exposes teams from the summaries section.")]
    [Trait("Category", "Unit")]
    public void TeamsShouldExposeSummariesValue()
    {
        // Arrange
        var (report, _, _, _, _, teams) = CreateReport();

        // Act
        var value = report.Teams;

        // Assert
        value.Should().BeSameAs(teams);
    }

    [Fact(DisplayName = "The hierarchy report exposes the root employee name from the overview.")]
    [Trait("Category", "Unit")]
    public void RootEmployeeNameShouldExposeOverviewValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.RootEmployeeName;

        // Assert
        value.Should().Be("Alice Smith");
    }

    [Fact(DisplayName = "The hierarchy report exposes the recent-hire period length from the summaries.")]
    [Trait("Category", "Unit")]
    public void RecentHirePeriodDaysShouldExposeSummariesValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.RecentHirePeriodDays;

        // Assert
        value.Should().Be(30);
    }

    [Fact(DisplayName = "The hierarchy report exposes location counts from the distributions section.")]
    [Trait("Category", "Unit")]
    public void LocationCountsShouldExposeDistributionValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.LocationCounts["Germany"];

        // Assert
        value.Should().Be(1);
    }

    [Fact(DisplayName = "The hierarchy report exposes country-city counts from the distributions section.")]
    [Trait("Category", "Unit")]
    public void CountryCityCountsShouldExposeDistributionValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.CountryCityCounts["Germany"]["Berlin"];

        // Assert
        value.Should().Be(1);
    }

    [Fact(DisplayName = "The hierarchy report exposes age counts from the distributions section.")]
    [Trait("Category", "Unit")]
    public void AgeCountsShouldExposeDistributionValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.AgeCounts["35-44"];

        // Assert
        value.Should().Be(1);
    }

    [Fact(DisplayName = "The hierarchy report exposes tenure counts from the distributions section.")]
    [Trait("Category", "Unit")]
    public void TenureCountsShouldExposeDistributionValue()
    {
        // Arrange
        var (report, _, _, _, _, _) = CreateReport();

        // Act
        var value = report.TenureCounts["6-10 years"];

        // Assert
        value.Should().Be(1);
    }

    private static (
        HierarchyReport Report,
        AvailabilityWindow AvailabilityWindow,
        HierarchyRelationshipField RelationshipField,
        IReadOnlyList<HolidayReportItem> Holidays,
        IReadOnlyList<HierarchyReportRow> Rows,
        IReadOnlyList<HierarchyTeam> Teams) CreateReport()
    {
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 8));
        var relationshipField = new HierarchyRelationshipField(
            "reportsTo",
            "Reports To",
            usesEmployeeId: false);
        IReadOnlyList<HolidayReportItem> holidays =
        [
            new HolidayReportItem(
                "Founders Day",
                new DateOnly(2026, 4, 3),
                new DateOnly(2026, 4, 3),
                ["Germany"])
        ];
        IReadOnlyList<HierarchyReportRow> rows =
        [
            new(
                0,
                new EmployeeId(1),
                "Alice Smith",
                department: "Leadership",
                jobTitle: "Director",
                location: "Berlin, Germany",
                dateOfBirth: null,
                employmentStartDate: null,
                managerName: null,
                [])
        ];
        IReadOnlyList<HierarchyTeam> teams =
        [
            new(
                new EmployeeId(1),
                "Alice Smith",
                ["Bob Jones"],
                new Dictionary<string, int> { ["Director"] = 1, ["Engineer"] = 1 })
        ];
        var report = new HierarchyReport(
            new HierarchyReportOverview(
                new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero),
                availabilityWindow,
                "Alice Smith",
                relationshipField),
            new HierarchyReportHierarchy(holidays, rows),
            new HierarchyReportSummaries(rows, 30, teams),
            new HierarchyReportDistributions(
                new Dictionary<string, int> { ["Germany"] = 1 },
                new Dictionary<string, IReadOnlyDictionary<string, int>>
                {
                    ["Germany"] = new Dictionary<string, int> { ["Berlin"] = 1 }
                },
                new Dictionary<string, int> { ["35-44"] = 1 },
                new Dictionary<string, int> { ["6-10 years"] = 1 }));

        return (report, availabilityWindow, relationshipField, holidays, rows, teams);
    }
}
