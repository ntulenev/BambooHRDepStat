namespace Models;

/// <summary>
/// BambooHR employee field metadata.
/// </summary>
public sealed class BambooHrField
{
    /// <summary>
    /// Creates field metadata.
    /// </summary>
    public BambooHrField(string id, string name, string? alias, string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id;
        Name = name;
        Alias = string.IsNullOrWhiteSpace(alias) ? null : alias;
        Type = type;
    }

    /// <summary>
    /// Gets BambooHR field identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets field display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets API alias when available.
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// Gets BambooHR field data type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets request key used in BambooHR API calls.
    /// </summary>
    public string RequestKey => Alias ?? Id;
}
