using Models;

namespace Abstractions;

/// <summary>
/// Renders the hierarchy report to the interactive console.
/// </summary>
public interface IConsoleReportRenderer
{
    /// <summary>
    /// Renders the report.
    /// </summary>
    void Render(HierarchyReport report);
}
