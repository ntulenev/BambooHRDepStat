namespace Abstractions;

/// <summary>
/// Reports interactive loading progress to the current UI.
/// </summary>
public interface ILoadingNotifier
{
    /// <summary>
    /// Runs an operation with interactive loading feedback enabled.
    /// </summary>
    Task<T> RunAsync<T>(
        string description,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct);

    /// <summary>
    /// Sets the current loading status.
    /// </summary>
    void SetStatus(string description);

    /// <summary>
    /// Updates a determinate loading progress state.
    /// </summary>
    void SetProgress(string description, int completed, int total);
}
