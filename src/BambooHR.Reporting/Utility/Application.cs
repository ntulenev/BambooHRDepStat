using Abstractions;

namespace BambooHR.Reporting.Utility;

/// <summary>
/// Runs the BambooHR report workflow.
/// </summary>
public sealed class Application : IApplication
{
    /// <summary>
    /// Creates application.
    /// </summary>
    public Application(
        IHierarchyReportBuilder hierarchyReportBuilder,
        ILoadingNotifier loadingNotifier,
        IReportWriter reportWriter,
        HierarchyReportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(hierarchyReportBuilder);
        ArgumentNullException.ThrowIfNull(loadingNotifier);
        ArgumentNullException.ThrowIfNull(reportWriter);
        ArgumentNullException.ThrowIfNull(settings);

        _hierarchyReportBuilder = hierarchyReportBuilder;
        _loadingNotifier = loadingNotifier;
        _reportWriter = reportWriter;
        _settings = settings;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string[] args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = args;

        var report = await _loadingNotifier.RunAsync(
                "Connecting to BambooHR...",
                async cancellationToken => await _hierarchyReportBuilder
                    .BuildAsync(_settings.RootEmployeeId, cancellationToken)
                    .ConfigureAwait(false),
                ct)
            .ConfigureAwait(false);
        _reportWriter.Write(report);
    }

    private readonly IHierarchyReportBuilder _hierarchyReportBuilder;
    private readonly ILoadingNotifier _loadingNotifier;
    private readonly IReportWriter _reportWriter;
    private readonly HierarchyReportSettings _settings;
}
