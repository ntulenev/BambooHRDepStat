using FluentAssertions;

namespace Infrastructure.Tests;

public sealed class HtmlContentComposerTests
{
    [Fact(DisplayName = "The HTML composer renders the embedded template with escaped report values.")]
    [Trait("Category", "Unit")]
    public void ComposeShouldRenderTemplateWithEscapedValues()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        var composer = new HtmlContentComposer(formatter);
        var report = ReportTestData.CreateReport();

        // Act
        var html = composer.Compose(report);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("BambooHR Hierarchy Report");
        html.Should().Contain("Alice &amp; Smith");
        html.Should().Contain("Founders Day");
        html.Should().Contain("Bob Jones");
        html.Should().Contain("ADF Processing Team");
        html.Should().Contain("Mobile Phone: +49 111 1111");
        html.Should().Contain("Work Phone: +49 333 3333");
        html.Should().Contain("Flat Team Reports");
        html.Should().Contain("Alice &amp; Smith's Team");
        html.Should().Contain("Reports To (employee name)");
        html.Should().NotContain("__ROOT_EMPLOYEE__");
    }

    [Fact(DisplayName = "The HTML composer hides flat team report sections when they are disabled in the report.")]
    [Trait("Category", "Unit")]
    public void ComposeShouldHideFlatTeamReportsWhenDisabled()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        var composer = new HtmlContentComposer(formatter);
        var report = CreateReport(showTeamReports: false);

        // Act
        var html = composer.Compose(report);

        // Assert
        html.Should().NotContain("Flat Team Reports");
        html.Should().NotContain("Alice &amp; Smith's Team");
    }

    private static Models.HierarchyReport CreateReport(bool showTeamReports)
    {
        var report = ReportTestData.CreateReport();
        return new Models.HierarchyReport(
            report.Overview,
            report.Hierarchy,
            new Models.HierarchyReportSummaries(
                report.Summaries.RecentHires,
                report.Summaries.RecentHirePeriodDays,
                report.Summaries.Teams,
                showTeamReports),
            report.Distributions);
    }
}
