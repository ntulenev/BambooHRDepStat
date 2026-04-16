namespace Abstractions;

/// <summary>
/// Opens generated report files via the operating system shell.
/// </summary>
public interface IReportFileLauncher
{
    /// <summary>
    /// Opens the generated report file with the default associated application.
    /// </summary>
    /// <param name="reportPath">Path to the generated report file.</param>
    void Open(string reportPath);
}
