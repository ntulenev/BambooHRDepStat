using Models;

namespace Abstractions;

/// <summary>
/// Renders the hierarchy report as PDF.
/// </summary>
public interface IPdfReportRenderer
{
    /// <summary>
    /// Renders the report.
    /// </summary>
    void Render(HierarchyReport report);
}
