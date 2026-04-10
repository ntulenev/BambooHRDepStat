namespace Models;

/// <summary>
/// CSV export output settings.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether CSV export generation is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the output path for the CSV export file.
    /// </summary>
    public string OutputPath { get; set; } = Path.Combine("reports", "bamboohr-employee-export.csv");
}
