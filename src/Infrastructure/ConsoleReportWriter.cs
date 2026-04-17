using Abstractions;

using Models;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// Writes report using Spectre.Console tree rendering.
/// </summary>
public sealed class ConsoleReportWriter : IConsoleReportRenderer
{
    internal static readonly Color[] TeamPalette =
    [
        Color.Green,
        Color.Blue,
        Color.Orange1,
        Color.MediumPurple,
        Color.DeepPink1,
        Color.Teal,
        Color.Yellow
    ];

    /// <summary>
    /// Creates console report writer.
    /// </summary>
    public ConsoleReportWriter(IReportPresentationFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatter = formatter;
        _tableFactory = new ReportTableModelFactory(formatter);
    }

    /// <inheritdoc/>
    public void Render(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AnsiConsole.MarkupLine(
            $"[bold]BambooHR hierarchy report[/] for [green]{Escape(report.Overview.RootEmployeeName)}[/]");
        AnsiConsole.MarkupLine(
            $"Generated: [grey]{report.Overview.GeneratedAt:yyyy-MM-dd HH:mm:ss zzz}[/]");
        AnsiConsole.MarkupLine(
            $"Availability window: [yellow]{report.Overview.AvailabilityWindow.Start:yyyy-MM-dd}[/] to [yellow]{report.Overview.AvailabilityWindow.End:yyyy-MM-dd}[/]");
        var relationshipMode = report.Overview.RelationshipField.UsesEmployeeId
            ? "employee ID"
            : "employee name";
        AnsiConsole.MarkupLine(
            $"Hierarchy link: [blue]{Escape(report.Overview.RelationshipField.DisplayName)}[/] ({relationshipMode})");
        AnsiConsole.WriteLine();
        WriteHolidays(report.Overview.AvailabilityWindow, report.Hierarchy.Holidays);
        AnsiConsole.WriteLine();

        var rootRow = report.Hierarchy.Rows.Count == 0
            ? throw new InvalidOperationException("Hierarchy report is empty.")
            : report.Hierarchy.Rows[0];
        var tree = new Tree("[grey]Hierarchy[/]")
        {
            Guide = TreeGuide.Ascii
        };
        var referenceDate = DateOnly.FromDateTime(report.Overview.GeneratedAt.Date);
        var rootNode = tree.AddNode(FormatNode(rootRow, referenceDate));

        var nodeStack = new Stack<TreeNode>();
        nodeStack.Push(rootNode);

        foreach (var row in report.Hierarchy.Rows.Skip(1))
        {
            while (nodeStack.Count > row.Level)
            {
                _ = nodeStack.Pop();
            }

            var parentNode = nodeStack.Peek();
            var childNode = parentNode.AddNode(FormatNode(row, referenceDate));
            nodeStack.Push(childNode);
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
        WriteRecentHires(report.Summaries, referenceDate);
        AnsiConsole.WriteLine();
        WriteJobTitles(report.Hierarchy.Rows);
        AnsiConsole.WriteLine();
        WriteTeams(report.Summaries.Teams);
        AnsiConsole.WriteLine();
        WriteTeamSizeChart(report.Summaries.Teams);
        AnsiConsole.WriteLine();
        WriteTeamGradeCharts(report.Summaries.Teams);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Location Distribution", report.Distributions.LocationCounts);
        AnsiConsole.WriteLine();
        WriteCountryCityDistribution(report.Distributions.CountryCityCounts);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Age Distribution", report.Distributions.AgeCounts);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Company Tenure Distribution", report.Distributions.TenureCounts);
        if (report.ShowTeamReports)
        {
            AnsiConsole.WriteLine();
            WriteFlatTeamReports(report.Summaries.Teams, referenceDate);
        }
    }

    private string FormatUnavailability(
        IReadOnlyList<TimeOffEntry> entries,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var text = _formatter.FormatAvailability(entries, referenceDate);
        var color = _formatter.GetAvailabilityState(entries, referenceDate) switch
        {
            ReportAvailabilityState.Available => "green",
            ReportAvailabilityState.Upcoming => "yellow",
            ReportAvailabilityState.UnavailableToday => "red",
            _ => throw new InvalidOperationException("Unknown availability state.")
        };

        return $"[{color}]{Escape(text)}[/]";
    }

    private string FormatNode(HierarchyReportRow row, DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(row);

        var details = $"{row.EmployeeId} | {row.Department ?? "-"} | {row.Team ?? "-"} | {row.JobTitle ?? "-"}";
        var location = $"{Environment.NewLine}[grey]Location: {Escape(row.Location ?? "-")}[/]";
        var birthDate =
            $"{Environment.NewLine}[grey]Birth date: {FormatDate(row.DateOfBirth)} | Age: {Escape(_formatter.FormatAge(row.DateOfBirth, referenceDate))}[/]";
        var employmentStart = $"{Environment.NewLine}[grey]Employment start: {FormatDate(row.EmploymentStartDate)}[/]";
        var manager = row.ManagerName is null ? string.Empty : $"{Environment.NewLine}[grey]Reports to: {Escape(row.ManagerName)}[/]";

        return $"[green]{Escape(row.DisplayName)}[/] [grey](#{row.EmployeeId})[/]"
            + $"{Environment.NewLine}[grey]{Escape(details)}[/]"
            + location
            + birthDate
            + employmentStart
            + $"{Environment.NewLine}{FormatUnavailability(row.UnavailabilityEntries, referenceDate)}"
            + manager;
    }

    private void WriteJobTitles(IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        WriteTable(
            "Job Titles",
            _tableFactory.CreateJobTitlesTable(rows));
    }

    private void WriteHolidays(
        AvailabilityWindow availabilityWindow,
        IReadOnlyList<HolidayReportItem> holidays)
    {
        ArgumentNullException.ThrowIfNull(availabilityWindow);
        ArgumentNullException.ThrowIfNull(holidays);

        AnsiConsole.MarkupLine(
            $"[bold]{Escape(_formatter.BuildHolidaySectionTitle(availabilityWindow))}[/]");

        if (holidays.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No holidays found in the availability window.[/]");
            return;
        }

        AnsiConsole.Write(CreateTable(_tableFactory.CreateHolidaysTable(holidays)));
    }

    private void WriteTeams(IReadOnlyList<HierarchyTeam> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);

        AnsiConsole.MarkupLine("[bold]Teams[/]");

        if (teams.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No teams found.[/]");
            return;
        }

        AnsiConsole.Write(CreateTable(_tableFactory.CreateTeamsTable(teams, "Leaf Members")));
    }

    private static void WriteTeamSizeChart(IReadOnlyList<HierarchyTeam> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (teams.Count == 0)
        {
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("[bold]Team Size[/]")
            .CenterLabel()
            .ShowValues();

        for (var index = 0; index < teams.Count; index++)
        {
            var team = teams[index];
            _ = chart.AddItem(
                $"{team.ManagerDisplayName}'s Team",
                team.PeopleCount,
                TeamPalette[index % TeamPalette.Length]);
        }

        AnsiConsole.Write(chart);
    }

    private static void WriteTeamGradeCharts(IReadOnlyList<HierarchyTeam> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (teams.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Team Grade Distribution[/]");

        foreach (var team in teams)
        {
            var chart = new BreakdownChart()
                .Width(80)
                .FullSize()
                .ShowPercentage()
                .ShowTagValues()
                .UseValueFormatter(value =>
                    value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var grades = team.GradeCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var index = 0; index < grades.Length; index++)
            {
                var grade = grades[index];
                _ = chart.AddItem(
                    Escape(grade.Key),
                    grade.Value,
                    TeamPalette[index % TeamPalette.Length]);
            }

            var panel = new Panel(chart)
                .Header($"{Escape(team.ManagerDisplayName)}'s Team ({team.PeopleCount})")
                .Border(BoxBorder.Rounded);
            AnsiConsole.Write(panel);
        }
    }

    private void WriteDistributionSection(
        string title,
        IReadOnlyDictionary<string, int> counts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(counts);

        AnsiConsole.MarkupLine($"[bold]{Escape(title)}[/]");

        if (counts.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No data.[/]");
            return;
        }

        var orderedCounts = counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        AnsiConsole.Write(CreateTable(_tableFactory.CreateCountTable(counts, "No data.")));

        AnsiConsole.Write(CreateDistributionBarChart(title, orderedCounts));
    }

    private static BarChart CreateDistributionBarChart(
        string title,
        KeyValuePair<string, int>[] counts)
    {
        var chart = new BarChart()
            .Width(38)
            .Label($"[bold]{Escape(title)} Count[/]")
            .CenterLabel()
            .ShowValues();

        for (var index = 0; index < counts.Length; index++)
        {
            var item = counts[index];
            _ = chart.AddItem(
                Escape(item.Key),
                item.Value,
                TeamPalette[index % TeamPalette.Length]);
        }

        return chart;
    }

    private static void WriteCountryCityDistribution(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts)
    {
        ArgumentNullException.ThrowIfNull(countryCityCounts);

        if (countryCityCounts.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Location Distribution By Country Cities[/]");

        foreach (var country in countryCityCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var orderedCities = country.Value
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (orderedCities.Length == 0)
            {
                continue;
            }

            var chart = CreateDistributionBarChart(
                $"{country.Key} Cities",
                orderedCities);
            var panel = new Panel(chart)
                .Header(Escape(country.Key))
                .Border(BoxBorder.Rounded);
            AnsiConsole.Write(panel);
        }
    }

    private void WriteFlatTeamReports(
        IReadOnlyList<HierarchyTeam> teams,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(teams);

        AnsiConsole.MarkupLine("[bold]Flat Team Reports[/]");

        if (teams.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No teams found.[/]");
            return;
        }

        foreach (var team in teams)
        {
            AnsiConsole.MarkupLine($"[bold]{Escape(team.ManagerDisplayName)}'s Team[/]");
            AnsiConsole.Write(CreateTable(_tableFactory.CreateFlatTeamTable(team.Rows, ToReferenceTimestamp(referenceDate))));
            AnsiConsole.WriteLine();
        }
    }

    private void WriteRecentHires(
        HierarchyReportSummaries summaries,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        AnsiConsole.MarkupLine(
            $"[bold]{Escape(_formatter.BuildRecentHireSectionTitle(summaries.RecentHirePeriodDays))}[/]");

        if (summaries.RecentHires.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No employees joined during the configured period.[/]");
            return;
        }

        AnsiConsole.Write(CreateTable(
            _tableFactory.CreateRecentHiresTable(
                summaries.RecentHires,
                ToReferenceTimestamp(referenceDate))));
    }

    private string FormatDate(DateOnly? date)
    {
        return _formatter.FormatDate(date);
    }

    private static void WriteTable(string title, ReportTableModel table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(table);

        AnsiConsole.MarkupLine($"[bold]{Escape(title)}[/]");

        if (table.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine($"[grey]{Escape(table.EmptyText)}[/]");
            return;
        }

        AnsiConsole.Write(CreateTable(table));
    }

    private static Table CreateTable(ReportTableModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var table = new Table().Border(TableBorder.Rounded);
        foreach (var column in model.Columns)
        {
            _ = table.AddColumn(column.Header);
        }

        foreach (var row in model.Rows)
        {
            _ = table.AddRow(row.Cells.Select(BuildCellText).ToArray());
        }

        return table;
    }

    private static string BuildCellText(ReportTableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        return Escape(
            cell.SecondaryText is null
                ? cell.Text
                : $"{cell.Text} {cell.SecondaryText}");
    }

    private static DateTimeOffset ToReferenceTimestamp(DateOnly referenceDate)
    {
        return new DateTimeOffset(referenceDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static string Escape(string value) => Markup.Escape(value);

    private readonly IReportPresentationFormatter _formatter;
    private readonly ReportTableModelFactory _tableFactory;
}
