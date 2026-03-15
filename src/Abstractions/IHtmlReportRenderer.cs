using Models;

namespace Abstractions;

/// <summary>
/// Renders the hierarchy report as HTML.
/// </summary>
public interface IHtmlReportRenderer
{
    /// <summary>
    /// Renders the report.
    /// </summary>
    void Render(HierarchyReport report);
}
