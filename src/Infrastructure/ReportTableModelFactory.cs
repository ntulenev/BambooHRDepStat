using System.Globalization;

using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// Builds shared table models for report sections reused by multiple renderers.
/// </summary>
internal sealed class ReportTableModelFactory
{
    /// <summary>
    /// Creates a table model factory.
    /// </summary>
    public ReportTableModelFactory(IReportPresentationFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatter = formatter;
    }

    /// <summary>
    /// Creates the holidays table model.
    /// </summary>
    public ReportTableModel CreateHolidaysTable(IReadOnlyList<HolidayReportItem> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        return new ReportTableModel(
            [
                new ReportTableColumn("Holiday", 2f),
                new ReportTableColumn("Start", 1f),
                new ReportTableColumn("End", 1f),
                new ReportTableColumn("Countries", 1.5f)
            ],
            [
                .. holidays.Select(holiday => new ReportTableRow(
                    [
                        new ReportTableCell(holiday.Name),
                        new ReportTableCell(_formatter.FormatDate(holiday.Start)),
                        new ReportTableCell(_formatter.FormatDate(holiday.End)),
                        new ReportTableCell(_formatter.FormatAssociatedCountries(holiday.AssociatedCountries))
                    ]))
            ],
            "No holidays found in the availability window.");
    }

    /// <summary>
    /// Creates the hierarchy table model.
    /// </summary>
    public ReportTableModel CreateHierarchyTable(
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

        return new ReportTableModel(
            [
                new ReportTableColumn("Employee", 2.2f),
                new ReportTableColumn("Department", 1.2f),
                new ReportTableColumn("Team", 1.2f),
                new ReportTableColumn("Job Title", 1.3f),
                new ReportTableColumn("Location", 1.4f),
                new ReportTableColumn("Birth", 0.7f),
                new ReportTableColumn("Age", 0.8f),
                new ReportTableColumn("Start", 0.9f),
                new ReportTableColumn("Manager", 1.1f),
                new ReportTableColumn("Availability", 1.6f)
            ],
            [
                .. rows.Select(row => new ReportTableRow(
                    [
                        new ReportTableCell(
                            row.DisplayName,
                            $"(#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})",
                            IsEmphasized: true,
                            IndentLevel: row.Level),
                        new ReportTableCell(row.Department ?? "-"),
                        new ReportTableCell(row.Team ?? "-"),
                        new ReportTableCell(row.JobTitle ?? "-"),
                        new ReportTableCell(row.Location ?? "-"),
                        new ReportTableCell(_formatter.FormatDate(row.DateOfBirth)),
                        new ReportTableCell(_formatter.FormatAge(row.DateOfBirth, referenceDate)),
                        new ReportTableCell(_formatter.FormatDate(row.EmploymentStartDate)),
                        new ReportTableCell(row.ManagerName ?? "-"),
                        new ReportTableCell(
                            _formatter.FormatAvailability(row.UnavailabilityEntries, referenceDate),
                            AvailabilityState: _formatter.GetAvailabilityState(
                                row.UnavailabilityEntries,
                                referenceDate))
                    ]))
            ],
            "No hierarchy data.");
    }

    /// <summary>
    /// Creates the recent hires table model.
    /// </summary>
    public ReportTableModel CreateRecentHiresTable(
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

        return new ReportTableModel(
            [
                new ReportTableColumn("Employee", 1.8f),
                new ReportTableColumn("Department", 1f),
                new ReportTableColumn("Team", 1f),
                new ReportTableColumn("Job Title", 1.3f),
                new ReportTableColumn("Location", 1.2f),
                new ReportTableColumn("Birth", 0.9f),
                new ReportTableColumn("Age", 0.7f),
                new ReportTableColumn("Start", 0.9f),
                new ReportTableColumn("Days With Us", 0.9f),
                new ReportTableColumn("Manager", 1.1f)
            ],
            [
                .. rows.Select(row => new ReportTableRow(
                    [
                        new ReportTableCell(
                            row.DisplayName,
                            $"(#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})",
                            IsEmphasized: true),
                        new ReportTableCell(row.Department ?? "-"),
                        new ReportTableCell(row.Team ?? "-"),
                        new ReportTableCell(row.JobTitle ?? "-"),
                        new ReportTableCell(row.Location ?? "-"),
                        new ReportTableCell(_formatter.FormatDate(row.DateOfBirth)),
                        new ReportTableCell(_formatter.FormatAge(row.DateOfBirth, referenceDate)),
                        new ReportTableCell(_formatter.FormatDate(row.EmploymentStartDate)),
                        new ReportTableCell(_formatter.FormatDaysWithUs(row.EmploymentStartDate, referenceDate)),
                        new ReportTableCell(row.ManagerName ?? "-")
                    ]))
            ],
            "No employees joined during the configured period.");
    }

    /// <summary>
    /// Creates the flat team report table model.
    /// </summary>
    public ReportTableModel CreateFlatTeamTable(
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

        return new ReportTableModel(
            [
                new ReportTableColumn("Employee", 1.8f),
                new ReportTableColumn("Department", 1f),
                new ReportTableColumn("Team", 1f),
                new ReportTableColumn("Job Title", 1.3f),
                new ReportTableColumn("Location", 1.2f),
                new ReportTableColumn("Phone", 1.5f),
                new ReportTableColumn("Vacation Leave Available", 1.4f),
                new ReportTableColumn("Birth", 0.9f),
                new ReportTableColumn("Age", 0.7f),
                new ReportTableColumn("Start", 0.9f),
                new ReportTableColumn("Manager", 1.1f),
                new ReportTableColumn("Availability", 1.2f)
            ],
            [
                .. rows.Select(row => new ReportTableRow(
                    [
                        new ReportTableCell(
                            row.DisplayName,
                            $"(#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})",
                            IsEmphasized: true),
                        new ReportTableCell(row.Department ?? "-"),
                        new ReportTableCell(row.Team ?? "-"),
                        new ReportTableCell(row.JobTitle ?? "-"),
                        new ReportTableCell(row.Location ?? "-"),
                        new ReportTableCell(_formatter.FormatPhones(row.Phones)),
                        new ReportTableCell(_formatter.FormatVacationLeaveBalance(row.VacationLeaveBalance)),
                        new ReportTableCell(_formatter.FormatDate(row.DateOfBirth)),
                        new ReportTableCell(_formatter.FormatAge(row.DateOfBirth, referenceDate)),
                        new ReportTableCell(_formatter.FormatDate(row.EmploymentStartDate)),
                        new ReportTableCell(row.ManagerName ?? "-"),
                        new ReportTableCell(
                            _formatter.FormatAvailability(row.UnavailabilityEntries, referenceDate),
                            AvailabilityState: _formatter.GetAvailabilityState(
                                row.UnavailabilityEntries,
                                referenceDate))
                    ]))
            ],
            "No team data.");
    }

    /// <summary>
    /// Creates the job title counts table model.
    /// </summary>
    public ReportTableModel CreateJobTitlesTable(IReadOnlyList<HierarchyReportRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return CreateKeyValueTable(
            _formatter.GetJobTitleCounts(rows),
            "Job Title",
            "People",
            "No job title data.");
    }

    /// <summary>
    /// Creates the teams table model.
    /// </summary>
    public ReportTableModel CreateTeamsTable(
        IReadOnlyList<HierarchyTeam> teams,
        string memberHeader = "Members")
    {
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberHeader);

        return new ReportTableModel(
            [
                new ReportTableColumn("Team", 1.6f),
                new ReportTableColumn("People", 54, IsConstantWidth: true),
                new ReportTableColumn(memberHeader, 2f),
                new ReportTableColumn("Grades", 2f)
            ],
            [
                .. teams.Select(team => new ReportTableRow(
                    [
                        new ReportTableCell($"{team.ManagerDisplayName}'s Team"),
                        new ReportTableCell(team.PeopleCount.ToString(CultureInfo.InvariantCulture)),
                        new ReportTableCell(string.Join(", ", team.MemberDisplayNames)),
                        new ReportTableCell(_formatter.FormatGradeCounts(team.GradeCounts))
                    ]))
            ],
            "No teams found.");
    }

    /// <summary>
    /// Creates the country and city distribution table model.
    /// </summary>
    public ReportTableModel CreateCountryCityTable(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts)
    {
        ArgumentNullException.ThrowIfNull(countryCityCounts);

        return new ReportTableModel(
            [
                new ReportTableColumn("Country", 1.3f),
                new ReportTableColumn("City", 1.7f),
                new ReportTableColumn("People", 70, IsConstantWidth: true)
            ],
            [
                .. countryCityCounts
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(country => _formatter.OrderCounts(country.Value)
                        .Select(city => new ReportTableRow(
                            [
                                new ReportTableCell(country.Key),
                                new ReportTableCell(city.Key),
                                new ReportTableCell(city.Value.ToString(CultureInfo.InvariantCulture))
                            ])))
            ],
            "No city breakdown available.");
    }

    /// <summary>
    /// Creates a generic bucket-to-count table model.
    /// </summary>
    public ReportTableModel CreateCountTable(
        IReadOnlyDictionary<string, int> counts,
        string emptyText)
    {
        ArgumentNullException.ThrowIfNull(counts);
        ArgumentException.ThrowIfNullOrWhiteSpace(emptyText);

        return CreateKeyValueTable(
            _formatter.OrderCounts(counts),
            "Bucket",
            "People",
            emptyText);
    }

    private static ReportTableModel CreateKeyValueTable(
        IReadOnlyList<KeyValuePair<string, int>> counts,
        string keyHeader,
        string valueHeader,
        string emptyText)
    {
        return new ReportTableModel(
            [
                new ReportTableColumn(keyHeader, 3f),
                new ReportTableColumn(valueHeader, 70, IsConstantWidth: true)
            ],
            [
                .. counts.Select(pair => new ReportTableRow(
                    [
                        new ReportTableCell(pair.Key),
                        new ReportTableCell(pair.Value.ToString(CultureInfo.InvariantCulture))
                    ]))
            ],
            emptyText);
    }

    private readonly IReportPresentationFormatter _formatter;
}
