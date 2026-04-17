using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Builds a hierarchy report for one employee and descendants.
/// </summary>
public sealed class HierarchyReportBuilder : IHierarchyReportBuilder
{
    /// <summary>
    /// Creates report builder.
    /// </summary>
    public HierarchyReportBuilder(
        IBambooHrClient bambooHrClient,
        HierarchyReportSettings settings,
        ILoadingNotifier loadingNotifier,
        IAvailabilityWindowProvider availabilityWindowProvider,
        IEmployeeProfileDirectoryLoader employeeProfileDirectoryLoader,
        IHierarchyFieldResolver hierarchyFieldResolver,
        IHierarchyTopologyBuilder hierarchyTopologyBuilder,
        IEmployeeAvailabilityResolver employeeAvailabilityResolver,
        IHierarchyAnalytics hierarchyAnalytics,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(loadingNotifier);
        ArgumentNullException.ThrowIfNull(availabilityWindowProvider);
        ArgumentNullException.ThrowIfNull(employeeProfileDirectoryLoader);
        ArgumentNullException.ThrowIfNull(hierarchyFieldResolver);
        ArgumentNullException.ThrowIfNull(hierarchyTopologyBuilder);
        ArgumentNullException.ThrowIfNull(employeeAvailabilityResolver);
        ArgumentNullException.ThrowIfNull(hierarchyAnalytics);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bambooHrClient = bambooHrClient;
        _settings = settings;
        _loadingNotifier = loadingNotifier;
        _availabilityWindowProvider = availabilityWindowProvider;
        _employeeProfileDirectoryLoader = employeeProfileDirectoryLoader;
        _hierarchyFieldResolver = hierarchyFieldResolver;
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
        _loadingNotifier.SetStatus("Loading availability window and BambooHR field selection...");
        var availabilityWindow = _availabilityWindowProvider.GetAvailabilityWindow(generatedAt);
        var fieldSelection = await _hierarchyFieldResolver.ResolveAsync(rootEmployeeId, ct)
            .ConfigureAwait(false);

        _loadingNotifier.SetStatus("Loading employee directory...");
        var employees = await _bambooHrClient.GetEmployeesAsync(ct).ConfigureAwait(false);
        _loadingNotifier.SetStatus($"Loading employee profiles (0/{employees.Count})...");
        var profiles = await _employeeProfileDirectoryLoader.LoadAsync(
                employees,
                fieldSelection.RelationshipField,
                fieldSelection.LocationField,
                fieldSelection.CountryField,
                fieldSelection.CityField,
                fieldSelection.BirthDateField,
                fieldSelection.HireDateField,
                fieldSelection.TeamField,
                fieldSelection.PhoneFields,
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
            fieldSelection.RelationshipField);
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
            _settings.HolidayCountryMappings);
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
            _settings.RecentHirePeriodDays);
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
            new HierarchyReportOverview(
                generatedAt,
                availabilityWindow,
                rootEmployee.DisplayName,
                fieldSelection.RelationshipField),
            new HierarchyReportHierarchy(
                holidays,
                rows),
            new HierarchyReportSummaries(
                recentHires,
                _settings.RecentHirePeriodDays,
                teams,
                _settings.ShowTeamReports),
            new HierarchyReportDistributions(
                locationCounts,
                countryCityCounts,
                ageCounts,
                tenureCounts));
    }

    private readonly IBambooHrClient _bambooHrClient;
    private readonly HierarchyReportSettings _settings;
    private readonly ILoadingNotifier _loadingNotifier;
    private readonly IAvailabilityWindowProvider _availabilityWindowProvider;
    private readonly IEmployeeProfileDirectoryLoader _employeeProfileDirectoryLoader;
    private readonly IHierarchyFieldResolver _hierarchyFieldResolver;
    private readonly IHierarchyTopologyBuilder _hierarchyTopologyBuilder;
    private readonly IEmployeeAvailabilityResolver _employeeAvailabilityResolver;
    private readonly IHierarchyAnalytics _hierarchyAnalytics;
    private readonly TimeProvider _timeProvider;
}
