using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Builds and traverses employee hierarchy topology.
/// </summary>
public sealed class HierarchyTopologyBuilder : IHierarchyTopologyBuilder
{
    /// <inheritdoc />
    public IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> BuildChildrenByManager(
        IReadOnlyList<EmployeeProfile> profiles,
        HierarchyRelationshipField relationshipField)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(relationshipField);

        return relationshipField.UsesEmployeeId
            ? BuildEmployeeIdHierarchy(profiles)
            : BuildNameHierarchy(profiles);
    }

    /// <inheritdoc />
    public HashSet<EmployeeId> CollectHierarchyEmployeeIds(
        EmployeeId rootEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId)
    {
        ArgumentNullException.ThrowIfNull(childrenByManager);
        ArgumentNullException.ThrowIfNull(profilesByEmployeeId);

        var employeeIds = new HashSet<EmployeeId>();
        CollectHierarchyEmployeeIds(
            rootEmployeeId,
            childrenByManager,
            profilesByEmployeeId,
            employeeIds);

        return employeeIds;
    }

    /// <inheritdoc />
    public void FlattenHierarchy(
        EmployeeId employeeId,
        int level,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> employeeEntries,
        ICollection<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(profilesByEmployeeId);
        ArgumentNullException.ThrowIfNull(childrenByManager);
        ArgumentNullException.ThrowIfNull(employeeEntries);
        ArgumentNullException.ThrowIfNull(rows);

        var profile = profilesByEmployeeId[employeeId];
        List<TimeOffEntry> entries = [];
        if (employeeEntries.TryGetValue(employeeId, out var personalEntries))
        {
            entries.AddRange(personalEntries);
        }

        rows.Add(new HierarchyReportRow(
            level,
            profile.EmployeeId,
            profile.DisplayName,
            profile.Department,
            profile.JobTitle,
            profile.Location,
            profile.DateOfBirth,
            profile.HireDate,
            profile.ResolveManagerDisplayName(profilesByEmployeeId),
            [
                .. entries
                    .OrderBy(entry => entry.Start)
                    .ThenBy(entry => entry.End)
            ],
            profile.WorkEmail,
            profile.Team));

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

    /// <summary>
    /// Builds manager-child relations when BambooHR stores explicit manager employee identifiers.
    /// </summary>
    private static Dictionary<EmployeeId, IReadOnlyList<EmployeeId>> BuildEmployeeIdHierarchy(
        IEnumerable<EmployeeProfile> profiles)
    {
        var childrenByManager = new Dictionary<EmployeeId, List<EmployeeId>>();

        foreach (var profile in profiles)
        {
            if (!profile.TryGetManagerEmployeeId(out var managerId))
            {
                continue;
            }

            AddChild(childrenByManager, managerId, profile.EmployeeId);
        }

        return ToReadOnlyDictionary(childrenByManager);
    }

    /// <summary>
    /// Builds manager-child relations when BambooHR stores manager names instead of identifiers.
    /// </summary>
    private static Dictionary<EmployeeId, IReadOnlyList<EmployeeId>> BuildNameHierarchy(
        IReadOnlyList<EmployeeProfile> profiles)
    {
        var nameLookup = BuildNameLookup(profiles);
        var childrenByManager = new Dictionary<EmployeeId, List<EmployeeId>>();

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

            AddChild(childrenByManager, managerIds[0], profile.EmployeeId);
        }

        return ToReadOnlyDictionary(childrenByManager);
    }

    /// <summary>
    /// Builds normalized employee name lookup used for manager-name matching.
    /// </summary>
    private static Dictionary<string, List<EmployeeId>> BuildNameLookup(
        IEnumerable<EmployeeProfile> profiles)
    {
        var lookup = new Dictionary<string, List<EmployeeId>>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles)
        {
            foreach (var candidate in profile.CandidateNames)
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

    /// <summary>
    /// Converts mutable child collections into read-only lists.
    /// </summary>
    private static Dictionary<EmployeeId, IReadOnlyList<EmployeeId>> ToReadOnlyDictionary(
        Dictionary<EmployeeId, List<EmployeeId>> childrenByManager)
    {
        return childrenByManager.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<EmployeeId>)[.. pair.Value],
            EqualityComparer<EmployeeId>.Default);
    }

    /// <summary>
    /// Adds one child employee under the specified manager.
    /// </summary>
    private static void AddChild(
        Dictionary<EmployeeId, List<EmployeeId>> childrenByManager,
        EmployeeId managerId,
        EmployeeId childId)
    {
        if (!childrenByManager.TryGetValue(managerId, out var children))
        {
            children = [];
            childrenByManager[managerId] = children;
        }

        children.Add(childId);
    }

    /// <summary>
    /// Recursively collects the root employee and all reachable descendants.
    /// </summary>
    private static void CollectHierarchyEmployeeIds(
        EmployeeId employeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId,
        ISet<EmployeeId> employeeIds)
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

    /// <summary>
    /// Normalizes employee names for resilient BambooHR manager matching.
    /// </summary>
    private static string Normalize(string value)
    {
        var buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(buffer);
    }
}
