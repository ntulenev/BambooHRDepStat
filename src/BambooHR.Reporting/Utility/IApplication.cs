namespace BambooHR.Reporting.Utility;

/// <summary>
/// Console application entry point.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Runs the application.
    /// </summary>
    Task RunAsync(string[] args, CancellationToken ct);
}
