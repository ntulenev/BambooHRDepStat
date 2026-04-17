using Abstractions;

using FluentAssertions;

using Moq;

using Models;

namespace Logic.Tests;

public sealed class HierarchyFieldResolverTests
{
    [Fact(DisplayName = "The constructor throws when the BambooHR client is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenBambooHrClientIsNull()
    {
        // Arrange

        // Act
        var action = () => new HierarchyFieldResolver(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The resolver uses the relationship field discovered from the root employee probe and returns the preferred supporting fields.")]
    [Trait("Category", "Unit")]
    public async Task ResolveAsyncShouldUseProbeFieldAndReturnPreferredSupportingFields()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var getFieldsCalls = 0;
        var getEmployeeFieldsCalls = 0;
        var fields = new[]
        {
            CreateField("office_location", "Office Location"),
            CreateField("countryName", "Country"),
            CreateField("officeCity", "Office City"),
            CreateField("birthday", "Birthday"),
            CreateField("employmentStartDate", "Employment Start Date"),
            CreateField("division", "Division"),
            CreateField("mobilePhone", "Mobile Phone"),
            CreateField("workPhone", "Work Phone")
        };

        bambooHrClient.Setup(client => client.GetFieldsAsync(
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getFieldsCalls++)
            .ReturnsAsync(fields);
        bambooHrClient.Setup(client => client.GetEmployeeFieldsAsync(
                It.Is<EmployeeId>(employeeId => employeeId == new EmployeeId(42)),
                It.Is<IReadOnlyCollection<string>>(keys => MatchKeys(
                    keys,
                    "supervisorEId",
                    "supervisorId",
                    "managerId",
                    "managerEId",
                    "reportsToId",
                    "reportsToEId",
                    "reportsTo",
                    "supervisor",
                    "manager")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getEmployeeFieldsCalls++)
            .ReturnsAsync(new EmployeeFieldValues(
                new EmployeeId(42),
                new Dictionary<string, string?>
                {
                    ["managerId"] = "7"
                }));

        var resolver = new HierarchyFieldResolver(bambooHrClient.Object);

        // Act
        var selection = await resolver.ResolveAsync(new EmployeeId(42), cts.Token);

        // Assert
        selection.RelationshipField.RequestKey.Should().Be("managerId");
        selection.RelationshipField.DisplayName.Should().Be("Manager Id");
        selection.RelationshipField.UsesEmployeeId.Should().BeTrue();
        selection.LocationField!.RequestKey.Should().Be("office_location");
        selection.CountryField!.RequestKey.Should().Be("countryName");
        selection.CityField!.RequestKey.Should().Be("officeCity");
        selection.BirthDateField!.RequestKey.Should().Be("birthday");
        selection.HireDateField!.RequestKey.Should().Be("employmentStartDate");
        selection.TeamField!.RequestKey.Should().Be("division");
        selection.PhoneFields.Select(field => field.RequestKey)
            .Should()
            .Equal("mobilePhone", "workPhone");
        getFieldsCalls.Should().Be(1);
        getEmployeeFieldsCalls.Should().Be(1);
    }

    [Fact(DisplayName = "The resolver falls back to BambooHR metadata when the root employee probe does not contain a manager relationship value.")]
    [Trait("Category", "Unit")]
    public async Task ResolveAsyncShouldFallbackToMetadataWhenProbeDoesNotContainRelationshipField()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var getFieldsCalls = 0;
        var getEmployeeFieldsCalls = 0;
        var fields = new[]
        {
            CreateField("customReportsToId", "Reports To Identifier"),
            CreateField("location", "Location"),
            CreateField("teamName", "Team")
        };

        bambooHrClient.Setup(client => client.GetFieldsAsync(
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getFieldsCalls++)
            .ReturnsAsync(fields);
        bambooHrClient.Setup(client => client.GetEmployeeFieldsAsync(
                It.Is<EmployeeId>(employeeId => employeeId == new EmployeeId(5)),
                It.Is<IReadOnlyCollection<string>>(keys => MatchKeys(
                    keys,
                    "supervisorEId",
                    "supervisorId",
                    "managerId",
                    "managerEId",
                    "reportsToId",
                    "reportsToEId",
                    "reportsTo",
                    "supervisor",
                    "manager")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getEmployeeFieldsCalls++)
            .ReturnsAsync(new EmployeeFieldValues(
                new EmployeeId(5),
                new Dictionary<string, string?>()));

        var resolver = new HierarchyFieldResolver(bambooHrClient.Object);

        // Act
        var selection = await resolver.ResolveAsync(new EmployeeId(5), cts.Token);

        // Assert
        selection.RelationshipField.RequestKey.Should().Be("customReportsToId");
        selection.RelationshipField.DisplayName.Should().Be("Reports To Identifier");
        selection.RelationshipField.UsesEmployeeId.Should().BeTrue();
        selection.LocationField!.RequestKey.Should().Be("location");
        selection.TeamField!.RequestKey.Should().Be("teamName");
        getFieldsCalls.Should().Be(1);
        getEmployeeFieldsCalls.Should().Be(1);
    }

    [Fact(DisplayName = "The resolver throws when no BambooHR relationship field can be resolved.")]
    [Trait("Category", "Unit")]
    public async Task ResolveAsyncShouldThrowWhenRelationshipFieldCannotBeResolved()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var getFieldsCalls = 0;
        var getEmployeeFieldsCalls = 0;

        bambooHrClient.Setup(client => client.GetFieldsAsync(
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getFieldsCalls++)
            .ReturnsAsync(new[] { CreateField("department", "Department") });
        bambooHrClient.Setup(client => client.GetEmployeeFieldsAsync(
                It.Is<EmployeeId>(employeeId => employeeId == new EmployeeId(5)),
                It.Is<IReadOnlyCollection<string>>(keys => MatchKeys(
                    keys,
                    "supervisorEId",
                    "supervisorId",
                    "managerId",
                    "managerEId",
                    "reportsToId",
                    "reportsToEId",
                    "reportsTo",
                    "supervisor",
                    "manager")),
                It.Is<CancellationToken>(token => token == cts.Token)))
            .Callback(() => getEmployeeFieldsCalls++)
            .ReturnsAsync(new EmployeeFieldValues(
                new EmployeeId(5),
                new Dictionary<string, string?>()));

        var resolver = new HierarchyFieldResolver(bambooHrClient.Object);

        // Act
        Func<Task> action = () => resolver.ResolveAsync(new EmployeeId(5), cts.Token);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No BambooHR manager relationship field could be resolved.");
        getFieldsCalls.Should().Be(1);
        getEmployeeFieldsCalls.Should().Be(1);
    }

    private static bool MatchKeys(IReadOnlyCollection<string> keys, params string[] expectedKeys)
        => keys.Count == expectedKeys.Length
            && keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(
                    expectedKeys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

    private static BambooHrField CreateField(string requestKey, string displayName)
        => new(requestKey, displayName, requestKey, "text");
}
