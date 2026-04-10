using FluentAssertions;

using Models;

namespace Logic.Tests;

public sealed class HierarchyTopologyBuilderTests
{
    [Fact(DisplayName = "The topology builder links employees by manager employee identifier when the relationship field stores IDs.")]
    [Trait("Category", "Unit")]
    public void BuildChildrenByManagerShouldUseManagerEmployeeIdentifiers()
    {
        // Arrange
        var builder = new HierarchyTopologyBuilder();
        var profiles = new[]
        {
            CreateProfile(1, "Alice Smith"),
            CreateProfile(2, "Bob Jones", managerEmployeeId: 1),
            CreateProfile(3, "Carol Brown", managerEmployeeId: 1),
            CreateProfile(4, "Diana White", managerEmployeeId: 2)
        };
        var relationshipField = new HierarchyRelationshipField("managerId", "Manager Id", usesEmployeeId: true);

        // Act
        var childrenByManager = builder.BuildChildrenByManager(profiles, relationshipField);

        // Assert
        childrenByManager.Should().HaveCount(2);
        childrenByManager[new EmployeeId(1)].Should().Equal(new EmployeeId(2), new EmployeeId(3));
        childrenByManager[new EmployeeId(2)].Should().Equal(new EmployeeId(4));
    }

    [Fact(DisplayName = "The topology builder throws when a manager name matches more than one employee.")]
    [Trait("Category", "Unit")]
    public void BuildChildrenByManagerShouldThrowWhenManagerNameIsAmbiguous()
    {
        // Arrange
        var builder = new HierarchyTopologyBuilder();
        var profiles = new[]
        {
            CreateProfile(1, "Alex Smith"),
            CreateProfile(2, "Alex Smith"),
            CreateProfile(3, "Carol Brown", managerDisplayName: "Alex Smith")
        };
        var relationshipField = new HierarchyRelationshipField("reportsTo", "Reports To", usesEmployeeId: false);

        // Act
        var action = () => builder.BuildChildrenByManager(profiles, relationshipField);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Manager name 'Alex Smith' is ambiguous in BambooHR.");
    }

    [Fact(DisplayName = "The hierarchy flattener orders children alphabetically, removes duplicate child links, and sorts unavailability entries.")]
    [Trait("Category", "Unit")]
    public void FlattenHierarchyShouldOrderChildrenAndEntriesDeterministically()
    {
        // Arrange
        var builder = new HierarchyTopologyBuilder();
        var rootEmployeeId = new EmployeeId(1);
        var bobEmployeeId = new EmployeeId(2);
        var carolEmployeeId = new EmployeeId(3);
        var profilesByEmployeeId = new Dictionary<EmployeeId, EmployeeProfile>
        {
            [rootEmployeeId] = CreateProfile(1, "Alice Smith"),
            [bobEmployeeId] = CreateProfile(2, "Bob Jones", managerEmployeeId: 1),
            [carolEmployeeId] = CreateProfile(3, "Carol Brown", managerEmployeeId: 1)
        };
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager = new Dictionary<EmployeeId, IReadOnlyList<EmployeeId>>
        {
            [rootEmployeeId] = [carolEmployeeId, bobEmployeeId, carolEmployeeId]
        };
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<TimeOffEntry>> employeeEntries = new Dictionary<EmployeeId, IReadOnlyList<TimeOffEntry>>
        {
            [bobEmployeeId] =
            [
                CreateTimeOffEntry(20, 2, "Bob Jones", 2026, 3, 20, 2026, 3, 20),
                CreateTimeOffEntry(10, 2, "Bob Jones", 2026, 3, 18, 2026, 3, 18)
            ]
        };
        List<HierarchyReportRow> rows = [];

        // Act
        builder.FlattenHierarchy(
            rootEmployeeId,
            level: 0,
            profilesByEmployeeId,
            childrenByManager,
            employeeEntries,
            rows);

        // Assert
        rows.Select(row => (row.DisplayName, row.Level)).Should().Equal(
            ("Alice Smith", 0),
            ("Bob Jones", 1),
            ("Carol Brown", 1));
        rows[1].UnavailabilityEntries.Select(entry => entry.Id).Should().Equal(10, 20);
        rows[1].ManagerName.Should().Be("Alice Smith");
        rows[1].WorkEmail.Should().Be("bob@example.com");
    }

    [Fact(DisplayName = "The hierarchy collector returns the root employee and all reachable descendants without duplicates.")]
    [Trait("Category", "Unit")]
    public void CollectHierarchyEmployeeIdsShouldReturnRootAndReachableDescendants()
    {
        // Arrange
        var builder = new HierarchyTopologyBuilder();
        var profilesByEmployeeId = new Dictionary<EmployeeId, EmployeeProfile>
        {
            [new EmployeeId(1)] = CreateProfile(1, "Alice Smith"),
            [new EmployeeId(2)] = CreateProfile(2, "Bob Jones", managerEmployeeId: 1),
            [new EmployeeId(3)] = CreateProfile(3, "Carol Brown", managerEmployeeId: 1),
            [new EmployeeId(4)] = CreateProfile(4, "Diana White", managerEmployeeId: 2)
        };
        IReadOnlyDictionary<EmployeeId, IReadOnlyList<EmployeeId>> childrenByManager = new Dictionary<EmployeeId, IReadOnlyList<EmployeeId>>
        {
            [new EmployeeId(1)] = [new EmployeeId(2), new EmployeeId(3), new EmployeeId(2)],
            [new EmployeeId(2)] = [new EmployeeId(4)]
        };

        // Act
        var employeeIds = builder.CollectHierarchyEmployeeIds(
            new EmployeeId(1),
            childrenByManager,
            profilesByEmployeeId);

        // Assert
        employeeIds.Should().BeEquivalentTo(
            [
                new EmployeeId(1),
                new EmployeeId(2),
                new EmployeeId(3),
                new EmployeeId(4)
            ]);
    }

    private static EmployeeProfile CreateProfile(
        int employeeId,
        string displayName,
        int? managerEmployeeId = null,
        string? managerDisplayName = null)
    {
        var names = displayName.Split(' ');

        return new EmployeeProfile(
            new EmployeeId(employeeId),
            displayName,
            names[0],
            names[^1],
            names[0],
            department: "Engineering",
            jobTitle: "Engineer",
            location: "Berlin, Germany",
            country: "Germany",
            city: "Berlin",
            dateOfBirth: null,
            hireDate: null,
            workEmail: $"{names[0].ToLowerInvariant()}@example.com",
            manager: new ManagerReference(
                managerEmployeeId is null ? null : new EmployeeId(managerEmployeeId.Value),
                managerDisplayName));
    }

    private static TimeOffEntry CreateTimeOffEntry(
        int id,
        int employeeId,
        string name,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
        => new(
            id,
            TimeOffEntryType.TimeOff,
            new EmployeeId(employeeId),
            name,
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay));
}
