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
        html.Should().Contain("Reports To (employee name)");
        html.Should().NotContain("__ROOT_EMPLOYEE__");
    }
}
