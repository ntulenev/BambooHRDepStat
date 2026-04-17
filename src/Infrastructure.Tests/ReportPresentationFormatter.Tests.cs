using Abstractions;

using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class ReportPresentationFormatterTests
{
    [Fact(DisplayName = "The formatter returns available when no availability entries exist.")]
    [Trait("Category", "Unit")]
    public void FormatAvailabilityShouldReturnAvailableWhenThereAreNoEntries()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();

        // Act
        var text = formatter.FormatAvailability([], new DateOnly(2026, 4, 2));

        // Assert
        text.Should().Be("Available");
    }

    [Fact(DisplayName = "The formatter joins employee phones for display.")]
    [Trait("Category", "Unit")]
    public void FormatPhonesShouldJoinPhonesForDisplay()
    {
        var formatter = new ReportPresentationFormatter();

        var text = formatter.FormatPhones(
            [
                new EmployeePhone("Mobile Phone", "+49 111 1111"),
                new EmployeePhone("Work Phone", "+49 222 2222")
            ]);

        text.Should().Be("Mobile Phone: +49 111 1111 | Work Phone: +49 222 2222");
    }

    [Fact(DisplayName = "The formatter rounds vacation leave balances to tenths.")]
    [Trait("Category", "Unit")]
    public void FormatVacationLeaveBalanceShouldRoundToTenths()
    {
        var formatter = new ReportPresentationFormatter();

        var text = formatter.FormatVacationLeaveBalance(new VacationLeaveBalance(18.54m));

        text.Should().Be("18.5 days");
    }

    [Fact(DisplayName = "The formatter marks the availability state as available when no entries exist.")]
    [Trait("Category", "Unit")]
    public void GetAvailabilityStateShouldReturnAvailableWhenThereAreNoEntries()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();

        // Act
        var state = formatter.GetAvailabilityState([], new DateOnly(2026, 4, 2));

        // Assert
        state.Should().Be(ReportAvailabilityState.Available);
    }

    [Fact(DisplayName = "The formatter prefixes upcoming availability entries with an upcoming marker.")]
    [Trait("Category", "Unit")]
    public void FormatAvailabilityShouldPrefixUpcomingEntries()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        IReadOnlyList<TimeOffEntry> entries =
        [
            new TimeOffEntry(
                1,
                TimeOffEntryType.Holiday,
                employeeId: null,
                "Founders Day",
                new DateOnly(2026, 4, 3),
                new DateOnly(2026, 4, 3))
        ];

        // Act
        var text = formatter.FormatAvailability(entries, new DateOnly(2026, 4, 2));

        // Assert
        text.Should().Be("Upcoming: Holiday (Founders Day): 2026-04-03 - 2026-04-03");
    }

    [Fact(DisplayName = "The formatter marks future availability entries as upcoming.")]
    [Trait("Category", "Unit")]
    public void GetAvailabilityStateShouldReturnUpcomingWhenEntriesStartInFuture()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        IReadOnlyList<TimeOffEntry> entries =
        [
            new TimeOffEntry(
                1,
                TimeOffEntryType.Holiday,
                employeeId: null,
                "Founders Day",
                new DateOnly(2026, 4, 3),
                new DateOnly(2026, 4, 3))
        ];

        // Act
        var state = formatter.GetAvailabilityState(entries, new DateOnly(2026, 4, 2));

        // Assert
        state.Should().Be(ReportAvailabilityState.Upcoming);
    }

    [Fact(DisplayName = "The formatter renders current time off entries without the upcoming prefix.")]
    [Trait("Category", "Unit")]
    public void FormatAvailabilityShouldRenderCurrentTimeOffWithoutUpcomingPrefix()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        IReadOnlyList<TimeOffEntry> entries =
        [
            new TimeOffEntry(
                2,
                TimeOffEntryType.TimeOff,
                new EmployeeId(2),
                "Bob Jones",
                new DateOnly(2026, 4, 2),
                new DateOnly(2026, 4, 4))
        ];

        // Act
        var text = formatter.FormatAvailability(entries, new DateOnly(2026, 4, 2));

        // Assert
        text.Should().Be("Time off: 2026-04-02 - 2026-04-04");
    }

    [Fact(DisplayName = "The formatter marks current time off entries as unavailable today.")]
    [Trait("Category", "Unit")]
    public void GetAvailabilityStateShouldReturnUnavailableTodayWhenReferenceDateIsInsideRange()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        IReadOnlyList<TimeOffEntry> entries =
        [
            new TimeOffEntry(
                2,
                TimeOffEntryType.TimeOff,
                new EmployeeId(2),
                "Bob Jones",
                new DateOnly(2026, 4, 2),
                new DateOnly(2026, 4, 4))
        ];

        // Act
        var state = formatter.GetAvailabilityState(entries, new DateOnly(2026, 4, 2));

        // Assert
        state.Should().Be(ReportAvailabilityState.UnavailableToday);
    }

    [Fact(DisplayName = "The formatter groups job titles and orders them by frequency, then name.")]
    [Trait("Category", "Unit")]
    public void GetJobTitleCountsShouldGroupAndOrderRows()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        IReadOnlyList<HierarchyReportRow> rows =
        [
            CreateRow(1, "Alice Smith", "Engineer"),
            CreateRow(2, "Bob Jones", null),
            CreateRow(3, "Carol Brown", "Engineer"),
            CreateRow(4, "Diana White", "Analyst")
        ];

        // Act
        var counts = formatter.GetJobTitleCounts(rows);

        // Assert
        counts.Should().Equal(
            new KeyValuePair<string, int>("Engineer", 2),
            new KeyValuePair<string, int>("(No title)", 1),
            new KeyValuePair<string, int>("Analyst", 1));
    }

    [Fact(DisplayName = "The formatter builds hierarchy display names with level indentation and the employee identifier.")]
    [Trait("Category", "Unit")]
    public void BuildHierarchyDisplayNameShouldIncludeLevelIndentationAndEmployeeIdentifier()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        var row = CreateRow(7, "Carol Brown", "Engineer", level: 2);

        // Act
        var displayName = formatter.BuildHierarchyDisplayName(row);

        // Assert
        displayName.Should().Be("    |- Carol Brown (#7)");
    }

    [Fact(DisplayName = "The formatter builds the holiday section title from the availability window.")]
    [Trait("Category", "Unit")]
    public void BuildHolidaySectionTitleShouldUseAvailabilityWindowDates()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();
        var availabilityWindow = new AvailabilityWindow(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 7));

        // Act
        var title = formatter.BuildHolidaySectionTitle(availabilityWindow);

        // Assert
        title.Should().Be("Holidays (2026-04-01 to 2026-04-07)");
    }

    [Fact(DisplayName = "The formatter builds the recent-hire section title from the configured period length.")]
    [Trait("Category", "Unit")]
    public void BuildRecentHireSectionTitleShouldUseConfiguredPeriodLength()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();

        // Act
        var title = formatter.BuildRecentHireSectionTitle(30);

        // Assert
        title.Should().Be("New Joiners (Last 30 Days)");
    }

    [Fact(DisplayName = "The formatter calculates age from the reference date.")]
    [Trait("Category", "Unit")]
    public void FormatAgeShouldCalculateAgeFromReferenceDate()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();

        // Act
        var age = formatter.FormatAge(new DateOnly(1995, 4, 2), new DateOnly(2026, 4, 1));

        // Assert
        age.Should().Be("30");
    }

    [Fact(DisplayName = "The formatter normalizes days-with-us to at least one day.")]
    [Trait("Category", "Unit")]
    public void FormatDaysWithUsShouldReturnAtLeastOneDay()
    {
        // Arrange
        var formatter = new ReportPresentationFormatter();

        // Act
        var daysWithUs = formatter.FormatDaysWithUs(new DateOnly(2026, 4, 5), new DateOnly(2026, 4, 1));

        // Assert
        daysWithUs.Should().Be("1 days");
    }

    private static HierarchyReportRow CreateRow(
        int employeeId,
        string displayName,
        string? jobTitle,
        int level = 0)
        => new(
            level,
            new EmployeeId(employeeId),
            displayName,
            department: "Engineering",
            jobTitle,
            location: "Berlin, Germany",
            dateOfBirth: null,
            employmentStartDate: null,
            managerName: null,
            []);
}
