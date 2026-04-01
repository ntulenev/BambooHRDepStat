using System.Text;

namespace Infrastructure;

/// <summary>
/// Filesystem-backed HTML report store.
/// </summary>
public sealed class HtmlReportFileStore
{
    public void Save(string outputPath, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(html);
        _ = _instanceMarker;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, html, Encoding.UTF8);
    }

    private readonly int _instanceMarker = 1;
}
