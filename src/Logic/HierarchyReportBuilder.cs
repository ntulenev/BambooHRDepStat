using System.Collections.Concurrent;

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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates report builder.
    /// </summary>
    public HierarchyReportBuilder(
        IBambooHrClient bambooHrClient,
        BambooHrOptions options,
        ILoadingNotifier loadingNotifier,
        IAvailabilityWindowProvider availabilityWindowProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loadingNotifier);
        ArgumentNullException.ThrowIfNull(availabilityWindowProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bambooHrClient = bambooHrClient;
        _options = options;
        _loadingNotifier = loadingNotifier;
        _availabilityWindowProvider = availabilityWindowProvider;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<HierarchyReport> BuildAsync(int rootEmployeeId, CancellationToken ct)
    {
        if (rootEmployeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rootEmployeeId),
                "Employee ID must be greater than zero.");
        }

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
        var profiles = await LoadProfilesAsync(
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

        var childrenByManager = relationshipField.UsesEmployeeId
            ? BuildEmployeeIdHierarchy(profiles)
            : BuildNameHierarchy(profiles);
        var includedEmployeeIds = CollectHierarchyEmployeeIds(
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

        var employeeEntries = BuildEmployeeEntries(
            includedProfiles,
            whoIsOut);

        List<HierarchyReportRow> rows = [];
        _loadingNotifier.SetStatus("Building hierarchy report...");
        FlattenHierarchy(
            rootEmployeeId,
            level: 0,
            profilesByEmployeeId,
            childrenByManager,
            employeeEntries,
            rows);
        var recentHires = BuildRecentHires(
            rows,
            DateOnly.FromDateTime(generatedAt.Date),
            _options.RecentHirePeriodDays);
        var teams = BuildTeams(rows, profilesByEmployeeId, childrenByManager);
        _loadingNotifier.SetStatus("Calculating distributions and summaries...");
        var locationCounts = BuildLocationCounts(includedProfiles);
        var countryCityCounts = BuildCountryCityCounts(includedProfiles);
        var ageCounts = BuildAgeCounts(includedProfiles, availabilityWindow.Start);
        var tenureCounts = BuildTenureCounts(includedProfiles, availabilityWindow.Start);

        return new HierarchyReport(
            generatedAt,
            availabilityWindow,
            rootEmployee.DisplayName,
            relationshipField,
            rows,
            recentHires,
            _options.RecentHirePeriodDays,
            teams,
            locationCounts,
            countryCityCounts,
            ageCounts,
            tenureCounts);
    }

    private static Dictionary<int, IReadOnlyList<TimeOffEntry>> BuildEmployeeEntries(
        EmployeeProfile[] includedProfiles,
        IReadOnlyList<TimeOffEntry> whoIsOut)
    {
        ArgumentNullException.ThrowIfNull(includedProfiles);
        ArgumentNullException.ThrowIfNull(whoIsOut);

        var timeOffEntries = whoIsOut
            .Where(entry => entry.Type == TimeOffEntryType.TimeOff && entry.EmployeeId.HasValue)
            .GroupBy(entry => entry.EmployeeId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(entry => entry.Start)
                    .ThenBy(entry => entry.End)
                    .ThenBy(entry => entry.Id)
                    .ToArray());
        var holidayEntries = whoIsOut
            .Where(entry => entry.Type == TimeOffEntryType.Holiday)
            .OrderBy(entry => entry.Start)
            .ThenBy(entry => entry.End)
            .ThenBy(entry => entry.Id)
            .ToArray();
        var distinctCountryCount = includedProfiles
            .Select(ResolveCountryLabel)
            .Where(country => !string.Equals(country, "Unknown", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var entriesByEmployee = new Dictionary<int, IReadOnlyList<TimeOffEntry>>();

        foreach (var profile in includedProfiles)
        {
            List<TimeOffEntry> employeeEntries = [];

            if (timeOffEntries.TryGetValue(profile.EmployeeId, out var personalEntries))
            {
                employeeEntries.AddRange(personalEntries);
            }

            employeeEntries.AddRange(
                holidayEntries.Where(holiday => IsHolidayApplicable(
                    profile,
                    holiday,
                    distinctCountryCount)));

            entriesByEmployee[profile.EmployeeId] =
            [
                .. employeeEntries
                    .OrderBy(entry => entry.Start)
                    .ThenBy(entry => entry.End)
                    .ThenBy(entry => entry.Id)
            ];
        }

        return entriesByEmployee;
    }

    private static List<HierarchyTeam> BuildTeams(
        IReadOnlyList<HierarchyReportRow> rows,
        Dictionary<int, EmployeeProfile> profilesByEmployeeId,
        Dictionary<int, List<int>> childrenByManager)
    {
        var includedEmployeeIds = rows
            .Select(row => row.EmployeeId)
            .ToHashSet();
        List<HierarchyTeam> teams = [];

        foreach (var row in rows)
        {
            if (!childrenByManager.TryGetValue(row.EmployeeId, out var directReports))
            {
                continue;
            }

            var leafDirectReports = directReports
                .Distinct()
                .Where(id => includedEmployeeIds.Contains(id))
                .Where(id => !childrenByManager.ContainsKey(id))
                .OrderBy(
                    id => profilesByEmployeeId[id].DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(id => id)
                .Select(id => profilesByEmployeeId[id].DisplayName)
                .ToArray();

            if (leafDirectReports.Length == 0)
            {
                continue;
            }

            teams.Add(new HierarchyTeam(
                row.EmployeeId,
                row.DisplayName,
                leafDirectReports,
                BuildGradeCounts(
                    row.EmployeeId,
                    directReports,
                    includedEmployeeIds,
                    childrenByManager,
                    profilesByEmployeeId)));
        }

        return teams;
    }

    private static IReadOnlyList<HierarchyReportRow> BuildRecentHires(
        IReadOnlyList<HierarchyReportRow> rows,
        DateOnly referenceDate,
        int recentHirePeriodDays)
    {
        var startDate = referenceDate.AddDays(-(recentHirePeriodDays - 1));

        return
        [
            .. rows
                .Where(row => row.EmploymentStartDate.HasValue)
                .Select(row => new
                {
                    Row = row,
                    EmploymentStartDate = row.EmploymentStartDate!.Value
                })
                .Where(item => item.EmploymentStartDate >= startDate)
                .Where(item => item.EmploymentStartDate <= referenceDate)
                .OrderByDescending(item => item.EmploymentStartDate)
                .ThenBy(item => item.Row.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Row.EmployeeId)
                .Select(item => item.Row)
        ];
    }

    private static Dictionary<string, int> BuildGradeCounts(
        int managerEmployeeId,
        IEnumerable<int> directReports,
        HashSet<int> includedEmployeeIds,
        Dictionary<int, List<int>> childrenByManager,
        Dictionary<int, EmployeeProfile> profilesByEmployeeId)
    {
        var peopleIds = directReports
            .Distinct()
            .Where(id => includedEmployeeIds.Contains(id))
            .Where(id => !childrenByManager.ContainsKey(id))
            .Append(managerEmployeeId);

        return peopleIds
            .Select(id => ResolveGradeLabel(profilesByEmployeeId[id].JobTitle))
            .GroupBy(grade => grade)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildLocationCounts(
        EmployeeProfile[] profiles)
    {
        return profiles
            .Select(ResolveCountryLabel)
            .GroupBy(location => location, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.First(),
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, IReadOnlyDictionary<string, int>> BuildCountryCityCounts(
        EmployeeProfile[] profiles)
    {
        return profiles
            .Select(profile => (Country: ResolveCountryLabel(profile), City: ResolveCityLabel(profile)))
            .Where(item => !string.IsNullOrWhiteSpace(item.City))
            .GroupBy(item => item.Country, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, int>)group
                    .GroupBy(item => item.City!, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(cityGroup => cityGroup.Count())
                    .ThenBy(cityGroup => cityGroup.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        cityGroup => cityGroup.First().City!,
                        cityGroup => cityGroup.Count(),
                        StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildAgeCounts(
        EmployeeProfile[] profiles,
        DateOnly referenceDate)
    {
        return profiles
            .Select(profile => ClassifyAge(profile.DateOfBirth, referenceDate))
            .GroupBy(bucket => bucket, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetAgeBucketOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildTenureCounts(
        EmployeeProfile[] profiles,
        DateOnly referenceDate)
    {
        return profiles
            .Select(profile => ClassifyTenure(profile.HireDate, referenceDate))
            .GroupBy(bucket => bucket, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetTenureBucketOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<int, List<int>> BuildEmployeeIdHierarchy(
        IEnumerable<EmployeeProfile> profiles)
    {
        var childrenByManager = new Dictionary<int, List<int>>();

        foreach (var profile in profiles)
        {
            if (!int.TryParse(profile.ManagerLookupValue, out var managerId)
                || managerId <= 0)
            {
                continue;
            }

            if (!childrenByManager.TryGetValue(managerId, out var children))
            {
                children = [];
                childrenByManager[managerId] = children;
            }

            children.Add(profile.EmployeeId);
        }

        return childrenByManager;
    }

    private static HashSet<int> CollectHierarchyEmployeeIds(
        int rootEmployeeId,
        IReadOnlyDictionary<int, List<int>> childrenByManager,
        IReadOnlyDictionary<int, EmployeeProfile> profilesByEmployeeId)
    {
        var employeeIds = new HashSet<int>();
        CollectHierarchyEmployeeIds(
            rootEmployeeId,
            childrenByManager,
            profilesByEmployeeId,
            employeeIds);

        return employeeIds;
    }

    private static Dictionary<int, List<int>> BuildNameHierarchy(
        IReadOnlyList<EmployeeProfile> profiles)
    {
        var nameLookup = BuildNameLookup(profiles);
        var childrenByManager = new Dictionary<int, List<int>>();

        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.ManagerLookupValue))
            {
                continue;
            }

            var normalizedManagerName = Normalize(profile.ManagerLookupValue);
            if (!nameLookup.TryGetValue(normalizedManagerName, out var managerIds))
            {
                continue;
            }

            if (managerIds.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Manager name '{profile.ManagerLookupValue}' is ambiguous in BambooHR.");
            }

            var managerId = managerIds[0];
            if (!childrenByManager.TryGetValue(managerId, out var children))
            {
                children = [];
                childrenByManager[managerId] = children;
            }

            children.Add(profile.EmployeeId);
        }

        return childrenByManager;
    }

    private static Dictionary<string, List<int>> BuildNameLookup(
        IEnumerable<EmployeeProfile> profiles)
    {
        var lookup = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles)
        {
            foreach (var candidate in GetCandidateNames(profile))
            {
                var normalized = Normalize(candidate);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!lookup.TryGetValue(normalized, out var employeeIds))
                {
                    employeeIds = [];
                    lookup[normalized] = employeeIds;
                }

                if (!employeeIds.Contains(profile.EmployeeId))
                {
                    employeeIds.Add(profile.EmployeeId);
                }
            }
        }

        return lookup;
    }

    private static IEnumerable<string> GetCandidateNames(EmployeeProfile profile)
    {
        yield return profile.DisplayName;
        yield return $"{profile.FirstName} {profile.LastName}";
        yield return $"{profile.PreferredName} {profile.LastName}";
    }

    private static string Normalize(string value)
    {
        var buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(buffer);
    }

    private static void FlattenHierarchy(
        int employeeId,
        int level,
        IReadOnlyDictionary<int, EmployeeProfile> profilesByEmployeeId,
        IReadOnlyDictionary<int, List<int>> childrenByManager,
        IReadOnlyDictionary<int, IReadOnlyList<TimeOffEntry>> employeeEntries,
        ICollection<HierarchyReportRow> rows)
    {
        var profile = profilesByEmployeeId[employeeId];
        List<TimeOffEntry> entries = [];
        if (employeeEntries.TryGetValue(employeeId, out var personalEntries))
        {
            entries.AddRange(personalEntries);
        }

        var managerName = ResolveManagerName(profile, profilesByEmployeeId);
        rows.Add(new HierarchyReportRow(
            level,
            profile.EmployeeId,
            profile.DisplayName,
            profile.Department,
            profile.JobTitle,
            profile.Location,
            profile.DateOfBirth,
            profile.HireDate,
            managerName,
            [
                .. entries
                .OrderBy(entry => entry.Start)
                .ThenBy(entry => entry.End)
            ]));

        if (!childrenByManager.TryGetValue(employeeId, out var children))
        {
            return;
        }

        foreach (var childId in children
                     .Distinct()
                     .OrderBy(
                         id => profilesByEmployeeId[id].DisplayName,
                         StringComparer.OrdinalIgnoreCase)
                     .ThenBy(id => id))
        {
            FlattenHierarchy(
                childId,
                level + 1,
                profilesByEmployeeId,
                childrenByManager,
                employeeEntries,
                rows);
        }
    }

    private static void CollectHierarchyEmployeeIds(
        int employeeId,
        IReadOnlyDictionary<int, List<int>> childrenByManager,
        IReadOnlyDictionary<int, EmployeeProfile> profilesByEmployeeId,
        ISet<int> employeeIds)
    {
        if (!profilesByEmployeeId.ContainsKey(employeeId)
            || !employeeIds.Add(employeeId)
            || !childrenByManager.TryGetValue(employeeId, out var children))
        {
            return;
        }

        foreach (var childId in children
                     .Distinct()
                     .OrderBy(
                         id => profilesByEmployeeId[id].DisplayName,
                         StringComparer.OrdinalIgnoreCase)
                     .ThenBy(id => id))
        {
            CollectHierarchyEmployeeIds(
                childId,
                childrenByManager,
                profilesByEmployeeId,
                employeeIds);
        }
    }

    private static string? ResolveManagerName(
        EmployeeProfile profile,
        IReadOnlyDictionary<int, EmployeeProfile> profilesByEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(profile.ManagerLookupValue))
        {
            return null;
        }

        return int.TryParse(profile.ManagerLookupValue, out var managerId)
               && profilesByEmployeeId.TryGetValue(managerId, out var managerProfile)
            ? managerProfile.DisplayName
            : profile.ManagerLookupValue;
    }

    private async Task<HierarchyRelationshipField> ResolveRelationshipFieldAsync(
        int rootEmployeeId,
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

    private async Task<IReadOnlyList<EmployeeProfile>> LoadProfilesAsync(
        IReadOnlyList<BasicEmployee> employees,
        HierarchyRelationshipField relationshipField,
        BambooHrField? locationField,
        BambooHrField? countryField,
        BambooHrField? cityField,
        BambooHrField? birthDateField,
        BambooHrField? hireDateField,
        CancellationToken ct)
    {
        var requestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "department",
            "jobTitle",
            "workEmail",
            relationshipField.RequestKey
        };
        if (locationField is not null)
        {
            _ = requestKeys.Add(locationField.RequestKey);
        }

        if (countryField is not null)
        {
            _ = requestKeys.Add(countryField.RequestKey);
        }

        if (cityField is not null)
        {
            _ = requestKeys.Add(cityField.RequestKey);
        }

        if (birthDateField is not null)
        {
            _ = requestKeys.Add(birthDateField.RequestKey);
        }

        if (hireDateField is not null)
        {
            _ = requestKeys.Add(hireDateField.RequestKey);
        }

        var profiles = new ConcurrentBag<EmployeeProfile>();
        var loadedProfiles = 0;
        var options = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 2
        };

        await Parallel.ForEachAsync(
                employees,
                options,
                async (employee, cancellationToken) =>
                {
                    var fieldValues = await _bambooHrClient.GetEmployeeFieldsAsync(
                            employee.EmployeeId,
                            requestKeys,
                            cancellationToken)
                        .ConfigureAwait(false);

                    _ = fieldValues.Values.TryGetValue("department", out var department);
                    _ = fieldValues.Values.TryGetValue("jobTitle", out var jobTitle);
                    _ = fieldValues.Values.TryGetValue("workEmail", out var workEmail);
                    _ = TryGetFieldValue(fieldValues, locationField, out var location);
                    _ = TryGetFieldValue(fieldValues, countryField, out var country);
                    _ = TryGetFieldValue(fieldValues, cityField, out var city);
                    _ = TryGetFieldValue(fieldValues, birthDateField, out var birthDateValue);
                    _ = TryGetFieldValue(fieldValues, hireDateField, out var hireDateValue);
                    _ = fieldValues.Values.TryGetValue(
                        relationshipField.RequestKey,
                        out var managerLookupValue);

                    profiles.Add(new EmployeeProfile(
                        employee.EmployeeId,
                        employee.DisplayName,
                        employee.FirstName,
                        employee.LastName,
                        employee.PreferredName,
                        department,
                        jobTitle ?? employee.JobTitle,
                        location,
                        country,
                        city,
                        ParseDate(birthDateValue),
                        ParseDate(hireDateValue),
                        workEmail,
                        managerLookupValue));

                    var completed = Interlocked.Increment(ref loadedProfiles);
                    _loadingNotifier.SetProgress(
                        $"Loading employee profiles ({completed}/{employees.Count})...",
                        completed,
                        employees.Count);
                })
            .ConfigureAwait(false);

        return [.. profiles.OrderBy(profile => profile.EmployeeId)];
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

    private static string ResolveGradeLabel(string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(jobTitle))
        {
            return "Unspecified";
        }

        if (jobTitle.Contains("Tech Lead", StringComparison.OrdinalIgnoreCase))
        {
            return "Tech Lead";
        }

        if (jobTitle.Contains("Team Lead", StringComparison.OrdinalIgnoreCase))
        {
            return "Team Lead";
        }

        if (jobTitle.Contains("Senior", StringComparison.OrdinalIgnoreCase))
        {
            return "Senior";
        }

        if (jobTitle.Contains("Middle", StringComparison.OrdinalIgnoreCase)
            || jobTitle.Contains("Mid", StringComparison.OrdinalIgnoreCase))
        {
            return "Middle";
        }

        if (jobTitle.Contains("Junior", StringComparison.OrdinalIgnoreCase))
        {
            return "Junior";
        }

        if (jobTitle.Contains("Lead", StringComparison.OrdinalIgnoreCase))
        {
            return "Lead";
        }

        if (jobTitle.Contains("Manager", StringComparison.OrdinalIgnoreCase))
        {
            return "Manager";
        }

        if (jobTitle.Contains("Director", StringComparison.OrdinalIgnoreCase))
        {
            return "Director";
        }

        if (jobTitle.Contains("Head", StringComparison.OrdinalIgnoreCase))
        {
            return "Head";
        }

        return jobTitle.Trim();
    }

    private static string ClassifyAge(DateOnly? dateOfBirth, DateOnly referenceDate)
    {
        if (dateOfBirth is null)
        {
            return "Unknown";
        }

        var age = GetFullYears(dateOfBirth.Value, referenceDate);
        if (age < 25)
        {
            return "<25";
        }

        if (age < 35)
        {
            return "25-34";
        }

        if (age < 45)
        {
            return "35-44";
        }

        if (age < 55)
        {
            return "45-54";
        }

        return "55+";
    }

    private static string ClassifyTenure(DateOnly? hireDate, DateOnly referenceDate)
    {
        if (hireDate is null)
        {
            return "Unknown";
        }

        var tenure = GetFullYears(hireDate.Value, referenceDate);
        if (tenure < 1)
        {
            return "<1 year";
        }

        if (tenure < 3)
        {
            return "1-2 years";
        }

        if (tenure < 6)
        {
            return "3-5 years";
        }

        if (tenure < 11)
        {
            return "6-10 years";
        }

        return "10+ years";
    }

    private static int GetFullYears(DateOnly startDate, DateOnly endDate)
    {
        var years = endDate.Year - startDate.Year;
        if (endDate < startDate.AddYears(years))
        {
            years--;
        }

        return years;
    }

    private static int GetAgeBucketOrder(string bucket)
    {
        return bucket switch
        {
            "<25" => 0,
            "25-34" => 1,
            "35-44" => 2,
            "45-54" => 3,
            "55+" => 4,
            _ => 5
        };
    }

    private static int GetTenureBucketOrder(string bucket)
    {
        return bucket switch
        {
            "<1 year" => 0,
            "1-2 years" => 1,
            "3-5 years" => 2,
            "6-10 years" => 3,
            "10+ years" => 4,
            _ => 5
        };
    }

    private static bool TryGetFieldValue(
        EmployeeFieldValues fieldValues,
        BambooHrField? field,
        out string? value)
    {
        value = null;
        return field is not null
            && fieldValues.Values.TryGetValue(field.RequestKey, out value);
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        if (DateTime.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal,
                out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return null;
    }

    private static string ResolveCountryLabel(EmployeeProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Country))
        {
            return profile.Country;
        }

        if (TryParseLocation(profile.Location, out _, out var country))
        {
            return country!;
        }

        return string.IsNullOrWhiteSpace(profile.Location)
            ? "Unknown"
            : profile.Location!;
    }

    private static string? ResolveCityLabel(EmployeeProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.City))
        {
            return profile.City;
        }

        return TryParseLocation(profile.Location, out var city, out _)
            ? city
            : null;
    }

    private static bool IsHolidayApplicable(
        EmployeeProfile profile,
        TimeOffEntry holiday,
        int distinctCountryCount)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(holiday);

        if (distinctCountryCount <= 1)
        {
            return true;
        }

        var holidayName = Normalize(holiday.Name);
        if (string.IsNullOrWhiteSpace(holidayName))
        {
            return false;
        }

        var country = Normalize(ResolveCountryLabel(profile));
        if (!string.IsNullOrWhiteSpace(country)
            && !string.Equals(country, "unknown", StringComparison.OrdinalIgnoreCase)
            && holidayName.Contains(country, StringComparison.Ordinal))
        {
            return true;
        }

        var city = Normalize(ResolveCityLabel(profile) ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(city)
            && holidayName.Contains(city, StringComparison.Ordinal))
        {
            return true;
        }

        var location = Normalize(profile.Location ?? string.Empty);
        return !string.IsNullOrWhiteSpace(location)
            && holidayName.Contains(location, StringComparison.Ordinal);
    }

    private static bool TryParseLocation(
        string? location,
        out string? city,
        out string? country)
    {
        city = null;
        country = null;
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var parts = location
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        city = parts[0];
        country = parts[^1];
        return !string.IsNullOrWhiteSpace(city)
            && !string.IsNullOrWhiteSpace(country);
    }
}
