using Models;

namespace Infrastructure.Tests;

internal static class ReportTestData
{
    public static HierarchyReport CreateReport()
    {
        var rootEmployeeId = new EmployeeId(1);
        var childEmployeeId = new EmployeeId(2);
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 7));
        var generatedAt = new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero);
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
            new HierarchyReportRow(
                0,
                rootEmployeeId,
                "Alice & Smith",
                "Leadership",
                "Director",
                "Berlin, Germany",
                new DateOnly(1985, 5, 10),
                new DateOnly(2018, 1, 1),
                null,
                [],
                "alice@example.com",
                "Leadership Team"),
            new HierarchyReportRow(
                1,
                childEmployeeId,
                "Bob Jones",
                "Engineering",
                "Senior Engineer",
                "Munich, Germany",
                new DateOnly(1993, 2, 20),
                new DateOnly(2026, 3, 20),
                "Alice & Smith",
                [
                    new TimeOffEntry(
                        100,
                        TimeOffEntryType.TimeOff,
                        childEmployeeId,
                        "Bob Jones",
                        new DateOnly(2026, 4, 2),
                        new DateOnly(2026, 4, 3))
                ],
                "bob@example.com",
                "ADF Processing Team")
        ];
        IReadOnlyList<HierarchyReportRow> recentHires = [rows[1]];
        IReadOnlyList<HierarchyTeam> teams =
        [
            new HierarchyTeam(
                rootEmployeeId,
                "Alice & Smith",
                ["Bob Jones"],
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Director"] = 1,
                    ["Senior"] = 1
                },
                rows)
        ];
        IReadOnlyDictionary<string, int> locationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = 2
        };
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts =
            new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Germany"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Berlin"] = 1,
                    ["Munich"] = 1
                }
            };
        IReadOnlyDictionary<string, int> ageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["25-34"] = 1,
            ["35-44"] = 1
        };
        IReadOnlyDictionary<string, int> tenureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["<1 year"] = 1,
            ["6-10 years"] = 1
        };

        return new HierarchyReport(
            new HierarchyReportOverview(
                generatedAt,
                availabilityWindow,
                "Alice & Smith",
                relationshipField),
            new HierarchyReportHierarchy(holidays, rows),
            new HierarchyReportSummaries(recentHires, 30, teams),
            new HierarchyReportDistributions(
                locationCounts,
                countryCityCounts,
                ageCounts,
                tenureCounts));
    }
}
