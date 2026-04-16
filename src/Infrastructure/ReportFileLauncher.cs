using System.Diagnostics;

using Abstractions;

namespace Infrastructure;

/// <summary>
/// Opens generated report files via the shell.
/// </summary>
public sealed class ReportFileLauncher : IReportFileLauncher
{
    /// <inheritdoc/>
    public void Open(string reportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = reportPath,
            UseShellExecute = true
        });
    }
}
