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
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text"),
                new BambooHrField("location", "Location", "location", "text"),
                new BambooHrField("country", "Country", "country", "text"),
                new BambooHrField("city", "City", "city", "text"),
                new BambooHrField("dateOfBirth", "Date of Birth", "dateOfBirth", "date"),
                new BambooHrField("hireDate", "Hire Date", "hireDate", "date")
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
                    ["jobTitle"] = "Director",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin",
                    ["dateOfBirth"] = "1980-01-10",
                    ["hireDate"] = "2012-02-01"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Manager",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Munich, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Munich",
                    ["dateOfBirth"] = "1990-03-12",
                    ["hireDate"] = "2020-01-15"
                },
                [3] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Bob Jones",
                    ["location"] = "London, United Kingdom",
                    ["country"] = "United Kingdom",
                    ["city"] = "London",
                    ["dateOfBirth"] = "1998-06-01",
                    ["hireDate"] = "2024-06-01"
                },
                [4] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Operations",
                    ["jobTitle"] = "Analyst",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin",
                    ["dateOfBirth"] = "1988-09-20",
                    ["hireDate"] = "2018-03-01"
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
            CreateOptions(),
            new NoOpLoadingNotifier(),
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 3, 16),
                new DateOnly(2026, 3, 20))),
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        string[] expectedNames = ["Alice Smith", "Bob Jones", "Carol Brown", "Diana White"];
        int[] expectedLevels = [0, 1, 2, 1];

        Assert.Equal(expectedNames, report.Rows.Select(row => row.DisplayName).ToArray());
        Assert.Equal(expectedLevels, report.Rows.Select(row => row.Level).ToArray());
        Assert.Equal(
            new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero),
            report.GeneratedAt);
        Assert.Empty(report.Rows[0].UnavailabilityEntries);
        _ = Assert.Single(report.Rows[1].UnavailabilityEntries);
        Assert.Equal("Alice Smith", report.RootEmployeeName);
        Assert.False(report.RelationshipField.UsesEmployeeId);
        Assert.Equal(3, report.LocationCounts["Germany"]);
        Assert.Equal(1, report.LocationCounts["United Kingdom"]);
        Assert.Equal(2, report.CountryCityCounts["Germany"]["Berlin"]);
        Assert.Equal(1, report.CountryCityCounts["Germany"]["Munich"]);
        Assert.Equal(1, report.CountryCityCounts["United Kingdom"]["London"]);
        Assert.Equal(1, report.AgeCounts["25-34"]);
        Assert.Equal(2, report.AgeCounts["35-44"]);
        Assert.Equal(1, report.AgeCounts["45-54"]);
        Assert.Equal(1, report.TenureCounts["1-2 years"]);
        Assert.Equal(2, report.TenureCounts["6-10 years"]);
        Assert.Equal(1, report.TenureCounts["10+ years"]);
        Assert.Collection(
            report.Teams,
            team =>
            {
                Assert.Equal("Alice Smith", team.ManagerDisplayName);
                Assert.Equal(["Diana White"], team.MemberDisplayNames);
                Assert.Equal(2, team.PeopleCount);
                Assert.Equal(1, team.GradeCounts["Analyst"]);
                Assert.Equal(1, team.GradeCounts["Director"]);
            },
            team =>
            {
                Assert.Equal("Bob Jones", team.ManagerDisplayName);
                Assert.Equal(["Carol Brown"], team.MemberDisplayNames);
                Assert.Equal(2, team.PeopleCount);
                Assert.Equal(1, team.GradeCounts["Engineer"]);
                Assert.Equal(1, team.GradeCounts["Manager"]);
            });
    }

    [Fact]
    public async Task BuildAsyncReturnsRecentHiresWithinConfiguredPeriodSortedByStartDate()
    {
        var client = new FakeBambooHrClient(
            [
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text"),
                new BambooHrField("location", "Location", "location", "text"),
                new BambooHrField("country", "Country", "country", "text"),
                new BambooHrField("city", "City", "city", "text"),
                new BambooHrField("dateOfBirth", "Date of Birth", "dateOfBirth", "date"),
                new BambooHrField("hireDate", "Hire Date", "hireDate", "date")
            ],
            [
                new BasicEmployee(1, "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active"),
                new BasicEmployee(2, "Bob Jones", "Bob", "Jones", "Bob", "Manager", "Active"),
                new BasicEmployee(3, "Carol Brown", "Carol", "Brown", "Carol", "Engineer", "Active"),
                new BasicEmployee(4, "Diana White", "Diana", "White", "Diana", "Engineer", "Active"),
                new BasicEmployee(5, "Evan Black", "Evan", "Black", "Evan", "Engineer", "Active")
            ],
            new Dictionary<int, IReadOnlyDictionary<string, string?>>
            {
                [1] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Leadership",
                    ["jobTitle"] = "Director",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin",
                    ["dateOfBirth"] = "1980-01-10",
                    ["hireDate"] = "2012-02-01"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Manager",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Munich, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Munich",
                    ["dateOfBirth"] = "1990-03-12",
                    ["hireDate"] = "2026-03-16"
                },
                [3] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Bob Jones",
                    ["location"] = "London, United Kingdom",
                    ["country"] = "United Kingdom",
                    ["city"] = "London",
                    ["dateOfBirth"] = "1998-06-01",
                    ["hireDate"] = "2026-03-05"
                },
                [4] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Prague, Czechia",
                    ["country"] = "Czechia",
                    ["city"] = "Prague",
                    ["dateOfBirth"] = "1995-07-20",
                    ["hireDate"] = "2026-03-02"
                },
                [5] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Operations",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Rome, Italy",
                    ["country"] = "Italy",
                    ["city"] = "Rome",
                    ["dateOfBirth"] = "1991-11-11",
                    ["hireDate"] = "2026-02-14"
                }
            },
            []);

        var builder = new HierarchyReportBuilder(
            client,
            CreateOptions(recentHirePeriodDays: 15),
            new NoOpLoadingNotifier(),
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 3, 16),
                new DateOnly(2026, 3, 20))),
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        Assert.Equal(15, report.RecentHirePeriodDays);
        Assert.Equal(
            ["Bob Jones", "Carol Brown", "Diana White"],
            [.. report.RecentHires.Select(row => row.DisplayName)]);
        Assert.Equal(
            [new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 2)],
            [.. report.RecentHires.Select(row => row.EmploymentStartDate!.Value)]);
        Assert.All(report.RecentHires, row => Assert.NotNull(row.ManagerName));
        Assert.Equal("Alice Smith", report.RecentHires[0].ManagerName);
        Assert.Equal("Bob Jones", report.RecentHires[1].ManagerName);
    }

    [Fact]
    public async Task BuildAsyncCreatesTeamsFromLeafDirectReportsOnly()
    {
        var client = new FakeBambooHrClient(
            [
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text"),
                new BambooHrField("location", "Location", "location", "text"),
                new BambooHrField("country", "Country", "country", "text"),
                new BambooHrField("city", "City", "city", "text"),
                new BambooHrField("dateOfBirth", "Date of Birth", "dateOfBirth", "date"),
                new BambooHrField("hireDate", "Hire Date", "hireDate", "date")
            ],
            [
                new BasicEmployee(1, "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active"),
                new BasicEmployee(2, "Bob Jones", "Bob", "Jones", "Bob", "Manager", "Active"),
                new BasicEmployee(3, "Carol Brown", "Carol", "Brown", "Carol", "Engineer", "Active"),
                new BasicEmployee(4, "Diana White", "Diana", "White", "Diana", "Engineer", "Active"),
                new BasicEmployee(5, "Evan Black", "Evan", "Black", "Evan", "Engineer", "Active"),
                new BasicEmployee(6, "Fiona Gray", "Fiona", "Gray", "Fiona", "Engineer", "Active")
            ],
            new Dictionary<int, IReadOnlyDictionary<string, string?>>
            {
                [1] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Leadership",
                    ["jobTitle"] = "Director",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin",
                    ["dateOfBirth"] = "1981-05-11",
                    ["hireDate"] = "2013-01-01"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Manager",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Munich, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Munich",
                    ["dateOfBirth"] = "1989-10-01",
                    ["hireDate"] = "2022-07-01"
                },
                [3] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "London, United Kingdom",
                    ["country"] = "United Kingdom",
                    ["city"] = "London",
                    ["dateOfBirth"] = "2001-10-01",
                    ["hireDate"] = "2025-01-10"
                },
                [4] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Prague, Czechia",
                    ["country"] = "Czechia",
                    ["city"] = "Prague",
                    ["dateOfBirth"] = "1997-12-12",
                    ["hireDate"] = "2021-05-10"
                },
                [5] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Bob Jones",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin",
                    ["dateOfBirth"] = "1994-03-08",
                    ["hireDate"] = "2019-03-08"
                },
                [6] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Bob Jones",
                    ["location"] = "Hamburg, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Hamburg",
                    ["dateOfBirth"] = "1985-08-08",
                    ["hireDate"] = "2010-08-08"
                }
            },
            []);

        var builder = new HierarchyReportBuilder(
            client,
            CreateOptions(),
            new NoOpLoadingNotifier(),
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 3, 16),
                new DateOnly(2026, 3, 20))),
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 16, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        Assert.Equal(4, report.LocationCounts["Germany"]);
        Assert.Equal(1, report.LocationCounts["United Kingdom"]);
        Assert.Equal(1, report.LocationCounts["Czechia"]);
        Assert.Equal(2, report.CountryCityCounts["Germany"]["Berlin"]);
        Assert.Equal(1, report.CountryCityCounts["Germany"]["Munich"]);
        Assert.Equal(1, report.CountryCityCounts["Germany"]["Hamburg"]);
        Assert.Equal(1, report.CountryCityCounts["United Kingdom"]["London"]);
        Assert.Equal(1, report.CountryCityCounts["Czechia"]["Prague"]);
        Assert.Equal(1, report.AgeCounts["<25"]);
        Assert.Equal(2, report.AgeCounts["25-34"]);
        Assert.Equal(3, report.AgeCounts["35-44"]);
        Assert.Equal(1, report.TenureCounts["1-2 years"]);
        Assert.Equal(2, report.TenureCounts["3-5 years"]);
        Assert.Equal(1, report.TenureCounts["6-10 years"]);
        Assert.Equal(2, report.TenureCounts["10+ years"]);
        Assert.Collection(
            report.Teams,
            team =>
            {
                Assert.Equal("Alice Smith", team.ManagerDisplayName);
                Assert.Equal(["Carol Brown", "Diana White"], team.MemberDisplayNames);
                Assert.Equal(3, team.PeopleCount);
                Assert.Equal(1, team.GradeCounts["Director"]);
                Assert.Equal(2, team.GradeCounts["Engineer"]);
            },
            team =>
            {
                Assert.Equal("Bob Jones", team.ManagerDisplayName);
                Assert.Equal(["Evan Black", "Fiona Gray"], team.MemberDisplayNames);
                Assert.Equal(3, team.PeopleCount);
                Assert.Equal(2, team.GradeCounts["Engineer"]);
                Assert.Equal(1, team.GradeCounts["Manager"]);
            });
    }

    [Fact]
    public async Task BuildAsyncAssignsCountrySpecificHolidayOnlyToMatchingEmployees()
    {
        var client = new FakeBambooHrClient(
            [
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text"),
                new BambooHrField("location", "Location", "location", "text"),
                new BambooHrField("country", "Country", "country", "text"),
                new BambooHrField("city", "City", "city", "text")
            ],
            [
                new BasicEmployee(1, "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active"),
                new BasicEmployee(2, "Branko Borg", "Branko", "Borg", "Branko", "Engineer", "Active"),
                new BasicEmployee(3, "Clara Weiss", "Clara", "Weiss", "Clara", "Engineer", "Active")
            ],
            new Dictionary<int, IReadOnlyDictionary<string, string?>>
            {
                [1] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Leadership",
                    ["jobTitle"] = "Director",
                    ["location"] = "Berlin, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Berlin"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Valletta, Malta",
                    ["country"] = "Malta",
                    ["city"] = "Valletta"
                },
                [3] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Hamburg, Germany",
                    ["country"] = "Germany",
                    ["city"] = "Hamburg"
                }
            },
            [
                new TimeOffEntry(
                    500,
                    TimeOffEntryType.Holiday,
                    employeeId: null,
                    "Malta National Day",
                    new DateOnly(2026, 9, 21),
                    new DateOnly(2026, 9, 21))
            ]);

        var builder = new HierarchyReportBuilder(
            client,
            CreateOptions(),
            new NoOpLoadingNotifier(),
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 9, 21),
                new DateOnly(2026, 9, 25))),
            new FakeTimeProvider(new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        Assert.Empty(report.Rows.Single(row => row.EmployeeId == 1).UnavailabilityEntries);
        Assert.Empty(report.Rows.Single(row => row.EmployeeId == 3).UnavailabilityEntries);

        var maltaEmployeeEntries = report.Rows.Single(row => row.EmployeeId == 2).UnavailabilityEntries;
        var holiday = Assert.Single(maltaEmployeeEntries);
        Assert.Equal(TimeOffEntryType.Holiday, holiday.Type);
        Assert.Equal("Malta National Day", holiday.Name);
    }

    [Fact]
    public async Task BuildAsyncAssignsSharedHolidayToSingleCountryHierarchy()
    {
        var client = new FakeBambooHrClient(
            [
                new BambooHrField("reportsTo", "Reports To", "reportsTo", "text"),
                new BambooHrField("location", "Location", "location", "text"),
                new BambooHrField("country", "Country", "country", "text"),
                new BambooHrField("city", "City", "city", "text")
            ],
            [
                new BasicEmployee(1, "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active"),
                new BasicEmployee(2, "Branko Borg", "Branko", "Borg", "Branko", "Engineer", "Active")
            ],
            new Dictionary<int, IReadOnlyDictionary<string, string?>>
            {
                [1] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Leadership",
                    ["jobTitle"] = "Director",
                    ["location"] = "Valletta, Malta",
                    ["country"] = "Malta",
                    ["city"] = "Valletta"
                },
                [2] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["department"] = "Engineering",
                    ["jobTitle"] = "Engineer",
                    ["reportsTo"] = "Alice Smith",
                    ["location"] = "Sliema, Malta",
                    ["country"] = "Malta",
                    ["city"] = "Sliema"
                }
            },
            [
                new TimeOffEntry(
                    700,
                    TimeOffEntryType.Holiday,
                    employeeId: null,
                    "Republic Day",
                    new DateOnly(2026, 12, 13),
                    new DateOnly(2026, 12, 13))
            ]);

        var builder = new HierarchyReportBuilder(
            client,
            CreateOptions(),
            new NoOpLoadingNotifier(),
            new StubWorkWeekProvider(new WorkWeek(
                new DateOnly(2026, 12, 14),
                new DateOnly(2026, 12, 18))),
            new FakeTimeProvider(new DateTimeOffset(2026, 12, 14, 8, 0, 0, TimeSpan.Zero)));

        var report = await builder.BuildAsync(1, CancellationToken.None);

        foreach (var row in report.Rows)
        {
            var holiday = Assert.Single(row.UnavailabilityEntries);
            Assert.Equal(TimeOffEntryType.Holiday, holiday.Type);
            Assert.Equal("Republic Day", holiday.Name);
        }
    }

    private static BambooHrOptions CreateOptions(int recentHirePeriodDays = 30)
    {
        return new BambooHrOptions
        {
            Organization = "test",
            Token = "token",
            EmployeeId = 1,
            RecentHirePeriodDays = recentHirePeriodDays
        };
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

    private sealed class NoOpLoadingNotifier : ILoadingNotifier
    {
        public Task<T> RunAsync<T>(
            string description,
            Func<CancellationToken, Task<T>> action,
            CancellationToken ct)
        {
            _ = description;
            return action(ct);
        }

        public void SetProgress(string description, int completed, int total)
        {
            _ = description;
            _ = completed;
            _ = total;
        }

        public void SetStatus(string description)
        {
            _ = description;
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
