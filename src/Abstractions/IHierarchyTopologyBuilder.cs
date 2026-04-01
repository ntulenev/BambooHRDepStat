using Models;

namespace Abstractions;

/// <summary>
/// Builds and traverses the employee hierarchy topology.
/// </summary>
public interface IHierarchyTopologyBuilder
{
    /// <summary>
    /// Builds a child lookup grouped by manager.
    /// </summary>
    IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> BuildChildrenByManager(
        IReadOnlyList<EmployeeProfile> profiles,
        HierarchyRelationshipField relationshipField);

    /// <summary>
    /// Collects all employee identifiers included under the specified root.
    /// </summary>
    HashSet<EmployeeId> CollectHierarchyEmployeeIds(
        EmployeeId rootEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId);

    /// <summary>
    /// Flattens a hierarchy tree into report rows.
    /// </summary>
    void FlattenHierarchy(
        EmployeeId employeeId,
        int level,
        IReadOnlyDictionary<EmployeeId, EmployeeProfile> profilesByEmployeeId,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager,
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> employeeEntries,
        ICollection<HierarchyReportRow> rows);
}
