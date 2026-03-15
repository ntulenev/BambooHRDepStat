using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure;

/// <summary>
/// Filesystem-backed PDF report store.
/// </summary>
public sealed class PdfReportFileStore
{
    private readonly int _instanceMarker = 1;

    public void Save(string outputPath, IDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(document);
        _ = _instanceMarker;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        var pdfBytes = document.GeneratePdf();
        File.WriteAllBytes(outputPath, pdfBytes);
    }
}
