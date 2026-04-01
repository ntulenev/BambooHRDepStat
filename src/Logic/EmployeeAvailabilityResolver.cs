using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves report availability entries and holiday applicability.
/// </summary>
public sealed class EmployeeAvailabilityResolver : IEmployeeAvailabilityResolver
{
    /// <inheritdoc />
    public Dictionary<string, IReadOnlyList<string>> BuildHolidayCountryMappings(
        IReadOnlyDictionary<string, string[]> configuredMappings)
    {
        ArgumentNullException.ThrowIfNull(configuredMappings);

        var mappings = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in configuredMappings)
        {
            var countries = (mapping.Value ?? [])
                .Where(country => !string.IsNullOrWhiteSpace(country))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(country => country, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            mappings[mapping.Key] = countries;
        }

        return mappings;
    }

    /// <inheritdoc />
    public IReadOnlyList<HolidayReportItem> BuildHolidayEntries(
        IReadOnlyList<TimeOffEntry> whoIsOut,
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings)
    {
        ArgumentNullException.ThrowIfNull(whoIsOut);
        ArgumentNullException.ThrowIfNull(holidayCountryMappings);

        return
        [
            .. whoIsOut
                .Where(entry => entry.Type == TimeOffEntryType.Holiday)
                .GroupBy(entry => new
                {
                    entry.Name,
                    entry.Start,
                    entry.End
                })
                .Select(group => group
                    .OrderBy(entry => entry.Id)
                    .First())
                .Select(entry => new HolidayReportItem(
                    entry.Name,
                    entry.Start,
                    entry.End,
                    holidayCountryMappings.TryGetValue(entry.Name, out var associatedCountries)
                        ? associatedCountries
                        : []))
                .OrderBy(entry => entry.Start)
                .ThenBy(entry => entry.End)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
        ];
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> BuildEmployeeEntries(
        IReadOnlyCollection<EmployeeProfile> includedProfiles,
        IReadOnlyList<TimeOffEntry> whoIsOut,
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings)
    {
        ArgumentNullException.ThrowIfNull(includedProfiles);
        ArgumentNullException.ThrowIfNull(whoIsOut);
        ArgumentNullException.ThrowIfNull(holidayCountryMappings);

        var timeOffEntries = whoIsOut
            .Where(entry => entry.Type == TimeOffEntryType.TimeOff && entry.EmployeeId.HasValue)
            .GroupBy(entry => entry.EmployeeId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TimeOffEntry>)[
                    .. group
                        .OrderBy(entry => entry.Start)
                        .ThenBy(entry => entry.End)
                        .ThenBy(entry => entry.Id)
                ]);

        var holidayEntries = whoIsOut
            .Where(entry => entry.Type == TimeOffEntryType.Holiday)
            .Where(entry => holidayCountryMappings.ContainsKey(entry.Name))
            .GroupBy(entry => new
            {
                entry.Name,
                entry.Start,
                entry.End
            })
            .Select(group => group
                .OrderBy(entry => entry.Id)
                .First())
            .OrderBy(entry => entry.Start)
            .ThenBy(entry => entry.End)
            .ThenBy(entry => entry.Id)
            .ToArray();

        var entriesByEmployee = new Dictionary<EmployeeId, IReadOnlyList<TimeOffEntry>>();

        foreach (var profile in includedProfiles)
        {
            List<TimeOffEntry> employeeEntries = [];

            if (timeOffEntries.TryGetValue(profile.EmployeeId, out var personalEntries))
            {
                employeeEntries.AddRange(personalEntries);
            }

            var employeeCountry = profile.ResolveCountryLabel();
            employeeEntries.AddRange(
                holidayEntries.Where(holiday => IsHolidayApplicableToCountry(
                    holiday,
                    employeeCountry,
                    holidayCountryMappings)));

            entriesByEmployee[profile.EmployeeId] =
            [
                .. employeeEntries
                    .OrderBy(entry => entry.Start)
                    .ThenBy(entry => entry.End)
                    .ThenBy(entry => entry.Id)
            ];
        }

        return entriesByEmployee;
    }

    /// <summary>
    /// Determines whether a holiday applies to an employee based on normalized country mappings.
    /// </summary>
    private static bool IsHolidayApplicableToCountry(
        TimeOffEntry holiday,
        string employeeCountry,
        Dictionary<string, IReadOnlyList<string>> holidayCountryMappings)
    {
        ArgumentNullException.ThrowIfNull(holiday);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCountry);
        ArgumentNullException.ThrowIfNull(holidayCountryMappings);

        if (!holidayCountryMappings.TryGetValue(holiday.Name, out var countries)
            || countries.Count == 0)
        {
            return false;
        }

        return countries.Any(country =>
            string.Equals(country, employeeCountry, StringComparison.OrdinalIgnoreCase));
    }
}
