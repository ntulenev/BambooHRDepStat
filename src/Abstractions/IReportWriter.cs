using Models;

namespace Abstractions;

/// <summary>
/// Writes hierarchy report to output.
/// </summary>
public interface IReportWriter
{
    /// <summary>
    /// Writes report.
    /// </summary>
    void Write(HierarchyReport report);
}
