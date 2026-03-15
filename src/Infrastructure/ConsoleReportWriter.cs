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

    /// <inheritdoc/>
    public void Render(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AnsiConsole.MarkupLine(
            $"[bold]BambooHR hierarchy report[/] for [green]{Escape(report.RootEmployeeName)}[/]");
        AnsiConsole.MarkupLine(
            $"Generated: [grey]{report.GeneratedAt:yyyy-MM-dd HH:mm:ss zzz}[/]");
        AnsiConsole.MarkupLine(
            $"Work week: [yellow]{report.WorkWeek.Start:yyyy-MM-dd}[/] to [yellow]{report.WorkWeek.End:yyyy-MM-dd}[/]");
        var relationshipMode = report.RelationshipField.UsesEmployeeId
            ? "employee ID"
            : "employee name";
        AnsiConsole.MarkupLine(
            $"Hierarchy link: [blue]{Escape(report.RelationshipField.DisplayName)}[/] ({relationshipMode})");
        AnsiConsole.WriteLine();

        var rootRow = report.Rows.Count == 0
            ? throw new InvalidOperationException("Hierarchy report is empty.")
            : report.Rows[0];
        var tree = new Tree("[grey]Hierarchy[/]")
        {
            Guide = TreeGuide.Ascii
        };
        var rootNode = tree.AddNode(FormatNode(rootRow));

        var nodeStack = new Stack<TreeNode>();
        nodeStack.Push(rootNode);

        foreach (var row in report.Rows.Skip(1))
        {
            while (nodeStack.Count > row.Level)
            {
                _ = nodeStack.Pop();
            }

            var parentNode = nodeStack.Peek();
            var childNode = parentNode.AddNode(FormatNode(row));
            nodeStack.Push(childNode);
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
        WriteJobTitles(report);
        AnsiConsole.WriteLine();
        WriteTeams(report);
        AnsiConsole.WriteLine();
        WriteTeamSizeChart(report);
        AnsiConsole.WriteLine();
        WriteTeamGradeCharts(report);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Location Distribution", report.LocationCounts);
        AnsiConsole.WriteLine();
        WriteCountryCityDistribution(report);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Age Distribution", report.AgeCounts);
        AnsiConsole.WriteLine();
        WriteDistributionSection("Company Tenure Distribution", report.TenureCounts);
    }

    private static string FormatUnavailability(IReadOnlyList<TimeOffEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries.Count == 0
            ? "[green]Available[/]"
            : string.Join(
                Environment.NewLine,
                entries.Select(FormatEntry));
    }

    private static string FormatNode(HierarchyReportRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        var details = $"{row.EmployeeId} | {row.Department ?? "-"} | {row.JobTitle ?? "-"}";
        var location = $"{Environment.NewLine}[grey]Location: {Escape(row.Location ?? "-")}[/]";
        var birthDate = $"{Environment.NewLine}[grey]Birth date: {FormatDate(row.DateOfBirth)}[/]";
        var employmentStart = $"{Environment.NewLine}[grey]Employment start: {FormatDate(row.EmploymentStartDate)}[/]";
        var manager = row.ManagerName is null ? string.Empty : $"{Environment.NewLine}[grey]Reports to: {Escape(row.ManagerName)}[/]";

        return $"[green]{Escape(row.DisplayName)}[/] [grey](#{row.EmployeeId})[/]"
            + $"{Environment.NewLine}[grey]{Escape(details)}[/]"
            + location
            + birthDate
            + employmentStart
            + $"{Environment.NewLine}{FormatUnavailability(row.UnavailabilityEntries)}"
            + manager;
    }

    private static void WriteJobTitles(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var table = new Table().Border(TableBorder.Rounded);
        _ = table.AddColumn("Job Title");
        _ = table.AddColumn("People");

        var groupedTitles = report.Rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.JobTitle) ? "(No title)" : row.JobTitle)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedTitles)
        {
            _ = table.AddRow(
                Escape(group.Key ?? "(No title)"),
                group.Count().ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AnsiConsole.MarkupLine("[bold]Job Titles[/]");
        AnsiConsole.Write(table);
    }

    private static void WriteTeams(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AnsiConsole.MarkupLine("[bold]Teams[/]");

        if (report.Teams.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No teams found.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        _ = table.AddColumn("Team");
        _ = table.AddColumn("People");
        _ = table.AddColumn("Leaf Members");

        foreach (var team in report.Teams)
        {
            _ = table.AddRow(
                $"{Escape(team.ManagerDisplayName)}'s Team",
                team.PeopleCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                string.Join(", ", team.MemberDisplayNames.Select(Escape)));
        }

        AnsiConsole.Write(table);
    }

    private static void WriteTeamSizeChart(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Teams.Count == 0)
        {
            return;
        }

        var chart = new BarChart()
            .Width(80)
            .Label("[bold]Team Size[/]")
            .CenterLabel()
            .ShowValues();

        for (var index = 0; index < report.Teams.Count; index++)
        {
            var team = report.Teams[index];
            _ = chart.AddItem(
                $"{team.ManagerDisplayName}'s Team",
                team.PeopleCount,
                TeamPalette[index % TeamPalette.Length]);
        }

        AnsiConsole.Write(chart);
    }

    private static void WriteTeamGradeCharts(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Teams.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Team Grade Distribution[/]");

        foreach (var team in report.Teams)
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

    private static void WriteDistributionSection(
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

        var table = new Table().Border(TableBorder.Rounded);
        _ = table.AddColumn("Bucket");
        _ = table.AddColumn("People");

        foreach (var item in orderedCounts)
        {
            _ = table.AddRow(
                Escape(item.Key),
                item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(table);

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

    private static void WriteCountryCityDistribution(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.CountryCityCounts.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Location Distribution By Country Cities[/]");

        foreach (var country in report.CountryCityCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
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

    private static string FormatEntry(TimeOffEntry entry)
    {
        var label = entry.Type == TimeOffEntryType.Holiday
            ? $"Holiday ({Escape(entry.Name)})"
            : "Time off";

        return $"{label}: {entry.Start:yyyy-MM-dd} - {entry.End:yyyy-MM-dd}";
    }

    private static string FormatDate(DateOnly? date)
    {
        return ReportPresentationFormatter.FormatDate(date);
    }

    private static string Escape(string value) => Markup.Escape(value);
}
