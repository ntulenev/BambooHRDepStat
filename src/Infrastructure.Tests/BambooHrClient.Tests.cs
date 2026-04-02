using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Abstractions;

using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class BambooHrClientTests
{
    [Fact(DisplayName = "The client configures the base address, JSON accept header, and Basic auth header.")]
    [Trait("Category", "Unit")]
    public void CtorShouldConfigureHttpClientHeaders()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        var settings = new BambooHrConnectionSettings("acme", "secret-token");

        // Act
        _ = new BambooHrClient(httpClient, settings);

        // Assert
        httpClient.BaseAddress.Should().Be(new Uri("https://acme.bamboohr.com"));
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(
            Convert.ToBase64String(Encoding.ASCII.GetBytes("secret-token:x")));
        httpClient.DefaultRequestHeaders.Accept.Should().Contain(header =>
            string.Equals(header.MediaType, "application/json", StringComparison.OrdinalIgnoreCase));
        httpClient.DefaultRequestHeaders.Accept.Should().Contain(header =>
            string.Equals(header.MediaType, "text/plain", StringComparison.OrdinalIgnoreCase));
        httpClient.DefaultRequestHeaders.Accept.Should().HaveCount(2);
    }

    [Fact(DisplayName = "The client does not duplicate the JSON accept header when it is already present.")]
    [Trait("Category", "Unit")]
    public void CtorShouldNotDuplicateJsonAcceptHeader()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var settings = new BambooHrConnectionSettings("acme", "secret-token");

        // Act
        _ = new BambooHrClient(httpClient, settings);

        // Assert
        httpClient.DefaultRequestHeaders.Accept.Should().ContainSingle(header =>
            string.Equals(header.MediaType, "application/json", StringComparison.OrdinalIgnoreCase));
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact(DisplayName = "The client fetches field metadata and parses numeric and string field identifiers.")]
    [Trait("Category", "Unit")]
    public async Task GetFieldsAsyncShouldParseMetadata()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(
            CreateJsonResponse("""
            [
              { "id": 1, "name": "Department", "type": "text" },
              { "id": "officeLocation", "name": "Office Location", "alias": "workLocation", "type": "text" }
            ]
            """));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));

        // Act
        var fields = await client.GetFieldsAsync(CancellationToken.None);

        // Assert
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/meta/fields");
        fields.Should().HaveCount(2);
        fields[0].Id.Should().Be("1");
        fields[0].RequestKey.Should().Be("1");
        fields[0].Name.Should().Be("Department");
        fields[0].Type.Should().Be("text");
        fields[1].Id.Should().Be("officeLocation");
        fields[1].RequestKey.Should().Be("workLocation");
        fields[1].Name.Should().Be("Office Location");
    }

    [Fact(DisplayName = "The client fetches the employee directory and falls back to the preferred name when display name is missing.")]
    [Trait("Category", "Unit")]
    public async Task GetEmployeesAsyncShouldParseDirectoryEntries()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(
            CreateJsonResponse("""
            {
              "employees": [
                {
                  "id": "42",
                  "firstName": "Alice",
                  "lastName": "Smith",
                  "preferredName": "Alice",
                  "displayName": "Alice Smith",
                  "jobTitle": "Director"
                },
                {
                  "id": "7",
                  "firstName": "Bob",
                  "lastName": "Jones",
                  "preferredName": null,
                  "jobTitle": "   "
                }
              ]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));

        // Act
        var employees = await client.GetEmployeesAsync(CancellationToken.None);

        // Assert
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be("/api/v1/employees/directory?onlyCurrent=true");
        employees.Should().HaveCount(2);
        employees[0].EmployeeId.Should().Be(new EmployeeId(42));
        employees[0].DisplayName.Should().Be("Alice Smith");
        employees[0].PreferredName.Should().Be("Alice");
        employees[0].JobTitle.Should().Be("Director");
        employees[0].Status.Should().Be("Active");
        employees[1].EmployeeId.Should().Be(new EmployeeId(7));
        employees[1].DisplayName.Should().Be("Bob Jones");
        employees[1].PreferredName.Should().Be("Bob");
        employees[1].JobTitle.Should().BeNull();
        employees[1].Status.Should().Be("Active");
    }

    [Fact(DisplayName = "The client fetches dynamic employee fields and converts JSON value kinds to strings consistently.")]
    [Trait("Category", "Unit")]
    public async Task GetEmployeeFieldsAsyncShouldParseDynamicValues()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(
            CreateJsonResponse("""
            {
              "department": "Engineering",
              "jobTitle": "Senior Engineer",
              "workEmail": null,
              "bonus": 42,
              "isManager": true,
              "isRemote": false,
              "metadata": { "city": "Berlin" },
              "tags": ["platform", "backend"]
            }
            """));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));
        var fieldKeys = new[] { "department", "JobTitle", "jobtitle", " ", "workEmail" };

        // Act
        var values = await client.GetEmployeeFieldsAsync(new EmployeeId(42), fieldKeys, CancellationToken.None);

        // Assert
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be(
            "/api/v1/employees/42?fields=department%2CJobTitle%2CworkEmail");
        values.EmployeeId.Should().Be(new EmployeeId(42));
        values.Values.Should().ContainKey("department");
        values.Values["DEPARTMENT"].Should().Be("Engineering");
        values.Values["jobTitle"].Should().Be("Senior Engineer");
        values.Values["workEmail"].Should().BeNull();
        values.Values["bonus"].Should().Be("42");
        values.Values["isManager"].Should().Be(bool.TrueString);
        values.Values["isRemote"].Should().Be(bool.FalseString);
        values.Values["metadata"].Should().Be("""{ "city": "Berlin" }""");
        values.Values["tags"].Should().Be("""["platform", "backend"]""");
    }

    [Fact(DisplayName = "The client fetches who's out entries and parses holidays, employee identifiers, and date ranges.")]
    [Trait("Category", "Unit")]
    public async Task GetWhosOutAsyncShouldParseEntries()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(
            CreateJsonResponse("""
            [
              {
                "id": 100,
                "type": "holiday",
                "name": "Founders Day",
                "start": "2026-04-03",
                "end": "2026-04-03"
              },
              {
                "id": 101,
                "type": "time_off",
                "employeeId": 7,
                "name": "Bob Jones",
                "start": "2026-04-04",
                "end": "2026-04-06"
              },
              {
                "id": 102,
                "type": "vacation",
                "employeeId": "9",
                "name": "Carol Brown",
                "start": "2026-04-07",
                "end": "2026-04-08"
              }
            ]
            """));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));

        // Act
        var entries = await client.GetWhosOutAsync(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 8),
            CancellationToken.None);

        // Assert
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.PathAndQuery.Should().Be(
            "/api/v1/time_off/whos_out?start=2026-04-01&end=2026-04-08&filter=off");
        entries.Should().HaveCount(3);
        entries[0].Type.Should().Be(TimeOffEntryType.Holiday);
        entries[0].EmployeeId.Should().BeNull();
        entries[0].Name.Should().Be("Founders Day");
        entries[0].Start.Should().Be(new DateOnly(2026, 4, 3));
        entries[0].End.Should().Be(new DateOnly(2026, 4, 3));
        entries[1].Type.Should().Be(TimeOffEntryType.TimeOff);
        entries[1].EmployeeId.Should().Be(new EmployeeId(7));
        entries[1].Name.Should().Be("Bob Jones");
        entries[1].Start.Should().Be(new DateOnly(2026, 4, 4));
        entries[1].End.Should().Be(new DateOnly(2026, 4, 6));
        entries[2].Type.Should().Be(TimeOffEntryType.TimeOff);
        entries[2].EmployeeId.Should().Be(new EmployeeId(9));
        entries[2].Name.Should().Be("Carol Brown");
        entries[2].Start.Should().Be(new DateOnly(2026, 4, 7));
        entries[2].End.Should().Be(new DateOnly(2026, 4, 8));
    }

    [Fact(DisplayName = "The client retries transient failures before returning a successful result.")]
    [Trait("Category", "Unit")]
    public async Task GetFieldsAsyncShouldRetryTransientFailures()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(
            CreateResponse(HttpStatusCode.ServiceUnavailable),
            CreateJsonResponse("""[{ "id": 1, "name": "Department", "type": "text" }]"""));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));

        // Act
        var fields = await client.GetFieldsAsync(CancellationToken.None);

        // Assert
        handler.Requests.Should().HaveCount(2);
        fields.Should().ContainSingle(field => field.Name == "Department");
    }

    [Fact(DisplayName = "The client does not retry non-transient failures.")]
    [Trait("Category", "Unit")]
    public async Task GetFieldsAsyncShouldNotRetryOnClientErrors()
    {
        // Arrange
        using var handler = new RecordingHttpMessageHandler(CreateResponse(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var client = new BambooHrClient(httpClient, new BambooHrConnectionSettings("acme", "secret-token"));

        // Act
        Func<Task> action = () => client.GetFieldsAsync(CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<HttpRequestException>();
        handler.Requests.Should().ContainSingle();
    }

    private static HttpResponseMessage CreateJsonResponse(string content)
        => CreateResponse(HttpStatusCode.OK, content);

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string? content = null)
        => new(statusCode)
        {
            Content = content is null
                ? null
                : new StringContent(content, Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public IReadOnlyList<HttpRequestMessageSnapshot> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requests.Add(new HttpRequestMessageSnapshot(request.Method, request.RequestUri));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response configured for the request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }

        private readonly Queue<HttpResponseMessage> _responses;
        private readonly List<HttpRequestMessageSnapshot> _requests = [];
    }

    private sealed record HttpRequestMessageSnapshot(HttpMethod Method, Uri? RequestUri);
}
