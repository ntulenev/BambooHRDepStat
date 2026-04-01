using Abstractions;

using FluentAssertions;

using Moq;

using Models;

namespace Logic.Tests;

public sealed class HierarchyReportBuilderTests
{
    [Fact(DisplayName = "The builder stops immediately when the supplied cancellation token is already canceled.")]
    [Trait("Category", "Unit")]
    public async Task BuildAsyncShouldThrowWhenCancellationIsAlreadyRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var builder = CreateBuilder(
            bambooHrClient: new Mock<IBambooHrClient>(MockBehavior.Strict).Object,
            loadingNotifier: new RecordingLoadingNotifier(),
            availabilityWindowProvider: new Mock<IAvailabilityWindowProvider>(MockBehavior.Strict).Object,
            employeeProfileDirectoryLoader: new Mock<IEmployeeProfileDirectoryLoader>(MockBehavior.Strict).Object,
            hierarchyFieldResolver: new Mock<IHierarchyFieldResolver>(MockBehavior.Strict).Object,
            hierarchyTopologyBuilder: new Mock<IHierarchyTopologyBuilder>(MockBehavior.Strict).Object,
            employeeAvailabilityResolver: new Mock<IEmployeeAvailabilityResolver>(MockBehavior.Strict).Object,
            hierarchyAnalytics: new Mock<IHierarchyAnalytics>(MockBehavior.Strict).Object);

        // Act
        Func<Task> action = () => builder.BuildAsync(new EmployeeId(1), cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "The builder orchestrates collaborators, passes the supplied cancellation token, and returns the composed hierarchy report.")]
    [Trait("Category", "Unit")]
    public async Task BuildAsyncShouldComposeHierarchyReportFromCollaboratorResults()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var generatedAt = new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero);
        var rootEmployeeId = new EmployeeId(1);
        var childEmployeeId = new EmployeeId(2);
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 8));
        var relationshipField = new HierarchyRelationshipField("reportsTo", "Reports To", usesEmployeeId: false);
        var locationField = CreateField("location", "Location");
        var countryField = CreateField("country", "Country");
        var cityField = CreateField("city", "City");
        var birthDateField = CreateField("dateOfBirth", "Date of Birth");
        var hireDateField = CreateField("hireDate", "Hire Date");
        var fieldSelection = new HierarchyFieldSelection(
            relationshipField,
            locationField,
            countryField,
            cityField,
            birthDateField,
            hireDateField);
        var employees = new[]
        {
            CreateEmployee(1, "Alice Smith", "Director"),
            CreateEmployee(2, "Bob Jones", "Engineer")
        };
        var rootProfile = CreateProfile(1, "Alice Smith", "Director");
        var childProfile = CreateProfile(2, "Bob Jones", "Engineer", managerDisplayName: "Alice Smith");
        IReadOnlyList<EmployeeProfile> profiles = [rootProfile, childProfile];
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager = new Dictionary<EmployeeId, IReadOnlyList<EmployeeId>>
        {
            [rootEmployeeId] = [childEmployeeId]
        };
        IReadOnlyCollection<TimeOffEntry> whoIsOut =
        [
            CreateHolidayEntry(100, "Founders Day", 2026, 4, 3, 2026, 4, 3),
            CreateTimeOffEntry(200, 2, "Bob Jones", 2026, 4, 2, 2026, 4, 2)
        ];
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Founders Day"] = ["Germany"]
        };
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> employeeEntries = new Dictionary<EmployeeId, IReadOnlyList<TimeOffEntry>>
        {
            [childEmployeeId] = [CreateTimeOffEntry(200, 2, "Bob Jones", 2026, 4, 2, 2026, 4, 2)]
        };
        IReadOnlyList<HolidayReportItem> holidays =
        [
            new HolidayReportItem(
                "Founders Day",
                new DateOnly(2026, 4, 3),
                new DateOnly(2026, 4, 3),
                ["Germany"])
        ];
        var rootRow = new HierarchyReportRow(
            level: 0,
            rootEmployeeId,
            "Alice Smith",
            department: "Leadership",
            jobTitle: "Director",
            location: "Berlin, Germany",
            dateOfBirth: new DateOnly(1985, 5, 5),
            employmentStartDate: new DateOnly(2018, 1, 1),
            managerName: null,
            []);
        var childRow = new HierarchyReportRow(
            level: 1,
            childEmployeeId,
            "Bob Jones",
            department: "Engineering",
            jobTitle: "Engineer",
            location: "Berlin, Germany",
            dateOfBirth: new DateOnly(1992, 8, 14),
            employmentStartDate: new DateOnly(2024, 6, 1),
            managerName: "Alice Smith",
            [CreateTimeOffEntry(200, 2, "Bob Jones", 2026, 4, 2, 2026, 4, 2)]);
        IReadOnlyList<HierarchyReportRow> recentHires = [childRow];
        IReadOnlyList<HierarchyTeam> teams =
        [
            new HierarchyTeam(
                rootEmployeeId,
                "Alice Smith",
                ["Bob Jones"],
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Director"] = 1,
                    ["Engineer"] = 1
                })
        ];
        Dictionary<string, int> locationCounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = 2
        };
        Dictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Berlin"] = 2
            }
        };
        Dictionary<string, int> ageCounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["25-34"] = 1,
            ["35-44"] = 1
        };
        Dictionary<string, int> tenureCounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["1-2 years"] = 1,
            ["6-10 years"] = 1
        };

        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var loadingNotifier = new RecordingLoadingNotifier();
        var availabilityWindowProvider = new Mock<IAvailabilityWindowProvider>(MockBehavior.Strict);
        var employeeProfileDirectoryLoader = new Mock<IEmployeeProfileDirectoryLoader>(MockBehavior.Strict);
        var hierarchyFieldResolver = new Mock<IHierarchyFieldResolver>(MockBehavior.Strict);
        var hierarchyTopologyBuilder = new Mock<IHierarchyTopologyBuilder>(MockBehavior.Strict);
        var employeeAvailabilityResolver = new Mock<IEmployeeAvailabilityResolver>(MockBehavior.Strict);
        var hierarchyAnalytics = new Mock<IHierarchyAnalytics>(MockBehavior.Strict);
        var availabilityWindowProviderCalls = 0;
        var resolveFieldCalls = 0;
        var getEmployeesCalls = 0;
        var loadProfilesCalls = 0;
        var buildChildrenCalls = 0;
        var collectHierarchyIdsCalls = 0;
        var getWhosOutCalls = 0;
        var buildHolidayMappingsCalls = 0;
        var buildHolidayEntriesCalls = 0;
        var buildEmployeeEntriesCalls = 0;
        var flattenHierarchyCalls = 0;
        var buildRecentHiresCalls = 0;
        var buildTeamsCalls = 0;
        var buildLocationCountsCalls = 0;
        var buildCountryCityCountsCalls = 0;
        var buildAgeCountsCalls = 0;
        var buildTenureCountsCalls = 0;

        availabilityWindowProvider.Setup(provider => provider.GetAvailabilityWindow(
                It.Is<DateTimeOffset>(date => date == generatedAt)))
            .Callback(() => availabilityWindowProviderCalls++)
            .Returns(availabilityWindow);
        hierarchyFieldResolver.Setup(resolver => resolver.ResolveAsync(
                It.Is<EmployeeId>(employeeId => employeeId == rootEmployeeId),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => resolveFieldCalls++)
            .ReturnsAsync(fieldSelection);
        bambooHrClient.Setup(client => client.GetEmployeesAsync(
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getEmployeesCalls++)
            .ReturnsAsync(employees);
        employeeProfileDirectoryLoader.Setup(loader => loader.LoadAsync(
                It.Is<IReadOnlyList<BasicEmployee>>(value => ReferenceEquals(value, employees)),
                It.Is<HierarchyRelationshipField>(value => ReferenceEquals(value, relationshipField)),
                It.Is<BambooHrField?>(value => value == locationField),
                It.Is<BambooHrField?>(value => value == countryField),
                It.Is<BambooHrField?>(value => value == cityField),
                It.Is<BambooHrField?>(value => value == birthDateField),
                It.Is<BambooHrField?>(value => value == hireDateField),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => loadProfilesCalls++)
            .ReturnsAsync(profiles);
        hierarchyTopologyBuilder.Setup(builder => builder.BuildChildrenByManager(
                It.Is<IReadOnlyList<EmployeeProfile>>(value => ReferenceEquals(value, profiles)),
                It.Is<HierarchyRelationshipField>(value => ReferenceEquals(value, relationshipField))))
            .Callback(() => buildChildrenCalls++)
            .Returns(childrenByManager);
        hierarchyTopologyBuilder.Setup(builder => builder.CollectHierarchyEmployeeIds(
                It.Is<EmployeeId>(employeeId => employeeId == rootEmployeeId),
                It.Is<IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>>>(value => ReferenceEquals(value, childrenByManager)),
                It.Is<IReadOnlyDictionary<EmployeeId, EmployeeProfile>>(value =>
                    value.Count == 2
                    && ReferenceEquals(value[rootEmployeeId], rootProfile)
                    && ReferenceEquals(value[childEmployeeId], childProfile))))
            .Callback(() => collectHierarchyIdsCalls++)
            .Returns([rootEmployeeId, childEmployeeId]);
        bambooHrClient.Setup(client => client.GetWhosOutAsync(
                It.Is<DateOnly>(start => start == availabilityWindow.Start),
                It.Is<DateOnly>(end => end == availabilityWindow.End),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getWhosOutCalls++)
            .ReturnsAsync([.. whoIsOut]);
        employeeAvailabilityResolver.Setup(resolver => resolver.BuildHolidayCountryMappings(
                It.Is<IReadOnlyDictionary<string, string[]>>(value =>
                    value.Count == 1
                    && value["Founders Day"].SequenceEqual(new[] { "Germany" }))))
            .Callback(() => buildHolidayMappingsCalls++)
            .Returns(holidayCountryMappings);
        employeeAvailabilityResolver.Setup(resolver => resolver.BuildHolidayEntries(
                It.Is<IReadOnlyList<TimeOffEntry>>(value => MatchTimeOffEntries(value, whoIsOut)),
                It.Is<Dictionary<string, IReadOnlyList<string>>>(value => ReferenceEquals(value, holidayCountryMappings))))
            .Callback(() => buildHolidayEntriesCalls++)
            .Returns(holidays);
        employeeAvailabilityResolver.Setup(resolver => resolver.BuildEmployeeEntries(
                It.Is<IReadOnlyCollection<EmployeeProfile>>(value => MatchProfiles(value, rootProfile, childProfile)),
                It.Is<IReadOnlyList<TimeOffEntry>>(value => MatchTimeOffEntries(value, whoIsOut)),
                It.Is<Dictionary<string, IReadOnlyList<string>>>(value => ReferenceEquals(value, holidayCountryMappings))))
            .Callback(() => buildEmployeeEntriesCalls++)
            .Returns(employeeEntries);
        hierarchyTopologyBuilder.Setup(builder => builder.FlattenHierarchy(
                It.Is<EmployeeId>(employeeId => employeeId == rootEmployeeId),
                It.Is<int>(level => level == 0),
                It.Is<IReadOnlyDictionary<EmployeeId, EmployeeProfile>>(value =>
                    value.Count == 2
                    && ReferenceEquals(value[rootEmployeeId], rootProfile)
                    && ReferenceEquals(value[childEmployeeId], childProfile)),
                It.Is<IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>>>(value => ReferenceEquals(value, childrenByManager)),
                It.Is<IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>>>(value => ReferenceEquals(value, employeeEntries)),
                It.Is<ICollection<HierarchyReportRow>>(rows => rows.Count == 0)))
            .Callback<EmployeeId, int, IReadOnlyDictionary<EmployeeId, EmployeeProfile>, IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>>, IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>>, ICollection<HierarchyReportRow>>((_, _, _, _, _, rows) =>
            {
                flattenHierarchyCalls++;
                rows.Add(rootRow);
                rows.Add(childRow);
            });
        hierarchyAnalytics.Setup(analytics => analytics.BuildRecentHires(
                It.Is<IReadOnlyList<HierarchyReportRow>>(value => value.Count == 2 && value[0] == rootRow && value[1] == childRow),
                It.Is<DateOnly>(date => date == new DateOnly(2026, 4, 1)),
                It.Is<int>(days => days == 30)))
            .Callback(() => buildRecentHiresCalls++)
            .Returns(recentHires);
        hierarchyAnalytics.Setup(analytics => analytics.BuildTeams(
                It.Is<IReadOnlyList<HierarchyReportRow>>(value => value.Count == 2 && value[0] == rootRow && value[1] == childRow),
                It.Is<IReadOnlyDictionary<EmployeeId, EmployeeProfile>>(value =>
                    value.Count == 2
                    && ReferenceEquals(value[rootEmployeeId], rootProfile)
                    && ReferenceEquals(value[childEmployeeId], childProfile)),
                It.Is<IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>>>(value => ReferenceEquals(value, childrenByManager))))
            .Callback(() => buildTeamsCalls++)
            .Returns(teams);
        hierarchyAnalytics.Setup(analytics => analytics.BuildLocationCounts(
                It.Is<IEnumerable<EmployeeProfile>>(value => MatchProfiles(value, rootProfile, childProfile))))
            .Callback(() => buildLocationCountsCalls++)
            .Returns(locationCounts);
        hierarchyAnalytics.Setup(analytics => analytics.BuildCountryCityCounts(
                It.Is<IEnumerable<EmployeeProfile>>(value => MatchProfiles(value, rootProfile, childProfile))))
            .Callback(() => buildCountryCityCountsCalls++)
            .Returns(countryCityCounts);
        hierarchyAnalytics.Setup(analytics => analytics.BuildAgeCounts(
                It.Is<IEnumerable<EmployeeProfile>>(value => MatchProfiles(value, rootProfile, childProfile)),
                It.Is<DateOnly>(date => date == availabilityWindow.Start)))
            .Callback(() => buildAgeCountsCalls++)
            .Returns(ageCounts);
        hierarchyAnalytics.Setup(analytics => analytics.BuildTenureCounts(
                It.Is<IEnumerable<EmployeeProfile>>(value => MatchProfiles(value, rootProfile, childProfile)),
                It.Is<DateOnly>(date => date == availabilityWindow.Start)))
            .Callback(() => buildTenureCountsCalls++)
            .Returns(tenureCounts);

        var builder = CreateBuilder(
            bambooHrClient.Object,
            loadingNotifier,
            availabilityWindowProvider.Object,
            employeeProfileDirectoryLoader.Object,
            hierarchyFieldResolver.Object,
            hierarchyTopologyBuilder.Object,
            employeeAvailabilityResolver.Object,
            hierarchyAnalytics.Object,
            CreateSettings());

        // Act
        var report = await builder.BuildAsync(rootEmployeeId, cts.Token);

        // Assert
        report.GeneratedAt.Should().Be(generatedAt);
        report.AvailabilityWindow.Should().BeEquivalentTo(availabilityWindow);
        report.RootEmployeeName.Should().Be("Alice Smith");
        report.RelationshipField.Should().BeSameAs(relationshipField);
        report.Holidays.Should().BeSameAs(holidays);
        report.Rows.Should().Equal(rootRow, childRow);
        report.RecentHires.Should().Equal(childRow);
        report.Teams.Should().Equal(teams);
        report.LocationCounts.Should().BeSameAs(locationCounts);
        report.CountryCityCounts.Should().BeSameAs(countryCityCounts);
        report.AgeCounts.Should().BeSameAs(ageCounts);
        report.TenureCounts.Should().BeSameAs(tenureCounts);
        loadingNotifier.Statuses.Should().Equal(
            "Loading availability window and BambooHR field selection...",
            "Loading employee directory...",
            "Loading employee profiles (0/2)...",
            "Loading who's out for the availability window...",
            "Building hierarchy report...",
            "Calculating distributions and summaries...");
        availabilityWindowProviderCalls.Should().Be(1);
        resolveFieldCalls.Should().Be(1);
        getEmployeesCalls.Should().Be(1);
        loadProfilesCalls.Should().Be(1);
        buildChildrenCalls.Should().Be(1);
        collectHierarchyIdsCalls.Should().Be(1);
        getWhosOutCalls.Should().Be(1);
        buildHolidayMappingsCalls.Should().Be(1);
        buildHolidayEntriesCalls.Should().Be(1);
        buildEmployeeEntriesCalls.Should().Be(1);
        flattenHierarchyCalls.Should().Be(1);
        buildRecentHiresCalls.Should().Be(1);
        buildTeamsCalls.Should().Be(1);
        buildLocationCountsCalls.Should().Be(1);
        buildCountryCityCountsCalls.Should().Be(1);
        buildAgeCountsCalls.Should().Be(1);
        buildTenureCountsCalls.Should().Be(1);
    }

    [Fact(DisplayName = "The builder throws when the requested root employee is not present among the loaded profiles.")]
    [Trait("Category", "Unit")]
    public async Task BuildAsyncShouldThrowWhenRootEmployeeProfileIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var generatedAt = new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero);
        var rootEmployeeId = new EmployeeId(1);
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 8));
        var relationshipField = new HierarchyRelationshipField("reportsTo", "Reports To", usesEmployeeId: false);
        var fieldSelection = new HierarchyFieldSelection(relationshipField, null, null, null, null, null);
        var employees = new[] { CreateEmployee(2, "Bob Jones", "Engineer") };
        IReadOnlyList<EmployeeProfile> profiles = [CreateProfile(2, "Bob Jones", "Engineer", managerDisplayName: "Alice Smith")];

        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var loadingNotifier = new RecordingLoadingNotifier();
        var availabilityWindowProvider = new Mock<IAvailabilityWindowProvider>(MockBehavior.Strict);
        var employeeProfileDirectoryLoader = new Mock<IEmployeeProfileDirectoryLoader>(MockBehavior.Strict);
        var hierarchyFieldResolver = new Mock<IHierarchyFieldResolver>(MockBehavior.Strict);
        var availabilityWindowProviderCalls = 0;
        var resolveFieldCalls = 0;
        var getEmployeesCalls = 0;
        var loadProfilesCalls = 0;

        availabilityWindowProvider.Setup(provider => provider.GetAvailabilityWindow(
                It.Is<DateTimeOffset>(date => date == generatedAt)))
            .Callback(() => availabilityWindowProviderCalls++)
            .Returns(availabilityWindow);
        hierarchyFieldResolver.Setup(resolver => resolver.ResolveAsync(
                It.Is<EmployeeId>(employeeId => employeeId == rootEmployeeId),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => resolveFieldCalls++)
            .ReturnsAsync(fieldSelection);
        bambooHrClient.Setup(client => client.GetEmployeesAsync(
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getEmployeesCalls++)
            .ReturnsAsync(employees);
        employeeProfileDirectoryLoader.Setup(loader => loader.LoadAsync(
                It.Is<IReadOnlyList<BasicEmployee>>(value => ReferenceEquals(value, employees)),
                It.Is<HierarchyRelationshipField>(value => ReferenceEquals(value, relationshipField)),
                It.Is<BambooHrField?>(value => value == null),
                It.Is<BambooHrField?>(value => value == null),
                It.Is<BambooHrField?>(value => value == null),
                It.Is<BambooHrField?>(value => value == null),
                It.Is<BambooHrField?>(value => value == null),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => loadProfilesCalls++)
            .ReturnsAsync(profiles);

        var builder = CreateBuilder(
            bambooHrClient.Object,
            loadingNotifier,
            availabilityWindowProvider.Object,
            employeeProfileDirectoryLoader.Object,
            hierarchyFieldResolver.Object,
            new Mock<IHierarchyTopologyBuilder>(MockBehavior.Strict).Object,
            new Mock<IEmployeeAvailabilityResolver>(MockBehavior.Strict).Object,
            new Mock<IHierarchyAnalytics>(MockBehavior.Strict).Object,
            CreateSettings());

        // Act
        Func<Task> action = () => builder.BuildAsync(rootEmployeeId, cts.Token);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Employee '1' was not found among active BambooHR employees.");
        loadingNotifier.Statuses.Should().Equal(
            "Loading availability window and BambooHR field selection...",
            "Loading employee directory...",
            "Loading employee profiles (0/1)...");
        availabilityWindowProviderCalls.Should().Be(1);
        resolveFieldCalls.Should().Be(1);
        getEmployeesCalls.Should().Be(1);
        loadProfilesCalls.Should().Be(1);
    }

    private static HierarchyReportBuilder CreateBuilder(
        IBambooHrClient bambooHrClient,
        ILoadingNotifier loadingNotifier,
        IAvailabilityWindowProvider availabilityWindowProvider,
        IEmployeeProfileDirectoryLoader employeeProfileDirectoryLoader,
        IHierarchyFieldResolver hierarchyFieldResolver,
        IHierarchyTopologyBuilder hierarchyTopologyBuilder,
        IEmployeeAvailabilityResolver employeeAvailabilityResolver,
        IHierarchyAnalytics hierarchyAnalytics,
        HierarchyReportSettings? settings = null,
        TimeProvider? timeProvider = null)
        => new(
            bambooHrClient,
            settings ?? CreateSettings(),
            loadingNotifier,
            availabilityWindowProvider,
            employeeProfileDirectoryLoader,
            hierarchyFieldResolver,
            hierarchyTopologyBuilder,
            employeeAvailabilityResolver,
            hierarchyAnalytics,
            timeProvider ?? new FixedTimeProvider(new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero)));

    private static HierarchyReportSettings CreateSettings()
        => new(
            new EmployeeId(1),
            availabilityLookaheadDays: 7,
            recentHirePeriodDays: 30,
            new Dictionary<string, string[]>
            {
                ["Founders Day"] = ["Germany"]
            });

    private static bool MatchProfiles(IEnumerable<EmployeeProfile> profiles, params EmployeeProfile[] expectedProfiles)
        => profiles.Select(profile => profile.EmployeeId)
            .OrderBy(employeeId => employeeId)
            .SequenceEqual(
                expectedProfiles.Select(profile => profile.EmployeeId).OrderBy(employeeId => employeeId));

    private static bool MatchTimeOffEntries(IEnumerable<TimeOffEntry> entries, IEnumerable<TimeOffEntry> expectedEntries)
        => entries.Select(entry => entry.Id)
            .OrderBy(id => id)
            .SequenceEqual(expectedEntries.Select(entry => entry.Id).OrderBy(id => id));

    private static BasicEmployee CreateEmployee(int employeeId, string displayName, string? jobTitle)
    {
        var names = displayName.Split(' ');

        return new BasicEmployee(
            new EmployeeId(employeeId),
            displayName,
            names[0],
            names[^1],
            names[0],
            jobTitle,
            status: "Active");
    }

    private static EmployeeProfile CreateProfile(
        int employeeId,
        string displayName,
        string jobTitle,
        string? managerDisplayName = null)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(employeeId),
            displayName,
            names[0],
            names[^1],
            names[0],
            department: employeeId == 1 ? "Leadership" : "Engineering",
            jobTitle,
            location: "Berlin, Germany",
            country: "Germany",
            city: "Berlin",
            dateOfBirth: employeeId == 1 ? new DateOnly(1985, 5, 5) : new DateOnly(1992, 8, 14),
            hireDate: employeeId == 1 ? new DateOnly(2018, 1, 1) : new DateOnly(2024, 6, 1),
            workEmail: $"employee{employeeId}@example.com",
            manager: ManagerReference.Parse(managerDisplayName));
    }

    private static BambooHrField CreateField(string requestKey, string displayName)
        => new(requestKey, displayName, requestKey, "text");

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

    private sealed class FixedTimeProvider(DateTimeOffset currentDate) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => currentDate.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class RecordingLoadingNotifier : ILoadingNotifier
    {
        public List<string> Statuses { get; } = [];

        public Task<T> RunAsync<T>(
            string description,
            Func<CancellationToken, Task<T>> action,
            CancellationToken ct)
            => action(ct);

        public void SetStatus(string description) => Statuses.Add(description);

        public void SetProgress(string description, int completed, int total)
        {
            _ = description;
            _ = completed;
            _ = total;
        }
    }
}
