using System.Collections.Concurrent;

using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Builds a hierarchy report for one employee and descendants.
/// </summary>
public sealed class HierarchyReportBuilder : IHierarchyReportBuilder
{
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
    private readonly IWorkWeekProvider _workWeekProvider;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates report builder.
    /// </summary>
    public HierarchyReportBuilder(
        IBambooHrClient bambooHrClient,
        IWorkWeekProvider workWeekProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        ArgumentNullException.ThrowIfNull(workWeekProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bambooHrClient = bambooHrClient;
        _workWeekProvider = workWeekProvider;
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

        var workWeek = _workWeekProvider.GetWorkWeek(_timeProvider.GetLocalNow());
        var relationshipField = await ResolveRelationshipFieldAsync(rootEmployeeId, ct)
            .ConfigureAwait(false);
        var employees = await _bambooHrClient.GetEmployeesAsync(ct).ConfigureAwait(false);
        var profiles = await LoadProfilesAsync(employees, relationshipField, ct)
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

        var whoIsOut = await _bambooHrClient.GetWhosOutAsync(
                workWeek.Start,
                workWeek.End,
                ct)
            .ConfigureAwait(false);

        var employeeEntries = whoIsOut
            .Where(entry => entry.Type == TimeOffEntryType.TimeOff && entry.EmployeeId.HasValue)
            .GroupBy(entry => entry.EmployeeId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TimeOffEntry>)[
                    .. group
                    .OrderBy(entry => entry.Start)
                    .ThenBy(entry => entry.End)
                ]);

        List<HierarchyReportRow> rows = [];
        FlattenHierarchy(
            rootEmployeeId,
            level: 0,
            profilesByEmployeeId,
            childrenByManager,
            employeeEntries,
            rows);

        return new HierarchyReport(
            workWeek,
            rootEmployee.DisplayName,
            relationshipField,
            rows);
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
        CancellationToken ct)
    {
        var requestKeys = new[]
        {
            "department",
            "jobTitle",
            "workEmail",
            relationshipField.RequestKey
        };

        var profiles = new ConcurrentBag<EmployeeProfile>();
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
                        workEmail,
                        managerLookupValue));
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
}
