using System.Collections.Concurrent;

using Abstractions;

using FluentAssertions;

using Moq;

using Models;

namespace Logic.Tests;

public sealed class EmployeeProfileDirectoryLoaderTests
{
    [Fact(DisplayName = "The constructor throws when the BambooHR client is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenBambooHrClientIsNull()
    {
        // Arrange
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict).Object;

        // Act
        var action = () => new EmployeeProfileDirectoryLoader(null!, loadingNotifier);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the loading notifier is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenLoadingNotifierIsNull()
    {
        // Arrange
        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict).Object;

        // Act
        var action = () => new EmployeeProfileDirectoryLoader(bambooHrClient, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The loader throws when the employee list is null.")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncShouldThrowWhenEmployeesAreNull()
    {
        // Arrange
        var loader = CreateLoader();

        // Act
        Func<Task> action = () => loader.LoadAsync(
            null!,
            CreateRelationshipField(),
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "The loader throws when the relationship field is null.")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncShouldThrowWhenRelationshipFieldIsNull()
    {
        // Arrange
        var loader = CreateLoader();

        // Act
        Func<Task> action = () => loader.LoadAsync(
            [],
            null!,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "The loader requests the expected BambooHR fields and returns profiles in employee order.")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncShouldReturnProfilesSortedByEmployeeIdAndReportProgress()
    {
        // Arrange
        var expectedRequestKeys =
            new[]
            {
                "department",
                "jobTitle",
                "workEmail",
                "reportsTo",
                "workLocation",
                "country",
                "officeCity",
                "dob",
                "employmentStartDate"
            };
        using var cts = new CancellationTokenSource();
        var employees = CreateEmployees();
        var relationshipField = CreateRelationshipField();
        var bambooHrClient = new Mock<IBambooHrClient>(MockBehavior.Strict);
        var loadingNotifier = new Mock<ILoadingNotifier>(MockBehavior.Strict);
        var requestedKeysByEmployeeId = new ConcurrentDictionary<EmployeeId, IReadOnlyCollection<string>>();
        var requestTokensByEmployeeId = new ConcurrentDictionary<EmployeeId, CancellationToken>();
        var getEmployeeFieldsCallsByEmployeeId = new ConcurrentDictionary<EmployeeId, int>();
        var progressUpdates = new ConcurrentBag<(string Description, int Completed, int Total)>();

        bambooHrClient.Setup(client => client.GetEmployeeFieldsAsync(
                It.Is<EmployeeId>(employeeId => employeeId == new EmployeeId(1)),
                It.Is<IReadOnlyCollection<string>>(keys => keys.Count == expectedRequestKeys.Length
                    && expectedRequestKeys.All(keys.Contains)),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback<EmployeeId, IReadOnlyCollection<string>, CancellationToken>((employeeId, keys, token) =>
            {
                requestedKeysByEmployeeId[employeeId] = keys;
                requestTokensByEmployeeId[employeeId] = token;
                getEmployeeFieldsCallsByEmployeeId.AddOrUpdate(employeeId, 1, (_, current) => current + 1);
            })
            .ReturnsAsync(CreateEmployeeOneFieldValues());
        bambooHrClient.Setup(client => client.GetEmployeeFieldsAsync(
                It.Is<EmployeeId>(employeeId => employeeId == new EmployeeId(2)),
                It.Is<IReadOnlyCollection<string>>(keys => keys.Count == expectedRequestKeys.Length
                    && expectedRequestKeys.All(keys.Contains)),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback<EmployeeId, IReadOnlyCollection<string>, CancellationToken>((employeeId, keys, token) =>
            {
                requestedKeysByEmployeeId[employeeId] = keys;
                requestTokensByEmployeeId[employeeId] = token;
                getEmployeeFieldsCallsByEmployeeId.AddOrUpdate(employeeId, 1, (_, current) => current + 1);
            })
            .ReturnsAsync(CreateEmployeeTwoFieldValues());
        loadingNotifier.Setup(notifier => notifier.SetProgress(
                It.Is<string>(description => description == "Loading employee profiles (1/2)..."
                    || description == "Loading employee profiles (2/2)..."),
                It.Is<int>(completed => completed == 1 || completed == 2),
                It.Is<int>(total => total == 2)))
            .Callback<string, int, int>((description, completed, total) =>
                progressUpdates.Add((description, completed, total)));

        var loader = new EmployeeProfileDirectoryLoader(bambooHrClient.Object, loadingNotifier.Object);

        // Act
        var profiles = await loader.LoadAsync(
            employees,
            relationshipField,
            locationField: new BambooHrField("workLocation", "Work Location", null, "text"),
            countryField: new BambooHrField("country", "Country", null, "text"),
            cityField: new BambooHrField("officeCity", "Office City", null, "text"),
            birthDateField: new BambooHrField("dob", "Date of Birth", null, "date"),
            hireDateField: new BambooHrField("employmentStartDate", "Employment Start Date", null, "date"),
            cts.Token);

        // Assert
        profiles.Should().HaveCount(2);
        profiles.Select(profile => profile.EmployeeId)
            .Should()
            .Equal(new[] { new EmployeeId(1), new EmployeeId(2) });

        profiles[0].DisplayName.Should().Be("Alice Smith");
        profiles[0].JobTitle.Should().Be("Director Override");
        profiles[0].DateOfBirth.Should().Be(new DateOnly(1980, 1, 10));
        profiles[0].HireDate.Should().Be(new DateOnly(2012, 2, 1));
        profiles[0].ManagerLookupValue.Should().Be("Alice Manager");

        profiles[1].DisplayName.Should().Be("Bob Jones");
        profiles[1].JobTitle.Should().Be("Manager");
        profiles[1].DateOfBirth.Should().Be(new DateOnly(2026, 3, 12));
        profiles[1].HireDate.Should().BeNull();
        profiles[1].ManagerLookupValue.Should().Be("Alice Manager");
        requestedKeysByEmployeeId.Should().HaveCount(2);
        requestTokensByEmployeeId.Should().HaveCount(2);
        requestedKeysByEmployeeId[new EmployeeId(1)]
            .Should()
            .BeEquivalentTo(expectedRequestKeys);
        requestedKeysByEmployeeId[new EmployeeId(2)]
            .Should()
            .BeEquivalentTo(expectedRequestKeys);
        requestTokensByEmployeeId[new EmployeeId(1)].CanBeCanceled.Should().BeTrue();
        requestTokensByEmployeeId[new EmployeeId(2)].CanBeCanceled.Should().BeTrue();
        requestTokensByEmployeeId[new EmployeeId(1)].Should().NotBe(CancellationToken.None);
        requestTokensByEmployeeId[new EmployeeId(2)].Should().NotBe(CancellationToken.None);
        getEmployeeFieldsCallsByEmployeeId[new EmployeeId(1)].Should().Be(1);
        getEmployeeFieldsCallsByEmployeeId[new EmployeeId(2)].Should().Be(1);
        progressUpdates.Should().HaveCount(2);
        progressUpdates.Should().Contain(update =>
            update.Description == "Loading employee profiles (1/2)..."
            && update.Completed == 1
            && update.Total == 2);
        progressUpdates.Should().Contain(update =>
            update.Description == "Loading employee profiles (2/2)..."
            && update.Completed == 2
            && update.Total == 2);
    }

    private static EmployeeProfileDirectoryLoader CreateLoader()
        => new(
            new Mock<IBambooHrClient>(MockBehavior.Strict).Object,
            new Mock<ILoadingNotifier>(MockBehavior.Strict).Object);

    private static HierarchyRelationshipField CreateRelationshipField()
        => new("reportsTo", "Reports To", usesEmployeeId: false);

    private static IReadOnlyList<BasicEmployee> CreateEmployees()
        => [
            new BasicEmployee(new EmployeeId(2), "Bob Jones", "Bob", "Jones", "Bob", "Manager", "Active"),
            new BasicEmployee(new EmployeeId(1), "Alice Smith", "Alice", "Smith", "Alice", "Director", "Active")
        ];

    private static EmployeeFieldValues CreateEmployeeOneFieldValues()
        => new(
            new EmployeeId(1),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["department"] = "Leadership",
                ["jobTitle"] = "Director Override",
                ["workEmail"] = "alice.smith@example.com",
                ["reportsTo"] = "Alice Manager",
                ["workLocation"] = "Berlin, Germany",
                ["country"] = "Germany",
                ["officeCity"] = "Berlin",
                ["dob"] = "1980-01-10",
                ["employmentStartDate"] = "2012-02-01"
            });

    private static EmployeeFieldValues CreateEmployeeTwoFieldValues()
        => new(
            new EmployeeId(2),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["department"] = "Engineering",
                ["workEmail"] = "bob.jones@example.com",
                ["reportsTo"] = "Alice Manager",
                ["workLocation"] = "Munich, Germany",
                ["country"] = "Germany",
                ["officeCity"] = "Munich",
                ["dob"] = "2026-03-12 08:30:00"
            });

}

