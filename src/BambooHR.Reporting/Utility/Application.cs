using Abstractions;

using Models;

namespace BambooHR.Reporting.Utility;

/// <summary>
/// Runs the BambooHR report workflow.
/// </summary>
public sealed class Application : IApplication
{
    private readonly IHierarchyReportBuilder _hierarchyReportBuilder;
    private readonly IReportWriter _reportWriter;
    private readonly BambooHrOptions _options;

    /// <summary>
    /// Creates application.
    /// </summary>
    public Application(
        IHierarchyReportBuilder hierarchyReportBuilder,
        IReportWriter reportWriter,
        BambooHrOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchyReportBuilder);
        ArgumentNullException.ThrowIfNull(reportWriter);
        ArgumentNullException.ThrowIfNull(options);

        _hierarchyReportBuilder = hierarchyReportBuilder;
        _reportWriter = reportWriter;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = args;

        var report = await _hierarchyReportBuilder.BuildAsync(_options.EmployeeId, ct)
            .ConfigureAwait(false);
        _reportWriter.Write(report);
    }
}
