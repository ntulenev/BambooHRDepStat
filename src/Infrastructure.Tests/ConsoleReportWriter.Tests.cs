using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

[Collection("Console stack")]
public sealed class ConsoleReportWriterTests
{
    [Fact(DisplayName = "The console writer renders the full report for a populated hierarchy.")]
    [Trait("Category", "Unit")]
    public void RenderShouldWritePopulatedReport()
    {
        // Arrange
        using var scope = new ConsoleStackTestScope();
        var writer = new ConsoleReportWriter(new ReportPresentationFormatter());
        var report = ReportTestData.CreateReport();

        // Act
        writer.Render(report);
        var output = string.Join(Environment.NewLine, scope.Console.Lines);

        // Assert
        output.Should().Contain("BambooHR hierarchy report");
        output.Should().Contain("Alice & Smith");
        output.Should().Contain("Bob Jones");
        output.Should().Contain("Holidays");
        output.Should().Contain("Teams");
        output.Should().Contain("Job Titles");
        output.Should().Contain("Location Distribution");
        output.Should().Contain("Team Grade Distribution");
    }

    [Fact(DisplayName = "The console writer renders empty-state sections and rejects an empty hierarchy.")]
    [Trait("Category", "Unit")]
    public void RenderShouldWriteEmptyStatesAndThrowForEmptyHierarchy()
    {
        // Arrange
        using var scope = new ConsoleStackTestScope();
        var writer = new ConsoleReportWriter(new ReportPresentationFormatter());
        var report = CreateEmptyStateReport();

        // Act
        var action = () => writer.Render(report);

        // Assert
        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Hierarchy report is empty.");

        var output = string.Join(Environment.NewLine, scope.Console.Lines);
        output.Should().Contain("BambooHR hierarchy report");
        output.Should().Contain("No holidays found in the availability window.");
        output.Should().Contain("Availability window:");
    }

    private static HierarchyReport CreateEmptyStateReport()
    {
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 7));
        var relationshipField = new HierarchyRelationshipField(
            "reportsTo",
            "Reports To",
            usesEmployeeId: false);
        var overview = new HierarchyReportOverview(
            new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero),
            availabilityWindow,
            "Alice Smith",
            relationshipField);

        return new HierarchyReport(
            overview,
            new HierarchyReportHierarchy([], []),
            new HierarchyReportSummaries([], 30, []),
            new HierarchyReportDistributions(
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)));
    }
}
