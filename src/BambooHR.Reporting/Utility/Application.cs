using Abstractions;

using Models;

namespace BambooHR.Reporting.Utility;

/// <summary>
/// Runs the BambooHR report workflow.
/// </summary>
public sealed class Application : IApplication
{
    private readonly IHierarchyReportBuilder _hierarchyReportBuilder;
    private readonly ILoadingNotifier _loadingNotifier;
    private readonly IReportWriter _reportWriter;
    private readonly BambooHrOptions _options;

    /// <summary>
    /// Creates application.
    /// </summary>
    public Application(
        IHierarchyReportBuilder hierarchyReportBuilder,
        ILoadingNotifier loadingNotifier,
        IReportWriter reportWriter,
        BambooHrOptions options)
    {
        ArgumentNullException.ThrowIfNull(hierarchyReportBuilder);
        ArgumentNullException.ThrowIfNull(loadingNotifier);
        ArgumentNullException.ThrowIfNull(reportWriter);
        ArgumentNullException.ThrowIfNull(options);

        _hierarchyReportBuilder = hierarchyReportBuilder;
        _loadingNotifier = loadingNotifier;
        _reportWriter = reportWriter;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = args;

        var report = await _loadingNotifier.RunAsync(
                "Connecting to BambooHR...",
                async cancellationToken => await _hierarchyReportBuilder
                    .BuildAsync(_options.RootEmployeeId, cancellationToken)
                    .ConfigureAwait(false),
                ct)
            .ConfigureAwait(false);
        _reportWriter.Write(report);
    }
}
