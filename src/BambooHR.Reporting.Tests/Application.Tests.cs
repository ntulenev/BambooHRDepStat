using Abstractions;

using BambooHR.Reporting.Utility;

using FluentAssertions;

using Moq;

using Models;

namespace BambooHR.Reporting.Tests;

public sealed class ApplicationTests
{
    [Fact(DisplayName = "The constructor throws when the hierarchy report builder is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenHierarchyReportBuilderIsNull()
    {
        // Arrange
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict).Object;
        var reportWriter = new Mock<IReportWriter>(MockBehavior.Strict).Object;
        var settings = CreateSettings();

        // Act
        var action = () => new Application(null!, loadingNotifier, reportWriter, settings);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the loading notifier is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenLoadingNotifierIsNull()
    {
        // Arrange
        var hierarchyReportBuilder = new Mock<IHierarchyReportBuilder>(MockBehavior.Strict).Object;
        var reportWriter = new Mock<IReportWriter>(MockBehavior.Strict).Object;
        var settings = CreateSettings();

        // Act
        var action = () => new Application(hierarchyReportBuilder, null!, reportWriter, settings);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the report writer is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenReportWriterIsNull()
    {
        // Arrange
        var hierarchyReportBuilder = new Mock<IHierarchyReportBuilder>(MockBehavior.Strict).Object;
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict).Object;
        var settings = CreateSettings();

        // Act
        var action = () => new Application(hierarchyReportBuilder, loadingNotifier, null!, settings);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the settings are null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSettingsAreNull()
    {
        // Arrange
        var hierarchyReportBuilder = new Mock<IHierarchyReportBuilder>(MockBehavior.Strict).Object;
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict).Object;
        var reportWriter = new Mock<IReportWriter>(MockBehavior.Strict).Object;

        // Act
        var action = () => new Application(hierarchyReportBuilder, loadingNotifier, reportWriter, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The application stops immediately when the cancellation token is already canceled.")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldThrowWhenCancellationIsAlreadyRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var application = CreateApplication(
            new Mock<IHierarchyReportBuilder>(MockBehavior.Strict).Object,
            new Mock<ILoadingNotifier>(MockBehavior.Strict).Object,
            new Mock<IReportWriter>(MockBehavior.Strict).Object,
            CreateSettings());

        // Act
        Func<Task> action = () => application.RunAsync([], cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "The application loads the hierarchy report and writes it after the notifier completes the workflow.")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldBuildAndWriteReportThroughLoadingNotifier()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var expectedReport = CreateReport();
        var hierarchyReportBuilder = new Mock<IHierarchyReportBuilder>(MockBehavior.Strict);
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict);
        var reportWriter = new Mock<IReportWriter>(MockBehavior.Strict);
        var buildAsyncCalls = 0;
        var runAsyncCalls = 0;
        var writeCalls = 0;
        var application = CreateApplication(
            hierarchyReportBuilder.Object,
            loadingNotifier.Object,
            reportWriter.Object,
            settings);

        hierarchyReportBuilder.Setup(builder => builder.BuildAsync(
                It.Is<EmployeeId>(employeeId => employeeId == settings.RootEmployeeId),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => buildAsyncCalls++)
            .ReturnsAsync(expectedReport);
        loadingNotifier.Setup(notifier => notifier.RunAsync(
                "Connecting to BambooHR...",
                It.IsAny<Func<CancellationToken, Task<HierarchyReport>>>(),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => runAsyncCalls++)
            .Returns((string _, Func<CancellationToken, Task<HierarchyReport>> action, CancellationToken token) => action(token));
        reportWriter.Setup(writer => writer.Write(
                It.Is<HierarchyReport>(report => ReferenceEquals(report, expectedReport))))
            .Callback(() => writeCalls++);

        // Act
        await application.RunAsync(["--verbose"], cts.Token);

        // Assert
        buildAsyncCalls.Should().Be(1);
        runAsyncCalls.Should().Be(1);
        writeCalls.Should().Be(1);
    }

    private static Application CreateApplication(
        IHierarchyReportBuilder hierarchyReportBuilder,
        ILoadingNotifier loadingNotifier,
        IReportWriter reportWriter,
        HierarchyReportSettings settings)
        => new(hierarchyReportBuilder, loadingNotifier, reportWriter, settings);

    private static HierarchyReportSettings CreateSettings()
        => new(
            new EmployeeId(1),
            availabilityLookaheadDays: 7,
            recentHirePeriodDays: 30,
            new Dictionary<string, string[]>());

    private static HierarchyReport CreateReport()
    {
        var availabilityWindow = new AvailabilityWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 8));
        var relationshipField = new HierarchyRelationshipField("reportsTo", "Reports To", usesEmployeeId: false);
        var overview = new HierarchyReportOverview(
            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            availabilityWindow,
            "Alice Smith",
            relationshipField);
        var hierarchy = new HierarchyReportHierarchy([], []);
        var summaries = new HierarchyReportSummaries([], recentHirePeriodDays: 30, []);
        var distributions = new HierarchyReportDistributions(
            new Dictionary<string, int>(),
            new Dictionary<string, IReadOnlyDictionary<string, int>>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>());

        return new HierarchyReport(overview, hierarchy, summaries, distributions);
    }
}
