using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Builds hierarchy summaries and analytics used by report renderers.
/// </summary>
public sealed class HierarchyAnalytics : IHierarchyAnalytics
{
    /// <inheritdoc />
    public IReadOnlyList<HierarchyTeam> BuildTeams(
        IReadOnlyList<HierarchyReportRow> rows,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(profilesByEmployeeId);
        ArgumentNullException.ThrowIfNull(childrenByManager);

        var includedEmployeeIds = rows
            .Select(row => row.EmployeeId)
            .ToHashSet();
        var rowsByEmployeeId = rows.ToDictionary(row => row.EmployeeId);
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
                    profilesByEmployeeId),
                BuildTeamRows(
                    row.EmployeeId,
                    directReports,
                    includedEmployeeIds,
                    childrenByManager,
                    rowsByEmployeeId,
                    profilesByEmployeeId)));
        }

        return teams;
    }

    /// <inheritdoc />
    public IReadOnlyList<HierarchyReportRow> BuildRecentHires(
        IReadOnlyList<HierarchyReportRow> rows,
        DateOnly referenceDate,
        int recentHirePeriodDays)
    {
        ArgumentNullException.ThrowIfNull(rows);

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

    /// <inheritdoc />
    public Dictionary<string, int> BuildLocationCounts(
        IEnumerable<EmployeeProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        return profiles
            .Select(profile => profile.ResolveCountryLabel())
            .GroupBy(location => location, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.First(),
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Dictionary<string, IReadOnlyDictionary<string, int>> BuildCountryCityCounts(
        IEnumerable<EmployeeProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        return profiles
            .Select(profile => (Country: profile.ResolveCountryLabel(), City: profile.ResolveCityLabel()))
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

    /// <inheritdoc />
    public Dictionary<string, int> BuildAgeCounts(
        IEnumerable<EmployeeProfile> profiles,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(profiles);

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

    /// <inheritdoc />
    public Dictionary<string, int> BuildTenureCounts(
        IEnumerable<EmployeeProfile> profiles,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(profiles);

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

    /// <summary>
    /// Builds grade distribution for one team including the manager.
    /// </summary>
    private static Dictionary<string, int> BuildGradeCounts(
        EmployeeId managerEmployeeId,
        IEnumerable<EmployeeId> directReports,
        HashSet<EmployeeId> includedEmployeeIds,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId)
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

    /// <summary>
    /// Builds the flat rows rendered for one team section.
    /// </summary>
    private static IReadOnlyList<HierarchyReportRow> BuildTeamRows(
        EmployeeId managerEmployeeId,
        IEnumerable<EmployeeId> directReports,
        HashSet<EmployeeId> includedEmployeeIds,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        Dictionary<EmployeeId, HierarchyReportRow> rowsByEmployeeId,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId)
    {
        return
        [
            rowsByEmployeeId[managerEmployeeId],
            .. directReports
                .Distinct()
                .Where(id => includedEmployeeIds.Contains(id))
                .Where(id => !childrenByManager.ContainsKey(id))
                .OrderBy(
                    id => profilesByEmployeeId[id].DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(id => id)
                .Select(id => rowsByEmployeeId[id])
        ];
    }

    /// <summary>
    /// Resolves a report grade bucket from a job title.
    /// </summary>
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

    /// <summary>
    /// Maps an employee date of birth to an age bucket.
    /// </summary>
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

    /// <summary>
    /// Maps an employee hire date to a tenure bucket.
    /// </summary>
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

    /// <summary>
    /// Calculates full elapsed years between two dates.
    /// </summary>
    private static int GetFullYears(DateOnly startDate, DateOnly endDate)
    {
        var years = endDate.Year - startDate.Year;
        if (endDate < startDate.AddYears(years))
        {
            years--;
        }

        return years;
    }

    /// <summary>
    /// Returns deterministic display ordering for age buckets.
    /// </summary>
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

    /// <summary>
    /// Returns deterministic display ordering for tenure buckets.
    /// </summary>
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
}
