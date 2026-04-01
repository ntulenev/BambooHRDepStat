namespace Abstractions;

/// <summary>
/// Immutable BambooHR connection settings used by infrastructure services.
/// </summary>
public sealed class BambooHrConnectionSettings
{
    /// <summary>
    /// Creates connection settings.
    /// </summary>
    public BambooHrConnectionSettings(string organization, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        Organization = organization;
        Token = token;
    }

    /// <summary>
    /// Gets BambooHR company subdomain.
    /// </summary>
    public string Organization { get; }

    /// <summary>
    /// Gets BambooHR API token.
    /// </summary>
    public string Token { get; }
}
