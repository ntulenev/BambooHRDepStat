using Models;

namespace Abstractions;

/// <summary>
/// Builds BambooHR hierarchy report.
/// </summary>
public interface IHierarchyReportBuilder
{
    /// <summary>
    /// Builds report for the configured employee hierarchy.
    /// </summary>
    Task<HierarchyReport> BuildAsync(int rootEmployeeId, CancellationToken ct);
}
