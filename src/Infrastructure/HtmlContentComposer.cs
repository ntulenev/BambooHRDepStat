using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// Composes standalone HTML for the hierarchy report.
/// </summary>
public sealed class HtmlContentComposer
{
    /// <summary>
    /// Creates HTML content composer.
    /// </summary>
    public HtmlContentComposer(IReportPresentationFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatter = formatter;
    }

    public string Compose(HierarchyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var template = HtmlTemplateLoader.LoadHierarchyReportTemplate();
        var jobTitleCounts = _formatter.GetJobTitleCounts(report.Hierarchy.Rows);

        return ApplyTemplate(
            template,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__ROOT_EMPLOYEE__"] = Encode(report.Overview.RootEmployeeName),
                ["__GENERATED_AT__"] = Encode(
                    report.Overview.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                ["__WORK_WEEK__"] = Encode(
                    $"{report.Overview.AvailabilityWindow.Start:yyyy-MM-dd} to {report.Overview.AvailabilityWindow.End:yyyy-MM-dd}"),
                ["__RELATIONSHIP__"] = Encode(
                    $"{report.Overview.RelationshipField.DisplayName} ({(report.Overview.RelationshipField.UsesEmployeeId ? "employee ID" : "employee name")})"),
                ["__TOTAL_PEOPLE__"] = report.Hierarchy.Rows.Count.ToString(CultureInfo.InvariantCulture),
                ["__TOTAL_TEAMS__"] = report.Summaries.Teams.Count.ToString(CultureInfo.InvariantCulture),
                ["__TOTAL_COUNTRIES__"] = report.Distributions.LocationCounts.Count.ToString(CultureInfo.InvariantCulture),
                ["__TOTAL_JOB_TITLES__"] = jobTitleCounts.Count.ToString(CultureInfo.InvariantCulture),
                ["__TEAM_SIZE_JSON__"] = BuildTeamSizeChartJson(report.Summaries.Teams),
                ["__TEAM_GRADE_JSON__"] = BuildTeamGradeChartJson(report.Summaries.Teams),
                ["__HOLIDAY_TITLE__"] = Encode(_formatter.BuildHolidaySectionTitle(report.Overview.AvailabilityWindow)),
                ["__HOLIDAY_ROWS__"] = BuildHolidayRows(report.Hierarchy.Holidays),
                ["__HIERARCHY_ROWS__"] = BuildHierarchyRows(report.Hierarchy.Rows, report.Overview.GeneratedAt),
                ["__JOB_TITLE_ROWS__"] = BuildCountTableRows(jobTitleCounts, "No job title data."),
                ["__TEAM_CARDS__"] = BuildTeamCards(report.Summaries.Teams),
                ["__LOCATION_CARDS__"] = BuildDistributionCards(report.Distributions.LocationCounts, warm: false),
                ["__COUNTRY_CITY_SECTIONS__"] = BuildCountryCitySections(report.Distributions.CountryCityCounts),
                ["__AGE_CARDS__"] = BuildDistributionCards(report.Distributions.AgeCounts, warm: true),
                ["__TENURE_CARDS__"] = BuildDistributionCards(report.Distributions.TenureCounts, warm: true),
                ["__RECENT_HIRE_TITLE__"] = Encode(
                    _formatter.BuildRecentHireSectionTitle(report.Summaries.RecentHirePeriodDays)),
                ["__RECENT_HIRE_ROWS__"] = BuildRecentHireRows(report.Summaries.RecentHires, report.Overview.GeneratedAt)
            });
    }

    private string BuildHierarchyRows(
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return """              <tr><td class="empty" colspan="9">No hierarchy data.</td></tr>""";
        }

        var html = new StringBuilder(rows.Count * 320);
        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

        foreach (var row in rows)
        {
            var padding = row.Level * 20;
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"              <tr>
                <td style=""padding-left:{padding}px""><strong>{Encode(row.DisplayName)}</strong> <span class=""muted"">(#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})</span></td>
                <td>{Encode(row.Department ?? "-")}</td>
                <td>{Encode(row.JobTitle ?? "-")}</td>
                <td>{Encode(row.Location ?? "-")}</td>
                <td>{Encode(_formatter.FormatDate(row.DateOfBirth))}</td>
                <td>{Encode(_formatter.FormatAge(row.DateOfBirth, referenceDate))}</td>
                <td>{Encode(_formatter.FormatDate(row.EmploymentStartDate))}</td>
                <td>{Encode(row.ManagerName ?? "-")}</td>
                <td>{BuildAvailabilityMarkup(row.UnavailabilityEntries, referenceDate)}</td>
              </tr>");
        }

        return html.ToString();
    }

    private static string BuildCountTableRows(
        IReadOnlyList<KeyValuePair<string, int>> counts,
        string emptyText)
    {
        if (counts.Count == 0)
        {
            return $@"              <tr><td class=""empty"" colspan=""2"">{Encode(emptyText)}</td></tr>";
        }

        var html = new StringBuilder(counts.Count * 96);
        foreach (var pair in counts)
        {
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"              <tr><td>{Encode(pair.Key)}</td><td>{pair.Value.ToString(CultureInfo.InvariantCulture)}</td></tr>");
        }

        return html.ToString();
    }

    private string BuildHolidayRows(IReadOnlyList<HolidayReportItem> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        if (holidays.Count == 0)
        {
            return """              <tr><td class="empty" colspan="4">No holidays found in the availability window.</td></tr>""";
        }

        var html = new StringBuilder(holidays.Count * 140);
        foreach (var holiday in holidays)
        {
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"              <tr>
                <td>{Encode(holiday.Name)}</td>
                <td>{Encode(_formatter.FormatDate(holiday.Start))}</td>
                <td>{Encode(_formatter.FormatDate(holiday.End))}</td>
                <td>{Encode(_formatter.FormatAssociatedCountries(holiday.AssociatedCountries))}</td>
              </tr>");
        }

        return html.ToString();
    }

    private string BuildRecentHireRows(
        IReadOnlyList<HierarchyReportRow> rows,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return """              <tr><td class="empty" colspan="8">No employees joined during the configured period.</td></tr>""";
        }

        var html = new StringBuilder(rows.Count * 256);
        var referenceDate = DateOnly.FromDateTime(generatedAt.Date);

        foreach (var row in rows)
        {
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"              <tr>
                <td><strong>{Encode(row.DisplayName)}</strong> <span class=""muted"">(#{row.EmployeeId.ToString(CultureInfo.InvariantCulture)})</span></td>
                <td>{Encode(row.Department ?? "-")}</td>
                <td>{Encode(row.JobTitle ?? "-")}</td>
                <td>{Encode(row.Location ?? "-")}</td>
                <td>{Encode(_formatter.FormatDate(row.DateOfBirth))}</td>
                <td>{Encode(_formatter.FormatAge(row.DateOfBirth, referenceDate))}</td>
                <td>{Encode(_formatter.FormatDate(row.EmploymentStartDate))}</td>
                <td>{Encode(_formatter.FormatDaysWithUs(row.EmploymentStartDate, referenceDate))}</td>
                <td>{Encode(row.ManagerName ?? "-")}</td>
              </tr>");
        }

        return html.ToString();
    }

    private static string BuildTeamCards(IReadOnlyList<HierarchyTeam> teams)
    {
        if (teams.Count == 0)
        {
            return """          <p class="empty">No teams found.</p>""";
        }

        var maxCount = teams.Max(team => team.PeopleCount);
        var html = new StringBuilder(teams.Count * 512);
        _ = html.AppendLine("""          <div class="team-grid">""");

        foreach (var team in teams)
        {
            var width = GetWidthPercent(team.PeopleCount, maxCount);
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"            <article class=""team-card"">
              <div class=""team-header"">
                <div class=""team-name"">{Encode(team.ManagerDisplayName)}'s Team</div>
                <div class=""team-count"">{team.PeopleCount.ToString(CultureInfo.InvariantCulture)} people</div>
              </div>
              <div class=""bar-track""><div class=""bar-fill"" style=""width:{width}%""></div></div>
              <div class=""meta-list""><strong>Members:</strong> {Encode(string.Join(", ", team.MemberDisplayNames))}</div>
              <div class=""chip-list"">{BuildGradeChips(team.GradeCounts)}</div>
            </article>");
        }

        _ = html.AppendLine("""          </div>""");
        return html.ToString();
    }

    private static string BuildGradeChips(IReadOnlyDictionary<string, int> gradeCounts)
    {
        if (gradeCounts.Count == 0)
        {
            return """<span class="chip">No grades</span>""";
        }

        return string.Join(
            string.Empty,
            gradeCounts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair =>
                    $@"<span class=""chip"">{Encode(pair.Key)}: {pair.Value.ToString(CultureInfo.InvariantCulture)}</span>"));
    }

    private string BuildDistributionCards(
        IReadOnlyDictionary<string, int> counts,
        bool warm)
    {
        var orderedCounts = _formatter.OrderCounts(counts);
        if (orderedCounts.Count == 0)
        {
            return """          <p class="empty">No data.</p>""";
        }

        var maxCount = orderedCounts.Max(pair => pair.Value);
        var html = new StringBuilder(orderedCounts.Count * 256);
        _ = html.AppendLine("""          <div class="distribution-grid">""");

        foreach (var pair in orderedCounts)
        {
            var width = GetWidthPercent(pair.Value, maxCount);
            var fillCssClass = warm ? "bar-fill warm" : "bar-fill";
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"            <article class=""distribution-card"">
              <div class=""distribution-header"">
                <div class=""distribution-name"">{Encode(pair.Key)}</div>
                <div class=""distribution-count"">{pair.Value.ToString(CultureInfo.InvariantCulture)}</div>
              </div>
              <div class=""bar-track""><div class=""{fillCssClass}"" style=""width:{width}%""></div></div>
            </article>");
        }

        _ = html.AppendLine("""          </div>""");
        return html.ToString();
    }

    private string BuildCountryCitySections(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> countryCityCounts)
    {
        if (countryCityCounts.Count == 0)
        {
            return """          <p class="empty">No city breakdown available.</p>""";
        }

        var html = new StringBuilder(countryCityCounts.Count * 512);

        foreach (var country in countryCityCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var orderedCities = _formatter.OrderCounts(country.Value);
            if (orderedCities.Count == 0)
            {
                continue;
            }

            var maxCount = orderedCities.Max(pair => pair.Value);
            _ = html.AppendLine(
                CultureInfo.InvariantCulture,
                $@"          <section class=""country-card""><div class=""distribution-name"">{Encode(country.Key)}</div>");
            _ = html.AppendLine("""            <div class="distribution-grid" style="margin-top:10px">""");

            foreach (var city in orderedCities)
            {
                var width = GetWidthPercent(city.Value, maxCount);
                _ = html.AppendLine(
                    CultureInfo.InvariantCulture,
                    $@"              <article class=""distribution-card"">
                <div class=""distribution-header"">
                  <div class=""distribution-name"">{Encode(city.Key)}</div>
                  <div class=""distribution-count"">{city.Value.ToString(CultureInfo.InvariantCulture)}</div>
                </div>
                <div class=""bar-track""><div class=""bar-fill warm"" style=""width:{width}%""></div></div>
              </article>");
            }

            _ = html.AppendLine("""            </div></section>""");
        }

        return html.ToString();
    }

    private static int GetWidthPercent(int value, int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0;
        }

        return Math.Max(8, (int)Math.Round(value * 100d / maxValue, MidpointRounding.AwayFromZero));
    }

    private string BuildAvailabilityMarkup(
        IReadOnlyList<TimeOffEntry> entries,
        DateOnly referenceDate)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var state = _formatter.GetAvailabilityState(entries, referenceDate);
        var cssClass = state switch
        {
            ReportAvailabilityState.Available => "availability-available",
            ReportAvailabilityState.Upcoming => "availability-upcoming",
            ReportAvailabilityState.UnavailableToday => "availability-timeoff",
            _ => throw new InvalidOperationException("Unknown availability state.")
        };

        return $@"<span class=""availability {cssClass}"">{Encode(_formatter.FormatAvailability(entries, referenceDate))}</span>";
    }

    private static string BuildTeamSizeChartJson(IReadOnlyList<HierarchyTeam> teams)
    {
        var chartData = teams
            .OrderByDescending(team => team.PeopleCount)
            .ThenBy(team => team.ManagerDisplayName, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => new
            {
                label = $"{team.ManagerDisplayName}'s Team",
                value = team.PeopleCount,
                color = TeamChartPalette[index % TeamChartPalette.Length]
            })
            .ToArray();

        return JsonSerializer.Serialize(chartData, JsonOptions);
    }

    private static string BuildTeamGradeChartJson(IReadOnlyList<HierarchyTeam> teams)
    {
        var allGrades = teams
            .SelectMany(team => team.GradeCounts.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(grade => grade, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var fallbackColors = TeamChartPalette.ToArray();
        var colorIndex = 0;
        var gradeColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var grade in allGrades)
        {
            if (PreferredGradeColors.TryGetValue(grade, out var preferredColor))
            {
                gradeColors[grade] = preferredColor;
                continue;
            }

            gradeColors[grade] = fallbackColors[colorIndex % fallbackColors.Length];
            colorIndex++;
        }

        var chartData = teams
            .OrderByDescending(team => team.PeopleCount)
            .ThenBy(team => team.ManagerDisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(team => new
            {
                label = $"{team.ManagerDisplayName}'s Team",
                total = team.PeopleCount,
                segments = team.GradeCounts
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair => new
                    {
                        label = pair.Key,
                        value = pair.Value,
                        color = gradeColors[pair.Key]
                    })
                    .ToArray()
            })
            .ToArray();

        return JsonSerializer.Serialize(chartData, JsonOptions);
    }

    private static string ApplyTemplate(
        string template,
        IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static readonly string[] TeamChartPalette =
    [
        "#7ad0a4",
        "#6fa8ff",
        "#f0a35e",
        "#b98ae8",
        "#f3cb67",
        "#e486b4",
        "#6fc9d9",
        "#9fd38a"
    ];

    private static readonly Dictionary<string, string> PreferredGradeColors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Junior"] = "#6fa8ff",
            ["Middle"] = "#f3cb67",
            ["Senior"] = "#f0a35e",
            ["Lead"] = "#c6c6c6",
            ["Team Lead"] = "#c6c6c6",
            ["Tech Lead"] = "#7ad0a4",
            ["Manager"] = "#e486b4",
            ["Director"] = "#b98ae8",
            ["Head"] = "#6fc9d9"
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IReportPresentationFormatter _formatter;
}
