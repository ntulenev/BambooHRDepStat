using Models;

namespace Abstractions;

/// <summary>
/// Renders the hierarchy report as CSV export.
/// </summary>
public interface ICsvReportRenderer
{
    /// <summary>
    /// Renders the report.
    /// </summary>
    void Render(HierarchyReport report);
}
