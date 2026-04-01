using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// BambooHR REST API client.
/// </summary>
public sealed class BambooHrClient : IBambooHrClient
{
    /// <summary>
    /// Creates configured BambooHR client.
    /// </summary>
    public BambooHrClient(HttpClient httpClient, BambooHrConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(
            $"https://{settings.Organization}.bamboohr.com",
            UriKind.Absolute);

        if (_httpClient.DefaultRequestHeaders.Accept.All(
                header => !string.Equals(
                    header.MediaType,
                    "application/json",
                    StringComparison.OrdinalIgnoreCase)))
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Token}:x")));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BambooHrField>> GetFieldsAsync(CancellationToken ct)
    {
        using var response = await SendWithRetryAsync(
                CreateRelativeUri("/api/v1/meta/fields"),
                ct)
            .ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(content);

        return [
            .. document.RootElement.EnumerateArray()
            .Select(ParseField)
            .Where(field => !string.IsNullOrWhiteSpace(field.RequestKey))
        ];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BasicEmployee>> GetEmployeesAsync(CancellationToken ct)
    {
        using var response = await SendWithRetryAsync(
                CreateRelativeUri("/api/v1/employees/directory?onlyCurrent=true"),
                ct)
            .ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(content);

        return [
            .. document.RootElement
                .GetProperty("employees")
                .EnumerateArray()
                .Select(ParseDirectoryEmployee)
        ];
    }

    /// <inheritdoc/>
    public async Task<EmployeeFieldValues> GetEmployeeFieldsAsync(
        EmployeeId employeeId,
        IReadOnlyCollection<string> fieldKeys,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fieldKeys);

        var requestedFields = string.Join(
            ",",
            fieldKeys
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        var url = $"/api/v1/employees/{employeeId.Value}?fields={Uri.EscapeDataString(requestedFields)}";
        using var response = await SendWithRetryAsync(CreateRelativeUri(url), ct)
            .ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(content);

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = ReadValue(property.Value);
        }

        return new EmployeeFieldValues(employeeId, values);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TimeOffEntry>> GetWhosOutAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        var url = $"/api/v1/time_off/whos_out?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}&filter=off";
        using var response = await SendWithRetryAsync(CreateRelativeUri(url), ct)
            .ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(content);

        return [.. document.RootElement.EnumerateArray().Select(ParseWhosOutEntry)];
    }

    private static BambooHrField ParseField(JsonElement element)
    {
        var id = element.GetProperty("id").ValueKind switch
        {
            JsonValueKind.Number => element.GetProperty("id").GetInt32().ToString(
                CultureInfo.InvariantCulture),
            JsonValueKind.String => element.GetProperty("id").GetString(),
            JsonValueKind.False => null,
            JsonValueKind.True => null,
            JsonValueKind.Null => null,
            JsonValueKind.Object => null,
            JsonValueKind.Array => null,
            JsonValueKind.Undefined => null,
            _ => null
        };

        return new BambooHrField(
            id ?? throw new InvalidOperationException("BambooHR field id is missing."),
            element.GetProperty("name").GetString()
                ?? throw new InvalidOperationException("BambooHR field name is missing."),
            element.TryGetProperty("alias", out var aliasElement)
                ? aliasElement.GetString()
                : null,
            element.GetProperty("type").GetString()
                ?? throw new InvalidOperationException("BambooHR field type is missing."));
    }

    private static BasicEmployee ParseDirectoryEmployee(JsonElement element)
    {
        var employeeId = new EmployeeId(int.Parse(
            element.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Employee ID is missing."),
            CultureInfo.InvariantCulture));

        var firstName = element.GetProperty("firstName").GetString()
            ?? throw new InvalidOperationException("Employee first name is missing.");
        var lastName = element.GetProperty("lastName").GetString()
            ?? throw new InvalidOperationException("Employee last name is missing.");
        var preferredName = element.GetProperty("preferredName").GetString() ?? firstName;
        var displayName = element.TryGetProperty("displayName", out var displayNameElement)
            && !string.IsNullOrWhiteSpace(displayNameElement.GetString())
                ? displayNameElement.GetString()!
                : string.IsNullOrWhiteSpace(preferredName)
                    ? $"{firstName} {lastName}"
                    : $"{preferredName} {lastName}";

        return new BasicEmployee(
            employeeId,
            displayName,
            firstName,
            lastName,
            preferredName,
            element.TryGetProperty("jobTitle", out var jobTitleElement)
                ? jobTitleElement.GetString()
                : null,
            "Active");
    }

    private static string? ReadValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Object => element.GetRawText(),
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static TimeOffEntry ParseWhosOutEntry(JsonElement element)
    {
        var typeValue = element.GetProperty("type").GetString();
        var type = string.Equals(typeValue, "holiday", StringComparison.OrdinalIgnoreCase)
            ? TimeOffEntryType.Holiday
            : TimeOffEntryType.TimeOff;

        return new TimeOffEntry(
            element.GetProperty("id").GetInt32(),
            type,
            element.TryGetProperty("employeeId", out var employeeIdElement)
                ? ParseEmployeeId(employeeIdElement)
                : null,
            element.GetProperty("name").GetString() ?? string.Empty,
            DateOnly.Parse(
                element.GetProperty("start").GetString() ?? string.Empty,
                CultureInfo.InvariantCulture),
            DateOnly.Parse(
                element.GetProperty("end").GetString() ?? string.Empty,
                CultureInfo.InvariantCulture));
    }

    private static Uri CreateRelativeUri(string path)
    {
        return new Uri(path, UriKind.Relative);
    }

    private static EmployeeId? ParseEmployeeId(JsonElement employeeIdElement)
    {
        return employeeIdElement.ValueKind switch
        {
            JsonValueKind.Number => new EmployeeId(employeeIdElement.GetInt32()),
            JsonValueKind.String => EmployeeId.TryParse(employeeIdElement.GetString(), out var employeeId)
                ? employeeId
                : null,
            JsonValueKind.False => null,
            JsonValueKind.Null => null,
            JsonValueKind.Object => null,
            JsonValueKind.Array => null,
            JsonValueKind.True => null,
            JsonValueKind.Undefined => null,
            _ => null
        };
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Uri uri, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await _httpClient.GetAsync(uri, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (!ShouldRetry(response.StatusCode) || attempt >= RetryDelays.Length)
            {
                _ = response.EnsureSuccessStatusCode();
            }

            response.Dispose();
            await Task.Delay(RetryDelays[attempt], ct).ConfigureAwait(false);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.GatewayTimeout;
    }

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(13)
    ];

    private readonly HttpClient _httpClient;
}
