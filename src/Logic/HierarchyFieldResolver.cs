using Abstractions;

using Models;

namespace Logic;

/// <summary>
/// Resolves BambooHR fields used by hierarchy-report logic.
/// </summary>
public sealed class HierarchyFieldResolver : IHierarchyFieldResolver
{
    /// <summary>
    /// Creates field resolver.
    /// </summary>
    public HierarchyFieldResolver(IBambooHrClient bambooHrClient)
    {
        ArgumentNullException.ThrowIfNull(bambooHrClient);
        _bambooHrClient = bambooHrClient;
    }

    /// <inheritdoc />
    public async Task<HierarchyFieldSelection> ResolveAsync(
        EmployeeId rootEmployeeId,
        CancellationToken ct)
    {
        var fields = await _bambooHrClient.GetFieldsAsync(ct).ConfigureAwait(false);
        var relationshipField = await ResolveRelationshipFieldAsync(rootEmployeeId, fields, ct)
            .ConfigureAwait(false);

        return new HierarchyFieldSelection(
            relationshipField,
            FindField(fields, PreferredLocationFields),
            FindField(fields, PreferredCountryFields),
            FindField(fields, PreferredCityFields),
            FindField(fields, PreferredBirthDateFields),
            FindField(fields, PreferredHireDateFields),
            FindField(fields, PreferredTeamFields),
            FindFields(fields, PreferredPhoneFields));
    }

    private async Task<HierarchyRelationshipField> ResolveRelationshipFieldAsync(
        EmployeeId rootEmployeeId,
        IReadOnlyCollection<BambooHrField> fields,
        CancellationToken ct)
    {
        var probeCandidates = PreferredManagerIdFields
            .Concat(PreferredManagerNameFields)
            .ToArray();
        var probeValues = await _bambooHrClient.GetEmployeeFieldsAsync(
                rootEmployeeId,
                [.. probeCandidates.Select(candidate => candidate.RequestKey)],
                ct)
            .ConfigureAwait(false);

        foreach (var candidate in probeCandidates)
        {
            if (probeValues.Values.ContainsKey(candidate.RequestKey))
            {
                return candidate;
            }
        }

        var managerIdField = FindField(fields, PreferredManagerIdFields);
        if (managerIdField is not null)
        {
            return managerIdField;
        }

        var managerNameField = FindField(fields, PreferredManagerNameFields);
        if (managerNameField is not null)
        {
            return managerNameField;
        }

        throw new InvalidOperationException(
            "No BambooHR manager relationship field could be resolved.");
    }

    private static HierarchyRelationshipField? FindField(
        IEnumerable<BambooHrField> fields,
        IReadOnlyCollection<HierarchyRelationshipField> candidates)
    {
        var fieldList = fields.ToArray();

        foreach (var candidate in candidates)
        {
            var exact = fieldList.FirstOrDefault(field =>
                string.Equals(
                    Normalize(field.RequestKey),
                    Normalize(candidate.RequestKey),
                    StringComparison.Ordinal));
            if (exact is not null)
            {
                return new HierarchyRelationshipField(
                    exact.RequestKey,
                    exact.Name,
                    candidate.UsesEmployeeId);
            }
        }

        foreach (var candidate in candidates)
        {
            var partial = fieldList.FirstOrDefault(field =>
                Normalize(field.RequestKey).Contains(
                    Normalize(candidate.RequestKey),
                    StringComparison.Ordinal)
                || Normalize(field.Name).Contains(
                    Normalize(candidate.DisplayName),
                    StringComparison.Ordinal));
            if (partial is not null)
            {
                return new HierarchyRelationshipField(
                    partial.RequestKey,
                    partial.Name,
                    candidate.UsesEmployeeId);
            }
        }

        return null;
    }

    private static BambooHrField? FindField(
        IEnumerable<BambooHrField> fields,
        (string RequestKey, string DisplayName)[] candidates)
    {
        var fieldList = fields.ToArray();

        foreach (var (requestKey, _) in candidates)
        {
            var exact = fieldList.FirstOrDefault(field =>
                string.Equals(
                    Normalize(field.RequestKey),
                    Normalize(requestKey),
                    StringComparison.Ordinal));
            if (exact is not null)
            {
                return exact;
            }
        }

        foreach (var (requestKey, displayName) in candidates)
        {
            var partial = fieldList.FirstOrDefault(field =>
                Normalize(field.RequestKey).Contains(
                    Normalize(requestKey),
                    StringComparison.Ordinal)
                || Normalize(field.Name).Contains(
                    Normalize(displayName),
                    StringComparison.Ordinal));
            if (partial is not null)
            {
                return partial;
            }
        }

        return null;
    }

    private static IReadOnlyList<BambooHrField> FindFields(
        IEnumerable<BambooHrField> fields,
        (string RequestKey, string DisplayName)[] candidates)
    {
        var fieldList = fields.ToArray();
        List<BambooHrField> matches = [];
        var addedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (requestKey, _) in candidates)
        {
            var exact = fieldList.FirstOrDefault(field =>
                string.Equals(
                    Normalize(field.RequestKey),
                    Normalize(requestKey),
                    StringComparison.Ordinal));
            if (exact is not null && addedKeys.Add(exact.RequestKey))
            {
                matches.Add(exact);
            }
        }

        foreach (var (requestKey, displayName) in candidates)
        {
            var partial = fieldList.FirstOrDefault(field =>
                Normalize(field.RequestKey).Contains(
                    Normalize(requestKey),
                    StringComparison.Ordinal)
                || Normalize(field.Name).Contains(
                    Normalize(displayName),
                    StringComparison.Ordinal));
            if (partial is not null && addedKeys.Add(partial.RequestKey))
            {
                matches.Add(partial);
            }
        }

        return matches;
    }

    private static string Normalize(string value)
    {
        var buffer = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();

        return new string(buffer);
    }

    private static readonly (string RequestKey, string DisplayName)[] PreferredLocationFields =
    [
        ("location", "Location"),
        ("office", "Office"),
        ("workLocation", "Work Location")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredCountryFields =
    [
        ("country", "Country"),
        ("countryName", "Country"),
        ("workCountry", "Work Country")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredCityFields =
    [
        ("city", "City"),
        ("workCity", "Work City"),
        ("officeCity", "Office City")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredBirthDateFields =
    [
        ("dateOfBirth", "Date of Birth"),
        ("birthDate", "Birth Date"),
        ("birthday", "Birthday"),
        ("dob", "Date of Birth")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredHireDateFields =
    [
        ("hireDate", "Hire Date"),
        ("startDate", "Start Date"),
        ("dateHired", "Date Hired"),
        ("employmentStartDate", "Employment Start Date")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredTeamFields =
    [
        ("division", "Division"),
        ("team", "Team"),
        ("teamName", "Team"),
        ("workTeam", "Work Team"),
        ("employeeTeam", "Employee Team")
    ];

    private static readonly (string RequestKey, string DisplayName)[] PreferredPhoneFields =
    [
        ("mobilePhone", "Mobile Phone"),
        ("phoneNumber", "Phone Number"),
        ("phone", "Phone"),
        ("workPhone", "Work Phone"),
        ("homePhone", "Home Phone")
    ];

    private static readonly HierarchyRelationshipField[] PreferredManagerIdFields =
    [
        new("supervisorEId", "Supervisor EId", usesEmployeeId: true),
        new("supervisorId", "Supervisor Id", usesEmployeeId: true),
        new("managerId", "Manager Id", usesEmployeeId: true),
        new("managerEId", "Manager EId", usesEmployeeId: true),
        new("reportsToId", "Reports To Id", usesEmployeeId: true),
        new("reportsToEId", "Reports To EId", usesEmployeeId: true)
    ];

    private static readonly HierarchyRelationshipField[] PreferredManagerNameFields =
    [
        new("reportsTo", "Reporting to", usesEmployeeId: false),
        new("supervisor", "Supervisor", usesEmployeeId: false),
        new("manager", "Manager", usesEmployeeId: false)
    ];

    private readonly IBambooHrClient _bambooHrClient;
}
