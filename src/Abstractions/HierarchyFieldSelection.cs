using Models;

namespace Abstractions;

/// <summary>
/// Selected BambooHR fields required to build a hierarchy report.
/// </summary>
public sealed class HierarchyFieldSelection
{
    /// <summary>
    /// Creates selected field set.
    /// </summary>
    public HierarchyFieldSelection(
        HierarchyRelationshipField relationshipField,
        BambooHrField? locationField,
        BambooHrField? countryField,
        BambooHrField? cityField,
        BambooHrField? birthDateField,
        BambooHrField? hireDateField,
        BambooHrField? teamField,
        IReadOnlyList<BambooHrField>? phoneFields = null,
        BambooHrField? vacationLeaveAvailableField = null)
    {
        ArgumentNullException.ThrowIfNull(relationshipField);

        RelationshipField = relationshipField;
        LocationField = locationField;
        CountryField = countryField;
        CityField = cityField;
        BirthDateField = birthDateField;
        HireDateField = hireDateField;
        TeamField = teamField;
        PhoneFields = phoneFields ?? [];
        VacationLeaveAvailableField = vacationLeaveAvailableField;
    }

    /// <summary>
    /// Gets the field used to resolve hierarchy relationships.
    /// </summary>
    public HierarchyRelationshipField RelationshipField { get; }

    /// <summary>
    /// Gets the preferred location field when available.
    /// </summary>
    public BambooHrField? LocationField { get; }

    /// <summary>
    /// Gets the preferred country field when available.
    /// </summary>
    public BambooHrField? CountryField { get; }

    /// <summary>
    /// Gets the preferred city field when available.
    /// </summary>
    public BambooHrField? CityField { get; }

    /// <summary>
    /// Gets the preferred birth date field when available.
    /// </summary>
    public BambooHrField? BirthDateField { get; }

    /// <summary>
    /// Gets the preferred hire date field when available.
    /// </summary>
    public BambooHrField? HireDateField { get; }

    /// <summary>
    /// Gets the preferred team field when available.
    /// </summary>
    public BambooHrField? TeamField { get; }

    /// <summary>
    /// Gets preferred phone fields when available.
    /// </summary>
    public IReadOnlyList<BambooHrField> PhoneFields { get; }

    /// <summary>
    /// Gets the preferred vacation leave available field when available.
    /// </summary>
    public BambooHrField? VacationLeaveAvailableField { get; }
}
