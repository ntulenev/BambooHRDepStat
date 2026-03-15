using Abstractions;

using Spectre.Console;

namespace Infrastructure;

/// <summary>
/// Renders interactive loading progress in the console.
/// </summary>
public sealed class ConsoleLoadingNotifier : ILoadingNotifier
{
    private readonly Lock _sync = new();
    private ProgressTask? _task;

    /// <inheritdoc/>
    public Task<T> RunAsync<T>(
        string description,
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(action);

        return AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async context =>
            {
                var task = context.AddTask(description, autoStart: true, maxValue: 100);
                task.IsIndeterminate = true;

                using var scope = new TaskScope(this, task);
                try
                {
                    return await action(ct).ConfigureAwait(false);
                }
                finally
                {
                    lock (_sync)
                    {
                        if (_task is not null)
                        {
                            _task.Description = "[green]Completed[/]";
                            _task.IsIndeterminate = false;
                            _task.Value = _task.MaxValue;
                            _task.StopTask();
                        }
                    }
                }
            });
    }

    /// <inheritdoc/>
    public void SetStatus(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        lock (_sync)
        {
            if (_task is null)
            {
                return;
            }

            _task.Description = description;
            _task.IsIndeterminate = true;
        }
    }

    /// <inheritdoc/>
    public void SetProgress(string description, int completed, int total)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        lock (_sync)
        {
            if (_task is null)
            {
                return;
            }

            _task.Description = description;
            if (total <= 0)
            {
                _task.IsIndeterminate = true;
                return;
            }

            _task.IsIndeterminate = false;
            _task.MaxValue = total;
            _task.Value = Math.Min(Math.Max(completed, 0), total);
        }
    }

    private sealed class TaskScope : IDisposable
    {
        private readonly ConsoleLoadingNotifier _owner;

        public TaskScope(ConsoleLoadingNotifier owner, ProgressTask task)
        {
            _owner = owner;
            lock (_owner._sync)
            {
                _owner._task = task;
            }
        }

        public void Dispose()
        {
            lock (_owner._sync)
            {
                _owner._task = null;
            }
        }
    }
}
