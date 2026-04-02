using FluentAssertions;

namespace Infrastructure.Tests;

[Collection("Console stack")]
public sealed class ConsoleLoadingNotifierTests
{
    [Fact(DisplayName = "The loading notifier executes the action, supports status/progress updates, and clears its task after completion.")]
    [Trait("Category", "Unit")]
#pragma warning disable CA2007
    public async Task RunAsyncShouldExecuteActionAndAllowProgressUpdates()
    {
        // Arrange
        using var scope = new ConsoleStackTestScope();
        var notifier = new ConsoleLoadingNotifier();

        notifier.SetStatus("Ignored before task start");
        notifier.SetProgress("Ignored before task start", 1, 2);

        // Act
        var result = await notifier.RunAsync(
            "Loading hierarchy...",
            async ct =>
            {
                notifier.SetStatus("Fetching BambooHR data...");
                notifier.SetProgress("Loading employees...", -2, 0);
                notifier.SetProgress("Loading employees...", 3, 5);
                await Task.Delay(1, ct);
                return 123;
            },
            CancellationToken.None);

        notifier.SetStatus("Ignored after task completion");
        notifier.SetProgress("Ignored after task completion", 0, 0);

        // Assert
        result.Should().Be(123);
    }
#pragma warning restore CA2007

    [Fact(DisplayName = "The loading notifier rejects blank descriptions and null actions.")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldRejectInvalidArguments()
    {
        // Arrange
        var notifier = new ConsoleLoadingNotifier();

        // Act
        var blankDescription = () => notifier.SetStatus(" ");
        var nullAction = () => notifier.RunAsync<int>("Loading...", null!, CancellationToken.None);

        // Assert
        blankDescription.Should().Throw<ArgumentException>();
        await nullAction.Should().ThrowAsync<ArgumentNullException>();
    }
}
