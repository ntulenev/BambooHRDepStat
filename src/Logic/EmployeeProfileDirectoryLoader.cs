using System.Collections.Concurrent;
using System.Globalization;

using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Loads enriched employee profiles required for report generation.
/// </summary>
public sealed class EmployeeProfileDirectoryLoader : IEmployeeProfileDirectoryLoader
{
    /// <summary>
    /// Creates employee profile loader.
    /// </summary>
    public EmployeeProfileDirectoryLoader(
        IBambooHrClient bambooHrClient,
        ILoadingNotifier loadingNotifier)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        ArgumentNullException.ThrowIfNull(loadingNotifier);

        _bambooHrClient = bambooHrClient;
        _loadingNotifier = loadingNotifier;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmployeeProfile>> LoadAsync(
        IReadOnlyList<BasicEmployee> employees,
        HierarchyRelationshipField relationshipField,
        BambooHrField? locationField,
        BambooHrField? countryField,
        BambooHrField? cityField,
        BambooHrField? birthDateField,
        BambooHrField? hireDateField,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(relationshipField);

        var requestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "department",
            "jobTitle",
            "workEmail",
            relationshipField.RequestKey
        };

        AddFieldRequestKey(requestKeys, locationField);
        AddFieldRequestKey(requestKeys, countryField);
        AddFieldRequestKey(requestKeys, cityField);
        AddFieldRequestKey(requestKeys, birthDateField);
        AddFieldRequestKey(requestKeys, hireDateField);

        var profiles = new ConcurrentBag<EmployeeProfile>();
        var loadedProfiles = 0;
        var options = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = 2
        };

        await Parallel.ForEachAsync(
                employees,
                options,
                async (employee, cancellationToken) =>
                {
                    var fieldValues = await _bambooHrClient.GetEmployeeFieldsAsync(
                            employee.EmployeeId,
                            requestKeys,
                            cancellationToken)
                        .ConfigureAwait(false);

                    _ = fieldValues.Values.TryGetValue("department", out var department);
                    _ = fieldValues.Values.TryGetValue("jobTitle", out var jobTitle);
                    _ = fieldValues.Values.TryGetValue("workEmail", out var workEmail);
                    _ = TryGetFieldValue(fieldValues, locationField, out var location);
                    _ = TryGetFieldValue(fieldValues, countryField, out var country);
                    _ = TryGetFieldValue(fieldValues, cityField, out var city);
                    _ = TryGetFieldValue(fieldValues, birthDateField, out var birthDateValue);
                    _ = TryGetFieldValue(fieldValues, hireDateField, out var hireDateValue);
                    _ = fieldValues.Values.TryGetValue(
                        relationshipField.RequestKey,
                        out var managerLookupValue);

                    profiles.Add(new EmployeeProfile(
                        employee.EmployeeId,
                        employee.DisplayName,
                        employee.FirstName,
                        employee.LastName,
                        employee.PreferredName,
                        department,
                        jobTitle ?? employee.JobTitle,
                        location,
                        country,
                        city,
                        ParseDate(birthDateValue),
                        ParseDate(hireDateValue),
                        workEmail,
                        ManagerReference.Parse(managerLookupValue)));

                    var completed = Interlocked.Increment(ref loadedProfiles);
                    _loadingNotifier.SetProgress(
                        $"Loading employee profiles ({completed}/{employees.Count})...",
                        completed,
                        employees.Count);
                })
            .ConfigureAwait(false);

        return [.. profiles.OrderBy(profile => profile.EmployeeId)];
    }

    /// <summary>
    /// Adds a field request key when the field is available.
    /// </summary>
    private static void AddFieldRequestKey(HashSet<string> requestKeys, BambooHrField? field)
    {
        if (field is not null)
        {
            _ = requestKeys.Add(field.RequestKey);
        }
    }

    /// <summary>
    /// Tries to read a dynamic field value by BambooHR field metadata.
    /// </summary>
    private static bool TryGetFieldValue(
        EmployeeFieldValues fieldValues,
        BambooHrField? field,
        out string? value)
    {
        value = null;
        return field is not null
            && fieldValues.Values.TryGetValue(field.RequestKey, out value);
    }

    /// <summary>
    /// Parses a BambooHR date value into a local date when possible.
    /// </summary>
    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return null;
    }

    private readonly IBambooHrClient _bambooHrClient;
    private readonly ILoadingNotifier _loadingNotifier;
}
