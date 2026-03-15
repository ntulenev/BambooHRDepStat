using Abstractions;

using Models;

namespace Logic.Tests;

public sealed class HierarchyReportBuilderTests
{
    [Fact]
    public async Task BuildAsyncReturnsRootHierarchyOrderedByDepth()
    {
        var client = new FakeBambooHrClient(
            [
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text")
            ],
            [
                new BasicEmployee(1, "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active"),
                new BasicEmployee(2, "Bob Jones", "Bob", "Jones", "Bob", "Manager", "Active"),
                new BasicEmployee(3, "Carol Brown", "Carol", "Brown", "Carol", "Engineer", "Active"),
                new BasicEmployee(4, "Diana White", "Diana", "White", "Diana", "Analyst", "Active"),
                new BasicEmployee(5, "Frank Green", "Frank", "Green", "Frank", "Analyst", "Active")
            ],
            new Dictionary<int, IReadOnlyDictionary<string, string?>>
            {
                [1] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Leadership",
                    ["jobTitle"] = "Director"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Manager",
                    ["reportsTo"] = "Alice Smith"
                },
                [3] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Bob Jones"
                },
                [4] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Operations",
                    ["jobTitle"] = "Analyst",
                    ["reportsTo"] = "Alice Smith"
                },
                [5] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Operations",
                    ["jobTitle"] = "Analyst",
                    ["reportsTo"] = "Missing Manager"
                }
            },
            [
                new TimeOffEntry(
                    100,
                    TimeOffEntryType.TimeOff,
                    employeeId: 2,
                    "Bob Jones",
                    new DateOnly(2026, 3, 17),
                    new DateOnly(2026, 3, 18)),
                new TimeOffEntry(
                    200,
                    TimeOffEntryType.Holiday,
                    employeeId: null,
                    "Founders Day",
                    new DateOnly(2026, 3, 19),
                    new DateOnly(2026, 3, 19))
            ]);

        var builder = new HierarchyReportBuilder(
            client,
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 3, 16),
                new DateOnly(2026, 3, 20))),
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        string[] expectedNames = ["Alice Smith", "Bob Jones", "Carol Brown", "Diana White"];
        int[] expectedLevels = [0, 1, 2, 1];

        Assert.Equal(expectedNames, report.Rows.Select(row => row.DisplayName).ToArray());
        Assert.Equal(expectedLevels, report.Rows.Select(row => row.Level).ToArray());
        _ = Assert.Single(report.Rows[0].UnavailabilityEntries);
        Assert.Equal(2, report.Rows[1].UnavailabilityEntries.Count);
        Assert.Equal("Alice Smith", report.RootEmployeeName);
        Assert.False(report.RelationshipField.UsesEmployeeId);
    }

    private sealed class FakeBambooHrClient : IBambooHrClient
    {
        private readonly IReadOnlyList<BambooHrField> _fields;
        private readonly IReadOnlyList<BasicEmployee> _employees;
        private readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string?>> _employeeFields;
        private readonly IReadOnlyList<TimeOffEntry> _timeOffEntries;

        public FakeBambooHrClient(
            IReadOnlyList<BambooHrField> fields,
            IReadOnlyList<BasicEmployee> employees,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, string?>> employeeFields,
            IReadOnlyList<TimeOffEntry> timeOffEntries)
        {
            _fields = fields;
            _employees = employees;
            _employeeFields = employeeFields;
            _timeOffEntries = timeOffEntries;
        }

        public Task<IReadOnlyList<BambooHrField>> GetFieldsAsync(CancellationToken ct)
        {
            _ = ct;
            return Task.FromResult(_fields);
        }

        public Task<IReadOnlyList<BasicEmployee>> GetEmployeesAsync(CancellationToken ct)
        {
            _ = ct;
            return Task.FromResult(_employees);
        }

        public Task<EmployeeFieldValues> GetEmployeeFieldsAsync(
            int employeeId,
            IReadOnlyCollection<string> fieldKeys,
            CancellationToken ct)
        {
            _ = fieldKeys;
            _ = ct;
            return Task.FromResult(new EmployeeFieldValues(employeeId, _employeeFields[employeeId]));
        }

        public Task<IReadOnlyList<TimeOffEntry>> GetWhosOutAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken ct)
        {
            _ = startDate;
            _ = endDate;
            _ = ct;
            return Task.FromResult(_timeOffEntries);
        }
    }

    private sealed class StubWorkWeekProvider : IWorkWeekProvider
    {
        private readonly WorkWeek _workWeek;

        public StubWorkWeekProvider(WorkWeek workWeek)
        {
            _workWeek = workWeek;
        }

        public WorkWeek GetWorkWeek(DateTimeOffset currentDate)
        {
            _ = currentDate;
            return _workWeek;
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _currentDate;

        public FakeTimeProvider(DateTimeOffset currentDate)
        {
            _currentDate = currentDate;
        }

        public override DateTimeOffset GetUtcNow() => _currentDate.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
