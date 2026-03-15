using System.Globalization;

using Models;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Infrastructure;

/// <summary>
/// Composes QuestPDF content for the hierarchy report.
/// </summary>
public sealed class PdfContentComposer
{
    private readonly int _instanceMarker = 1;

    public void ComposeContent(ColumnDescriptor column, HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(report);
        _ = _instanceMarker;

        column.Spacing(14);
        column.Item().Row(row =>
        {
            row.Spacing(10);
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "People",
                report.Rows.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Teams",
                report.Teams.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Countries",
                report.LocationCounts.Count.ToString(CultureInfo.InvariantCulture)));
            row.RelativeItem().Element(container => ComposeStatCard(
                container,
                "Job Titles",
                ReportPresentationFormatter.GetJobTitleCounts(report).Count.ToString(CultureInfo.InvariantCulture)));
        });

        column.Item().Element(container => ComposeHierarchySection(container, report));
        column.Item().Element(container => ComposeJobTitlesSection(container, report));
        column.Item().Element(container => ComposeTeamsSection(container, report));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Location Distribution",
            ReportPresentationFormatter.OrderCounts(report.LocationCounts)));
        column.Item().Element(container => ComposeCountryCitySection(container, report.CountryCityCounts));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Age Distribution",
            ReportPresentationFormatter.OrderCounts(report.AgeCounts)));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Company Tenure Distribution",
            ReportPresentationFormatter.OrderCounts(report.TenureCounts)));
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

    private static void ComposeHierarchySection(
        IContainer container,
        HierarchyReport report)
    {
        ComposeSection(container, "Hierarchy", content =>
        {
            content.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.4f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    ComposeHeaderCell(header.Cell(), "Employee");
                    ComposeHeaderCell(header.Cell(), "Department");
                    ComposeHeaderCell(header.Cell(), "Job Title");
                    ComposeHeaderCell(header.Cell(), "Location");
                    ComposeHeaderCell(header.Cell(), "Birth");
                    ComposeHeaderCell(header.Cell(), "Start");
                    ComposeHeaderCell(header.Cell(), "Manager");
                    ComposeHeaderCell(header.Cell(), "Availability");
                });

                foreach (var row in report.Rows)
                {
                    ComposeBodyCell(
                        table.Cell(),
                        ReportPresentationFormatter.BuildHierarchyDisplayName(row),
                        leftPadding: row.Level * 8f);
                    ComposeBodyCell(table.Cell(), row.Department ?? "-");
                    ComposeBodyCell(table.Cell(), row.JobTitle ?? "-");
                    ComposeBodyCell(table.Cell(), row.Location ?? "-");
                    ComposeBodyCell(table.Cell(), ReportPresentationFormatter.FormatDate(row.DateOfBirth));
                    ComposeBodyCell(table.Cell(), ReportPresentationFormatter.FormatDate(row.EmploymentStartDate));
                    ComposeBodyCell(table.Cell(), row.ManagerName ?? "-");
                    ComposeBodyCell(table.Cell(), ReportPresentationFormatter.FormatAvailability(row.UnavailabilityEntries));
                }
            });
        });
    }

    private static void ComposeJobTitlesSection(
        IContainer container,
        HierarchyReport report)
    {
        ComposeDistributionSection(
            container,
            "Job Titles",
            ReportPresentationFormatter.GetJobTitleCounts(report));
    }

    private static void ComposeTeamsSection(
        IContainer container,
        HierarchyReport report)
    {
        ComposeSection(container, "Teams", content =>
        {
            if (report.Teams.Count == 0)
            {
                _ = content.Text("No teams found.").Italic().FontColor("#6B7467");
                return;
            }

            content.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.6f);
                    columns.ConstantColumn(54);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2f);
                });

                table.Header(header =>
                {
                    ComposeHeaderCell(header.Cell(), "Team");
                    ComposeHeaderCell(header.Cell(), "People");
                    ComposeHeaderCell(header.Cell(), "Members");
                    ComposeHeaderCell(header.Cell(), "Grades");
                });

                foreach (var team in report.Teams)
                {
                    ComposeBodyCell(table.Cell(), team.ManagerDisplayName + "'s Team");
                    ComposeBodyCell(table.Cell(), team.PeopleCount.ToString(CultureInfo.InvariantCulture));
                    ComposeBodyCell(table.Cell(), string.Join(", ", team.MemberDisplayNames));
                    ComposeBodyCell(table.Cell(), ReportPresentationFormatter.FormatGradeCounts(team.GradeCounts));
                }
            });
        });
    }

    private static void ComposeDistributionSection(
        IContainer container,
        string title,
        IReadOnlyList<KeyValuePair<string, int>> counts)
    {
        ComposeSection(container, title, content =>
        {
            if (counts.Count == 0)
            {
                _ = content.Text("No data.").Italic().FontColor("#6B7467");
                return;
            }

            content.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3f);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    ComposeHeaderCell(header.Cell(), "Bucket");
                    ComposeHeaderCell(header.Cell(), "People");
                });

                foreach (var count in counts)
                {
                    ComposeBodyCell(table.Cell(), count.Key);
                    ComposeBodyCell(table.Cell(), count.Value.ToString(CultureInfo.InvariantCulture));
                }
            });
        });
    }

    private static void ComposeCountryCitySection(
        IContainer container,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts)
    {
        ComposeSection(container, "Location By Country Cities", content =>
        {
            var rows = countryCityCounts
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .SelectMany(country => ReportPresentationFormatter.OrderCounts(country.Value)
                    .Select(city => new
                    {
                        Country = country.Key,
                        City = city.Key,
                        People = city.Value
                    }))
                .ToArray();

            if (rows.Length == 0)
            {
                _ = content.Text("No city breakdown available.").Italic().FontColor("#6B7467");
                return;
            }

            content.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.7f);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    ComposeHeaderCell(header.Cell(), "Country");
                    ComposeHeaderCell(header.Cell(), "City");
                    ComposeHeaderCell(header.Cell(), "People");
                });

                foreach (var row in rows)
                {
                    ComposeBodyCell(table.Cell(), row.Country);
                    ComposeBodyCell(table.Cell(), row.City);
                    ComposeBodyCell(table.Cell(), row.People.ToString(CultureInfo.InvariantCulture));
                }
            });
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
}
