namespace Models;

/// <summary>
/// Summary of a manager-led team made of leaf direct reports.
/// </summary>
public sealed class HierarchyTeam
{
    /// <summary>
    /// Creates team summary.
    /// </summary>
    public HierarchyTeam(
        int managerEmployeeId,
        string managerDisplayName,
        IReadOnlyList<string> memberDisplayNames,
        IReadOnlyDictionary<string, int> gradeCounts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managerDisplayName);
        ArgumentNullException.ThrowIfNull(memberDisplayNames);
        ArgumentNullException.ThrowIfNull(gradeCounts);

        ManagerEmployeeId = managerEmployeeId;
        ManagerDisplayName = managerDisplayName;
        MemberDisplayNames = memberDisplayNames;
        GradeCounts = gradeCounts;
    }

    /// <summary>
    /// Gets manager identifier.
    /// </summary>
    public int ManagerEmployeeId { get; }

    /// <summary>
    /// Gets manager display name.
    /// </summary>
    public string ManagerDisplayName { get; }

    /// <summary>
    /// Gets direct-report leaf team member names.
    /// </summary>
    public IReadOnlyList<string> MemberDisplayNames { get; }

    /// <summary>
    /// Gets people counts grouped by grade.
    /// </summary>
    public IReadOnlyDictionary<string, int> GradeCounts { get; }

    /// <summary>
    /// Gets total people count including the manager.
    /// </summary>
    public int PeopleCount => MemberDisplayNames.Count + 1;
}
