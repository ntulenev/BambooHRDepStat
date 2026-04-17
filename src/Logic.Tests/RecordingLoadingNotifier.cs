using Abstractions;

namespace Logic.Tests;

/// <summary>
/// Test notifier that records status messages while executing actions inline.
/// </summary>
internal sealed class RecordingLoadingNotifier : ILoadingNotifier
{
    /// <summary>
    /// Gets the captured status messages.
    /// </summary>
    public List<string> Statuses { get; } = [];

    /// <inheritdoc />
    public Task<T> RunAsync<T>(
        string description,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct)
        => action(ct);

    /// <inheritdoc />
    public void SetStatus(string description) => Statuses.Add(description);

    /// <inheritdoc />
    public void SetProgress(string description, int completed, int total)
    {
        _ = description;
        _ = completed;
        _ = total;
    }
}
