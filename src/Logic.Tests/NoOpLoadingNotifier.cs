using Abstractions;

namespace Logic.Tests;

internal sealed class NoOpLoadingNotifier : ILoadingNotifier
{
    public Task<T> RunAsync<T>(
        string description,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct)
    {
        _ = description;
        return action(ct);
    }

    public void SetProgress(string description, int completed, int total)
    {
        _ = description;
        _ = completed;
        _ = total;
    }

    public void SetStatus(string description)
    {
        _ = description;
    }
}
