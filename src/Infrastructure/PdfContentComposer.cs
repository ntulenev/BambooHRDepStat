using System.Globalization;

using Abstractions;

using Models;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure;

/// <summary>
/// Composes QuestPDF content for the hierarchy report.
/// </summary>
public sealed class PdfContentComposer
{
    /// <summary>
    /// Creates PDF content composer.
    /// </summary>
    public PdfContentComposer(IReportPresentationFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatter = formatter;
        _tableFactory = new ReportTableModelFactory(formatter);
    }

    public void ComposeContent(ColumnDescriptor column, HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(report);

        column.Spacing(14);
        column.Item().Row(row =>
        {
            row.Spacing(10);
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "People",
                report.Hierarchy.Rows.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Teams",
                report.Summaries.Teams.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Countries",
                report.Distributions.LocationCounts.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Job Titles",
                _formatter.GetJobTitleCounts(report.Hierarchy.Rows).Count.ToString(CultureInfo.InvariantCulture)));
        });

        column.Item().Element(container => ComposeTableSection(
            container,
            _formatter.BuildHolidaySectionTitle(report.Overview.AvailabilityWindow),
            _tableFactory.CreateHolidaysTable(report.Hierarchy.Holidays)));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Hierarchy",
            _tableFactory.CreateHierarchyTable(
                report.Hierarchy.Rows,
                report.Overview.GeneratedAt)));
        column.Item().Element(container => ComposeTableSection(
            container,
            _formatter.BuildRecentHireSectionTitle(report.Summaries.RecentHirePeriodDays),
            _tableFactory.CreateRecentHiresTable(
                report.Summaries.RecentHires,
                report.Overview.GeneratedAt)));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Job Titles",
            _tableFactory.CreateJobTitlesTable(report.Hierarchy.Rows)));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Teams",
            _tableFactory.CreateTeamsTable(report.Summaries.Teams)));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Location Distribution",
            _tableFactory.CreateCountTable(
                report.Distributions.LocationCounts,
                "No data.")));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Location By Country Cities",
            _tableFactory.CreateCountryCityTable(report.Distributions.CountryCityCounts)));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Age Distribution",
            _tableFactory.CreateCountTable(
                report.Distributions.AgeCounts,
                "No data.")));
        column.Item().Element(container => ComposeTableSection(
            container,
            "Company Tenure Distribution",
            _tableFactory.CreateCountTable(
                report.Distributions.TenureCounts,
                "No data.")));
        if (report.ShowTeamReports)
        {
            column.Item().Element(container => ComposeFlatTeamReportsSection(
                container,
                report.Summaries.Teams,
                report.Overview.GeneratedAt));
        }
    }

    private static void ComposeStatCard(
        IContainer container,
        string label,
        string value)
    {
        container
            .Border(1)
            .BorderColor("#D7D1C2")
            .Background("#F8F5EE")
            .CornerRadius(8)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(2);
                _ = column.Item().Text(label).FontSize(8).FontColor("#6B7467");
                _ = column.Item().Text(value).Bold().FontSize(16).FontColor("#1F2A21");
            });
    }

    private static void ComposeTableSection(
        IContainer container,
        string title,
        ReportTableModel table)
    {
        ComposeSection(container, title, content =>
        {
            if (table.Rows.Count == 0)
            {
                _ = content.Text(table.EmptyText).Italic().FontColor("#6B7467");
                return;
            }

            ComposeTable(content, table);
        });
    }

    private void ComposeFlatTeamReportsSection(
        IContainer container,
        IReadOnlyList<HierarchyTeam> teams,
        DateTimeOffset generatedAt)
    {
        ComposeSection(container, "Flat Team Reports", content =>
        {
            if (teams.Count == 0)
            {
                _ = content.Text("No teams found.").Italic().FontColor("#6B7467");
                return;
            }

            content.Column(column =>
            {
                column.Spacing(12);

                foreach (var team in teams)
                {
                    _ = column.Item().Text($"{team.ManagerDisplayName}'s Team")
                        .Bold()
                        .FontSize(10)
                        .FontColor("#435343");
                    var table = _tableFactory.CreateFlatTeamTable(team.Rows, generatedAt);
                    column.Item().Element(teamContainer => ComposeTable(teamContainer, table));
                }
            });
        });
    }

    private static void ComposeTable(
        IContainer container,
        ReportTableModel model)
    {
        container.Table(descriptor =>
        {
            descriptor.ColumnsDefinition(columns =>
            {
                foreach (var column in model.Columns)
                {
                    if (column.IsConstantWidth)
                    {
                        columns.ConstantColumn(column.Width);
                        continue;
                    }

                    columns.RelativeColumn(column.Width);
                }
            });

            descriptor.Header(header =>
            {
                foreach (var column in model.Columns)
                {
                    ComposeHeaderCell(header.Cell(), column.Header);
                }
            });

            foreach (var row in model.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    ComposeBodyCell(descriptor.Cell(), cell);
                }
            }
        });
    }

    private static void ComposeSection(
        IContainer container,
        string title,
        Action<IContainer> composeBody)
    {
        container
            .Border(1)
            .BorderColor("#D7D1C2")
            .Background("#FFFDF8")
            .CornerRadius(10)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);
                _ = column.Item().Text(title).Bold().FontSize(13).FontColor("#2F6B4F");
                composeBody(column.Item());
            });
    }

    private static void ComposeHeaderCell(IContainer container, string text)
    {
        _ = container
            .Background("#E8F0E4")
            .BorderBottom(1)
            .BorderColor("#D7D1C2")
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .Text(text)
            .Bold()
            .FontSize(8)
            .FontColor("#435343");
    }

    private static void ComposeBodyCell(
        IContainer container,
        string text,
        float leftPadding = 0)
    {
        _ = container
            .BorderBottom(1)
            .BorderColor("#ECE7DA")
            .PaddingTop(5)
            .PaddingBottom(5)
            .PaddingLeft(8 + leftPadding)
            .PaddingRight(8)
            .Text(text)
            .FontSize(8.5f)
            .FontColor("#1F2A21");
    }

    private static void ComposeBodyCell(
        IContainer container,
        ReportTableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        ComposeBodyCell(
            container,
            BuildCellText(cell),
            leftPadding: cell.IndentLevel * 8f);
    }

    private static string BuildCellText(ReportTableCell cell)
    {
        return cell.SecondaryText is null
            ? cell.Text
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{cell.Text} {cell.SecondaryText}");
    }

    private readonly IReportPresentationFormatter _formatter;
    private readonly ReportTableModelFactory _tableFactory;
}
