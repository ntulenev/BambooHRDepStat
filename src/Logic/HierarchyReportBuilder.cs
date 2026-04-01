using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Builds a hierarchy report for one employee and descendants.
/// </summary>
public sealed class HierarchyReportBuilder : IHierarchyReportBuilder
{
    private static readonly (string RequestKey, string DisplayName)[] PreferredLocationFields =
    [
        ("location", "Location"),
        ("office", "Office"),
        ("workLocation", "Work Location")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredCountryFields =
    [
        ("country", "Country"),
        ("countryName", "Country"),
        ("workCountry", "Work Country")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredCityFields =
    [
        ("city", "City"),
        ("workCity", "Work City"),
        ("officeCity", "Office City")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredBirthDateFields =
    [
        ("dateOfBirth", "Date of Birth"),
        ("birthDate", "Birth Date"),
        ("birthday", "Birthday"),
        ("dob", "Date of Birth")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredHireDateFields =
    [
        ("hireDate", "Hire Date"),
        ("startDate", "Start Date"),
        ("dateHired", "Date Hired"),
        ("employmentStartDate", "Employment Start Date")
    ];

    private static readonly HierarchyRelationshipField[] PreferredManagerIdFields =
    [
        new("supervisorEId", "Supervisor EId", usesEmployeeId: true),
        new("supervisorId", "Supervisor Id", usesEmployeeId: true),
        new("managerId", "Manager Id", usesEmployeeId: true),
        new("managerEId", "Manager EId", usesEmployeeId: true),
        new("reportsToId", "Reports To Id", usesEmployeeId: true),
        new("reportsToEId", "Reports To EId", usesEmployeeId: true)
    ];

    private static readonly HierarchyRelationshipField[] PreferredManagerNameFields =
    [
        new("reportsTo", "Reporting to", usesEmployeeId: false),
        new("supervisor", "Supervisor", usesEmployeeId: false),
        new("manager", "Manager", usesEmployeeId: false)
    ];

    private readonly IBambooHrClient _bambooHrClient;
    private readonly BambooHrOptions _options;
    private readonly ILoadingNotifier _loadingNotifier;
    private readonly IAvailabilityWindowProvider _availabilityWindowProvider;
    private readonly IEmployeeProfileDirectoryLoader _employeeProfileDirectoryLoader;
    private readonly IHierarchyTopologyBuilder _hierarchyTopologyBuilder;
    private readonly IEmployeeAvailabilityResolver _employeeAvailabilityResolver;
    private readonly IHierarchyAnalytics _hierarchyAnalytics;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates report builder.
    /// </summary>
    public HierarchyReportBuilder(
        IBambooHrClient bambooHrClient,
        BambooHrOptions options,
        ILoadingNotifier loadingNotifier,
        IAvailabilityWindowProvider availabilityWindowProvider,
        IEmployeeProfileDirectoryLoader employeeProfileDirectoryLoader,
        IHierarchyTopologyBuilder hierarchyTopologyBuilder,
        IEmployeeAvailabilityResolver employeeAvailabilityResolver,
        IHierarchyAnalytics hierarchyAnalytics,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loadingNotifier);
        ArgumentNullException.ThrowIfNull(availabilityWindowProvider);
        ArgumentNullException.ThrowIfNull(employeeProfileDirectoryLoader);
        ArgumentNullException.ThrowIfNull(hierarchyTopologyBuilder);
        ArgumentNullException.ThrowIfNull(employeeAvailabilityResolver);
        ArgumentNullException.ThrowIfNull(hierarchyAnalytics);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bambooHrClient = bambooHrClient;
        _options = options;
        _loadingNotifier = loadingNotifier;
        _availabilityWindowProvider = availabilityWindowProvider;
        _employeeProfileDirectoryLoader = employeeProfileDirectoryLoader;
        _hierarchyTopologyBuilder = hierarchyTopologyBuilder;
        _employeeAvailabilityResolver = employeeAvailabilityResolver;
        _hierarchyAnalytics = hierarchyAnalytics;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<HierarchyReport> BuildAsync(EmployeeId rootEmployeeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var generatedAt = _timeProvider.GetLocalNow();
        _loadingNotifier.SetStatus("Loading availability window and BambooHR field metadata...");
        var availabilityWindow = _availabilityWindowProvider.GetAvailabilityWindow(generatedAt);
        var fields = await _bambooHrClient.GetFieldsAsync(ct).ConfigureAwait(false);
        _loadingNotifier.SetStatus("Resolving hierarchy relationship field...");
        var relationshipField = await ResolveRelationshipFieldAsync(rootEmployeeId, ct)
            .ConfigureAwait(false);
        var locationField = FindField(fields, PreferredLocationFields);
        var countryField = FindField(fields, PreferredCountryFields);
        var cityField = FindField(fields, PreferredCityFields);
        var birthDateField = FindField(fields, PreferredBirthDateFields);
        var hireDateField = FindField(fields, PreferredHireDateFields);

        _loadingNotifier.SetStatus("Loading employee directory...");
        var employees = await _bambooHrClient.GetEmployeesAsync(ct).ConfigureAwait(false);
        _loadingNotifier.SetStatus($"Loading employee profiles (0/{employees.Count})...");
        var profiles = await _employeeProfileDirectoryLoader.LoadAsync(
                employees,
                relationshipField,
                locationField,
                countryField,
                cityField,
                birthDateField,
                hireDateField,
                ct)
            .ConfigureAwait(false);
        var profilesByEmployeeId = profiles.ToDictionary(
            profile => profile.EmployeeId,
            profile => profile);

        if (!profilesByEmployeeId.TryGetValue(rootEmployeeId, out var rootEmployee))
        {
            throw new InvalidOperationException(
                $"Employee '{rootEmployeeId}' was not found among active BambooHR employees.");
        }

        var childrenByManager = _hierarchyTopologyBuilder.BuildChildrenByManager(
            profiles,
            relationshipField);
        var includedEmployeeIds = _hierarchyTopologyBuilder.CollectHierarchyEmployeeIds(
            rootEmployeeId,
            childrenByManager,
            profilesByEmployeeId);
        var includedProfiles = includedEmployeeIds
            .Select(id => profilesByEmployeeId[id])
            .ToArray();

        _loadingNotifier.SetStatus("Loading who's out for the availability window...");
        var whoIsOut = await _bambooHrClient.GetWhosOutAsync(
                availabilityWindow.Start,
                availabilityWindow.End,
                ct)
            .ConfigureAwait(false);

        var holidayCountryMappings = _employeeAvailabilityResolver.BuildHolidayCountryMappings(
            _options.HolidayCountryMappings);
        var holidays = _employeeAvailabilityResolver.BuildHolidayEntries(
            whoIsOut,
            holidayCountryMappings);
        var employeeEntries = _employeeAvailabilityResolver.BuildEmployeeEntries(
            includedProfiles,
            whoIsOut,
            holidayCountryMappings);

        List<HierarchyReportRow> rows = [];
        _loadingNotifier.SetStatus("Building hierarchy report...");
        _hierarchyTopologyBuilder.FlattenHierarchy(
            rootEmployeeId,
            level: 0,
            profilesByEmployeeId,
            childrenByManager,
            employeeEntries,
            rows);

        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);
        var recentHires = _hierarchyAnalytics.BuildRecentHires(
            rows,
            referenceDate,
            _options.RecentHirePeriodDays);
        var teams = _hierarchyAnalytics.BuildTeams(
            rows,
            profilesByEmployeeId,
            childrenByManager);

        _loadingNotifier.SetStatus("Calculating distributions and summaries...");
        var locationCounts = _hierarchyAnalytics.BuildLocationCounts(includedProfiles);
        var countryCityCounts = _hierarchyAnalytics.BuildCountryCityCounts(includedProfiles);
        var ageCounts = _hierarchyAnalytics.BuildAgeCounts(
            includedProfiles,
            availabilityWindow.Start);
        var tenureCounts = _hierarchyAnalytics.BuildTenureCounts(
            includedProfiles,
            availabilityWindow.Start);

        return new HierarchyReport(
            generatedAt,
            availabilityWindow,
            rootEmployee.DisplayName,
            relationshipField,
            holidays,
            rows,
            recentHires,
            _options.RecentHirePeriodDays,
            teams,
            locationCounts,
            countryCityCounts,
            ageCounts,
            tenureCounts);
    }

    private async Task<HierarchyRelationshipField> ResolveRelationshipFieldAsync(
        EmployeeId rootEmployeeId,
        CancellationToken ct)
    {
        var probeCandidates = PreferredManagerIdFields
            .Concat(PreferredManagerNameFields)
            .ToArray();
        var probeValues = await _bambooHrClient.GetEmployeeFieldsAsync(
                rootEmployeeId,
                [.. probeCandidates.Select(candidate => candidate.RequestKey)],
                ct)
            .ConfigureAwait(false);

        foreach (var candidate in probeCandidates)
        {
            if (probeValues.Values.ContainsKey(candidate.RequestKey))
            {
                return candidate;
            }
        }

        var fields = await _bambooHrClient.GetFieldsAsync(ct).ConfigureAwait(false);
        var managerIdField = FindField(fields, PreferredManagerIdFields);
        if (managerIdField is not null)
        {
            return managerIdField;
        }

        var managerNameField = FindField(fields, PreferredManagerNameFields);
        if (managerNameField is not null)
        {
            return managerNameField;
        }

        throw new InvalidOperationException(
            "No BambooHR manager relationship field could be resolved.");
    }

    private static HierarchyRelationshipField? FindField(
        IEnumerable<BambooHrField> fields,
        IReadOnlyCollection<HierarchyRelationshipField> candidates)
    {
        var fieldList = fields.ToArray();

        foreach (var candidate in candidates)
        {
            var exact = fieldList.FirstOrDefault(field =>
                string.Equals(
                    Normalize(field.RequestKey),
                    Normalize(candidate.RequestKey),
                    StringComparison.Ordinal));
            if (exact is not null)
            {
                return new HierarchyRelationshipField(
                    exact.RequestKey,
                    exact.Name,
                    candidate.UsesEmployeeId);
            }
        }

        foreach (var candidate in candidates)
        {
            var partial = fieldList.FirstOrDefault(field =>
                Normalize(field.RequestKey).Contains(
                    Normalize(candidate.RequestKey),
                    StringComparison.Ordinal)
                || Normalize(field.Name).Contains(
                    Normalize(candidate.DisplayName),
                    StringComparison.Ordinal));
            if (partial is not null)
            {
                return new HierarchyRelationshipField(
                    partial.RequestKey,
                    partial.Name,
                    candidate.UsesEmployeeId);
            }
        }

        return null;
    }

    private static BambooHrField? FindField(
        IEnumerable<BambooHrField> fields,
        (string RequestKey, string DisplayName)[] candidates)
    {
        var fieldList = fields.ToArray();

        foreach (var (requestKey, _) in candidates)
        {
            var exact = fieldList.FirstOrDefault(field =>
                string.Equals(
                    Normalize(field.RequestKey),
                    Normalize(requestKey),
                    StringComparison.Ordinal));
            if (exact is not null)
            {
                return exact;
            }
        }

        foreach (var (requestKey, displayName) in candidates)
        {
            var partial = fieldList.FirstOrDefault(field =>
                Normalize(field.RequestKey).Contains(
                    Normalize(requestKey),
                    StringComparison.Ordinal)
                || Normalize(field.Name).Contains(
                    Normalize(displayName),
                    StringComparison.Ordinal));
            if (partial is not null)
            {
                return partial;
            }
        }

        return null;
    }

    private static string Normalize(string value)
    {
        var buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(buffer);
    }
}
