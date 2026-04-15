using System.Text;

using Models;

namespace Infrastructure;

/// <summary>
/// Filesystem-backed CSV export store.
/// </summary>
public sealed class CsvReportFileStore
{
    public void Save(string outputPath, IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(rows);
        _ = _instanceMarker;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        using var writer = new StreamWriter(
            outputPath,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine("Employee Name,Email,Team");

        foreach (var row in rows)
        {
            writer.WriteLine(
                $"{Escape(row.DisplayName)},{Escape(row.WorkEmail ?? string.Empty)},{Escape(row.Team ?? string.Empty)}");
        }
    }

    private static string Escape(string value)
    {
        var normalizedValue = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return normalizedValue.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{normalizedValue}\""
            : normalizedValue;
    }

    private readonly int _instanceMarker = 1;
}
