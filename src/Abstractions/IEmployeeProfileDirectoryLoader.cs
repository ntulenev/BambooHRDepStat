using Models;

namespace Abstractions;

/// <summary>
/// Loads enriched employee profiles from BambooHR.
/// </summary>
public interface IEmployeeProfileDirectoryLoader
{
    /// <summary>
    /// Loads report-ready employee profiles for all active employees.
    /// </summary>
    Task<IReadOnlyList<EmployeeProfile>> LoadAsync(
        IReadOnlyList<BasicEmployee> employees,
        HierarchyRelationshipField relationshipField,
        BambooHrField? locationField,
        BambooHrField? countryField,
        BambooHrField? cityField,
        BambooHrField? birthDateField,
        BambooHrField? hireDateField,
        CancellationToken ct);
}
