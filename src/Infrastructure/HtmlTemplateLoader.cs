namespace Infrastructure;

/// <summary>
/// Loads embedded HTML templates used by report generation.
/// </summary>
internal static class HtmlTemplateLoader
{
    private static readonly Lazy<string> _hierarchyReportTemplate =
        new(() => LoadTemplate("Infrastructure.HtmlTemplates.HierarchyReport.html"));

    public static string LoadHierarchyReportTemplate() => _hierarchyReportTemplate.Value;

    private static string LoadTemplate(string resourceName)
    {
        var assembly = typeof(HtmlTemplateLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded HTML template '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
