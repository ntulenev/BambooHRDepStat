using Abstractions;

using Models;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// Writes report using Spectre.Console tree rendering.
/// </summary>
public sealed class ConsoleReportWriter : IReportWriter
{
    /// <inheritdoc/>
    public void Write(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        AnsiConsole.MarkupLine(
            $"[bold]BambooHR hierarchy report[/] for [green]{Escape(report.RootEmployeeName)}[/]");
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
        var manager = row.ManagerName is null ? string.Empty : $"{Environment.NewLine}[grey]Reports to: {Escape(row.ManagerName)}[/]";

        return $"[green]{Escape(row.DisplayName)}[/] [grey](#{row.EmployeeId})[/]"
            + $"{Environment.NewLine}[grey]{Escape(details)}[/]"
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

    private static string FormatEntry(TimeOffEntry entry)
    {
        var label = entry.Type == TimeOffEntryType.Holiday
            ? $"Holiday ({Escape(entry.Name)})"
            : "Time off";

        return $"{label}: {entry.Start:yyyy-MM-dd} - {entry.End:yyyy-MM-dd}";
    }

    private static string Escape(string value) => Markup.Escape(value);
}
