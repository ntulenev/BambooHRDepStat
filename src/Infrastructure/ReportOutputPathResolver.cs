using System.Globalization;

namespace Infrastructure;

/// <summary>
/// Resolves dated output paths for generated reports.
/// </summary>
internal static class ReportOutputPathResolver
{
    public static string Resolve(
        string? configuredPath,
        string fallbackRelativePath,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackRelativePath);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var candidatePath = string.IsNullOrWhiteSpace(configuredPath)
            ? fallbackRelativePath
            : configuredPath.Trim();
        var absolutePath = Path.IsPathRooted(candidatePath)
            ? Path.GetFullPath(candidatePath)
            : Path.GetFullPath(candidatePath, AppContext.BaseDirectory);
        var directoryPath = Path.GetDirectoryName(absolutePath);
        var extension = Path.GetExtension(absolutePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
        var timestamp = timeProvider.GetLocalNow()
            .ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var datedFileName = fileNameWithoutExtension + "_" + timestamp + extension;

        return string.IsNullOrWhiteSpace(directoryPath)
            ? datedFileName
            : Path.Combine(directoryPath, datedFileName);
    }
}
