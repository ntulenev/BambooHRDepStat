namespace Infrastructure.Tests;

/// <summary>
/// Test HTTP handler that records outgoing requests and returns queued responses.
/// </summary>
internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// Creates a recording handler with the supplied response queue.
    /// </summary>
    public RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        _responses = new Queue<HttpResponseMessage>(responses);
    }

    /// <summary>
    /// Gets the recorded request snapshots.
    /// </summary>
    public IReadOnlyList<HttpRequestMessageSnapshot> Requests => _requests;

    /// <inheritdoc />
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
