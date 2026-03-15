using System.Diagnostics;

namespace Infrastructure;

/// <summary>
/// Opens generated HTML reports via the shell.
/// </summary>
public sealed class HtmlReportLauncher
{
    private readonly int _instanceMarker = 1;

    /// <summary>
    /// Opens the generated HTML report in the default browser.
    /// </summary>
    public void Open(string htmlReportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlReportPath);
        _ = _instanceMarker;

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = htmlReportPath,
            UseShellExecute = true
        });
    }
}
