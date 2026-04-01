using Models;

namespace Abstractions;

/// <summary>
/// Resolves BambooHR field aliases needed for hierarchy report generation.
/// </summary>
public interface IHierarchyFieldResolver
{
    /// <summary>
    /// Resolves the BambooHR field set used to build the report.
    /// </summary>
    Task<HierarchyFieldSelection> ResolveAsync(
        EmployeeId rootEmployeeId,
        CancellationToken ct);
}
