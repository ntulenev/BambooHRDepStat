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

        column.Item().Element(container => ComposeHolidaysSection(container, report.Overview.AvailabilityWindow, report.Hierarchy.Holidays));
        column.Item().Element(container => ComposeHierarchySection(container, report.Hierarchy.Rows, report.Overview.GeneratedAt));
        column.Item().Element(container => ComposeRecentHiresSection(container, report.Summaries.RecentHires, report.Summaries.RecentHirePeriodDays, report.Overview.GeneratedAt));
        column.Item().Element(container => ComposeJobTitlesSection(container, report.Hierarchy.Rows));
        column.Item().Element(container => ComposeTeamsSection(container, report.Summaries.Teams));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Location Distribution",
            _formatter.OrderCounts(report.Distributions.LocationCounts)));
        column.Item().Element(container => ComposeCountryCitySection(container, report.Distributions.CountryCityCounts));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Age Distribution",
            _formatter.OrderCounts(report.Distributions.AgeCounts)));
        column.Item().Element(container => ComposeDistributionSection(
            container,
            "Company Tenure Distribution",
            _formatter.OrderCounts(report.Distributions.TenureCounts)));
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

    private void ComposeHierarchySection(
        IContainer container,
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ComposeSection(container, "Hierarchy", content =>
        {
            content.Table(table =>
            {
                var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.6f);
                });

                table.Header(header =>
                {
                    ComposeHeaderCell(header.Cell(), "Employee");
                    ComposeHeaderCell(header.Cell(), "Department");
                    ComposeHeaderCell(header.Cell(), "Team");
                    ComposeHeaderCell(header.Cell(), "Job Title");
                    ComposeHeaderCell(header.Cell(), "Location");
                    ComposeHeaderCell(header.Cell(), "Birth");
                    ComposeHeaderCell(header.Cell(), "Age");
                    ComposeHeaderCell(header.Cell(), "Start");
                    ComposeHeaderCell(header.Cell(), "Manager");
                    ComposeHeaderCell(header.Cell(), "Availability");
                });

                foreach (var row in rows)
                {
                    ComposeBodyCell(
                        table.Cell(),
                        _formatter.BuildHierarchyDisplayName(row),
                        leftPadding: row.Level * 8f);
                    ComposeBodyCell(table.Cell(), row.Department ?? "-");
                    ComposeBodyCell(table.Cell(), row.Team ?? "-");
                    ComposeBodyCell(table.Cell(), row.JobTitle ?? "-");
                    ComposeBodyCell(table.Cell(), row.Location ?? "-");
                    ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.DateOfBirth));
                    ComposeBodyCell(table.Cell(), _formatter.FormatAge(row.DateOfBirth, referenceDate));
                    ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.EmploymentStartDate));
                    ComposeBodyCell(table.Cell(), row.ManagerName ?? "-");
                    ComposeBodyCell(
                        table.Cell(),
                        _formatter.FormatAvailability(
                            row.UnavailabilityEntries,
                            referenceDate));
                }
            });
        });
    }

    private void ComposeHolidaysSection(
        IContainer container,
        AvailabilityWindow availabilityWindow,
        IReadOnlyList<HolidayReportItem> holidays)
    {
        ComposeSection(
            container,
            _formatter.BuildHolidaySectionTitle(availabilityWindow),
            content =>
            {
                if (holidays.Count == 0)
                {
                    _ = content.Text("No holidays found in the availability window.")
                        .Italic()
                        .FontColor("#6B7467");
                    return;
                }

                content.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        ComposeHeaderCell(header.Cell(), "Holiday");
                        ComposeHeaderCell(header.Cell(), "Start");
                        ComposeHeaderCell(header.Cell(), "End");
                        ComposeHeaderCell(header.Cell(), "Countries");
                    });

                    foreach (var holiday in holidays)
                    {
                        ComposeBodyCell(table.Cell(), holiday.Name);
                        ComposeBodyCell(table.Cell(), _formatter.FormatDate(holiday.Start));
                        ComposeBodyCell(table.Cell(), _formatter.FormatDate(holiday.End));
                        ComposeBodyCell(
                            table.Cell(),
                            _formatter.FormatAssociatedCountries(holiday.AssociatedCountries));
                    }
                });
            });
    }

    private void ComposeJobTitlesSection(
        IContainer container,
        IReadOnlyList<HierarchyReportRow> rows)
    {
        ComposeDistributionSection(
            container,
            "Job Titles",
            _formatter.GetJobTitleCounts(rows));
    }

    private void ComposeTeamsSection(
        IContainer container,
        IReadOnlyList<HierarchyTeam> teams)
    {
        ComposeSection(container, "Teams", content =>
        {
            if (teams.Count == 0)
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

                foreach (var team in teams)
                {
                    ComposeBodyCell(table.Cell(), team.ManagerDisplayName + "'s Team");
                    ComposeBodyCell(table.Cell(), team.PeopleCount.ToString(CultureInfo.InvariantCulture));
                    ComposeBodyCell(table.Cell(), string.Join(", ", team.MemberDisplayNames));
                    ComposeBodyCell(table.Cell(), _formatter.FormatGradeCounts(team.GradeCounts));
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

    private void ComposeCountryCitySection(
        IContainer container,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts)
    {
        ComposeSection(container, "Location By Country Cities", content =>
        {
            var rows = countryCityCounts
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .SelectMany(country => _formatter.OrderCounts(country.Value)
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

    private void ComposeRecentHiresSection(
        IContainer container,
        IReadOnlyList<HierarchyReportRow> rows,
        int recentHirePeriodDays,
        DateTimeOffset generatedAt)
    {
        ComposeSection(
            container,
            _formatter.BuildRecentHireSectionTitle(recentHirePeriodDays),
            content =>
            {
                if (rows.Count == 0)
                {
                    _ = content.Text("No employees joined during the configured period.")
                        .Italic()
                        .FontColor("#6B7467");
                    return;
                }

                content.Table(table =>
                {
                    var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(0.7f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.1f);
                    });

                    table.Header(header =>
                    {
                        ComposeHeaderCell(header.Cell(), "Employee");
                        ComposeHeaderCell(header.Cell(), "Department");
                        ComposeHeaderCell(header.Cell(), "Team");
                        ComposeHeaderCell(header.Cell(), "Job Title");
                        ComposeHeaderCell(header.Cell(), "Location");
                        ComposeHeaderCell(header.Cell(), "Birth");
                        ComposeHeaderCell(header.Cell(), "Age");
                        ComposeHeaderCell(header.Cell(), "Start");
                        ComposeHeaderCell(header.Cell(), "Days With Us");
                        ComposeHeaderCell(header.Cell(), "Manager");
                    });

                    foreach (var row in rows)
                    {
                        ComposeBodyCell(
                            table.Cell(),
                            $"{row.DisplayName} (#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})");
                        ComposeBodyCell(table.Cell(), row.Department ?? "-");
                        ComposeBodyCell(table.Cell(), row.Team ?? "-");
                        ComposeBodyCell(table.Cell(), row.JobTitle ?? "-");
                        ComposeBodyCell(table.Cell(), row.Location ?? "-");
                        ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.DateOfBirth));
                        ComposeBodyCell(table.Cell(), _formatter.FormatAge(row.DateOfBirth, referenceDate));
                        ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.EmploymentStartDate));
                        ComposeBodyCell(
                            table.Cell(),
                            _formatter.FormatDaysWithUs(
                                row.EmploymentStartDate,
                                referenceDate));
                        ComposeBodyCell(table.Cell(), row.ManagerName ?? "-");
                    }
                });
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
                    column.Item().Element(teamContainer =>
                        ComposeFlatTeamTable(teamContainer, team.Rows, generatedAt));
                }
            });
        });
    }

    private void ComposeFlatTeamTable(
        IContainer container,
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        container.Table(table =>
        {
            var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                ComposeHeaderCell(header.Cell(), "Employee");
                ComposeHeaderCell(header.Cell(), "Department");
                ComposeHeaderCell(header.Cell(), "Team");
                ComposeHeaderCell(header.Cell(), "Job Title");
                ComposeHeaderCell(header.Cell(), "Location");
                ComposeHeaderCell(header.Cell(), "Birth");
                ComposeHeaderCell(header.Cell(), "Age");
                ComposeHeaderCell(header.Cell(), "Start");
                ComposeHeaderCell(header.Cell(), "Manager");
                ComposeHeaderCell(header.Cell(), "Availability");
            });

            foreach (var row in rows)
            {
                ComposeBodyCell(
                    table.Cell(),
                    $"{row.DisplayName} (#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})");
                ComposeBodyCell(table.Cell(), row.Department ?? "-");
                ComposeBodyCell(table.Cell(), row.Team ?? "-");
                ComposeBodyCell(table.Cell(), row.JobTitle ?? "-");
                ComposeBodyCell(table.Cell(), row.Location ?? "-");
                ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.DateOfBirth));
                ComposeBodyCell(table.Cell(), _formatter.FormatAge(row.DateOfBirth, referenceDate));
                ComposeBodyCell(table.Cell(), _formatter.FormatDate(row.EmploymentStartDate));
                ComposeBodyCell(table.Cell(), row.ManagerName ?? "-");
                ComposeBodyCell(
                    table.Cell(),
                    _formatter.FormatAvailability(row.UnavailabilityEntries, referenceDate));
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

    private readonly IReportPresentationFormatter _formatter;
}
