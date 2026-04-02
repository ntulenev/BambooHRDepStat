using FluentAssertions;

using Models;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure.Tests;

[Collection("Pdf stack")]
public sealed class PdfContentComposerTests
{
    [Fact(DisplayName = "The PDF composer renders a document for the populated report fixture.")]
    [Trait("Category", "Unit")]
    public void ComposeShouldRenderPdfForPopulatedReport()
    {
        // Arrange
        var report = ReportTestData.CreateReport();

        // Act
        var pdfBytes = RenderPdf(report);

        // Assert
        pdfBytes.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "The PDF composer renders a document for an empty report without throwing.")]
    [Trait("Category", "Unit")]
    public void ComposeShouldRenderPdfForEmptyReport()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var pdfBytes = RenderPdf(report);

        // Assert
        pdfBytes.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "The PDF composer skips country buckets that have no city rows.")]
    [Trait("Category", "Unit")]
    public void ComposeShouldRenderPdfWhenCountryHasNoCityRows()
    {
        // Arrange
        var report = CreateReportWithEmptyCountryCityBucket();

        // Act
        var pdfBytes = RenderPdf(report);

        // Assert
        pdfBytes.Should().NotBeNullOrEmpty();
    }

    private static byte[] RenderPdf(HierarchyReport report)
    {
        EnsureQuestPdfLicense();

        var composer = new PdfContentComposer(new ReportPresentationFormatter());
        var document = Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                page.Content().Column(column => composer.ComposeContent(column, report));
            });
        });

        return document.GeneratePdf();
    }

    private static void EnsureQuestPdfLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static HierarchyReport CreateEmptyReport()
    {
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 7));
        var relationshipField = new HierarchyRelationshipField(
            "reportsTo",
            "Reports To",
            usesEmployeeId: false);

        return new HierarchyReport(
            new HierarchyReportOverview(
                new DateTimeOffset(2026, 4, 1, 9, 30, 0, TimeSpan.Zero),
                availabilityWindow,
                "Alice Smith",
                relationshipField),
            new HierarchyReportHierarchy([], []),
            new HierarchyReportSummaries([], recentHirePeriodDays: 30, []),
            new HierarchyReportDistributions(
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)));
    }

    private static HierarchyReport CreateReportWithEmptyCountryCityBucket()
    {
        var report = CreateEmptyReport();
        var countryCityCounts = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        };

        return new HierarchyReport(
            report.Overview,
            report.Hierarchy,
            report.Summaries,
            new HierarchyReportDistributions(
                report.Distributions.LocationCounts,
                countryCityCounts,
                report.Distributions.AgeCounts,
                report.Distributions.TenureCounts));
    }
}
