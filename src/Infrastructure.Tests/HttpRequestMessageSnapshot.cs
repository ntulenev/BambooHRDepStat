namespace Infrastructure.Tests;

/// <summary>
/// Captures the essential parts of an outgoing HTTP request for assertions.
/// </summary>
internal sealed record HttpRequestMessageSnapshot(HttpMethod Method, Uri? RequestUri);
