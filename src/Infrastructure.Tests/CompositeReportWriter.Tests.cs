using Abstractions;

using FluentAssertions;

using Moq;

using Models;

namespace Infrastructure.Tests;

public sealed class CompositeReportWriterTests
{
    [Fact(DisplayName = "The constructor throws when the console report renderer is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenConsoleReportRendererIsNull()
    {
        // Arrange
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;

        // Act
        var action = () => new CompositeReportWriter(null!, htmlReportRenderer, pdfReportRenderer);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the HTML report renderer is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenHtmlReportRendererIsNull()
    {
        // Arrange
        var consoleReportRenderer = new Mock<IConsoleReportRenderer>(MockBehavior.Strict).Object;
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;

        // Act
        var action = () => new CompositeReportWriter(consoleReportRenderer, null!, pdfReportRenderer);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the PDF report renderer is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenPdfReportRendererIsNull()
    {
        // Arrange
        var consoleReportRenderer = new Mock<IConsoleReportRenderer>(MockBehavior.Strict).Object;
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;

        // Act
        var action = () => new CompositeReportWriter(consoleReportRenderer, htmlReportRenderer, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The composite report writer forwards the report to the console renderer.")]
    [Trait("Category", "Unit")]
    public void WriteShouldRenderReportThroughConsoleOutput()
    {
        // Arrange
        var report = CreateReport();
        var consoleReportRenderer = new Mock<IConsoleReportRenderer>(MockBehavior.Strict);
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict);
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        var consoleRenderCalls = 0;

        consoleReportRenderer.Setup(renderer => renderer.Render(report))
            .Callback(() => consoleRenderCalls++);
        htmlReportRenderer.Setup(renderer => renderer.Render(report));
        pdfReportRenderer.Setup(renderer => renderer.Render(report));

        var writer = new CompositeReportWriter(
            consoleReportRenderer.Object,
            htmlReportRenderer.Object,
            pdfReportRenderer.Object);

        // Act
        writer.Write(report);

        // Assert
        consoleRenderCalls.Should().Be(1);
    }

    [Fact(DisplayName = "The composite report writer forwards the report to the HTML renderer.")]
    [Trait("Category", "Unit")]
    public void WriteShouldRenderReportThroughHtmlOutput()
    {
        // Arrange
        var report = CreateReport();
        var consoleReportRenderer = new Mock<IConsoleReportRenderer>(MockBehavior.Strict);
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict);
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        var htmlRenderCalls = 0;

        consoleReportRenderer.Setup(renderer => renderer.Render(report));
        htmlReportRenderer.Setup(renderer => renderer.Render(report))
            .Callback(() => htmlRenderCalls++);
        pdfReportRenderer.Setup(renderer => renderer.Render(report));

        var writer = new CompositeReportWriter(
            consoleReportRenderer.Object,
            htmlReportRenderer.Object,
            pdfReportRenderer.Object);

        // Act
        writer.Write(report);

        // Assert
        htmlRenderCalls.Should().Be(1);
    }

    [Fact(DisplayName = "The composite report writer forwards the report to the PDF renderer.")]
    [Trait("Category", "Unit")]
    public void WriteShouldRenderReportThroughPdfOutput()
    {
        // Arrange
        var report = CreateReport();
        var consoleReportRenderer = new Mock<IConsoleReportRenderer>(MockBehavior.Strict);
        var htmlReportRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict);
        var pdfReportRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        var pdfRenderCalls = 0;

        consoleReportRenderer.Setup(renderer => renderer.Render(report));
        htmlReportRenderer.Setup(renderer => renderer.Render(report));
        pdfReportRenderer.Setup(renderer => renderer.Render(report))
            .Callback(() => pdfRenderCalls++);

        var writer = new CompositeReportWriter(
            consoleReportRenderer.Object,
            htmlReportRenderer.Object,
            pdfReportRenderer.Object);

        // Act
        writer.Write(report);

        // Assert
        pdfRenderCalls.Should().Be(1);
    }

    private static HierarchyReport CreateReport()
        => new(
            new HierarchyReportOverview(
                new DateTimeOffset(2026, 4, 1, 8, 0, 0, TimeSpan.Zero),
                new AvailabilityWindow(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 8)),
                "Alice Smith",
                new HierarchyRelationshipField("reportsTo", "Reports To", usesEmployeeId: false)),
            new HierarchyReportHierarchy([], []),
            new HierarchyReportSummaries([], 30, []),
            new HierarchyReportDistributions(
                new Dictionary<string, int>(),
                new Dictionary<string, IReadOnlyDictionary<string, int>>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>()));
}
