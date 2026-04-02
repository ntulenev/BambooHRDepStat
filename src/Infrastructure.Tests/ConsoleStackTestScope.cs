using Spectre.Console;
using Spectre.Console.Testing;

namespace Infrastructure.Tests;

internal sealed class ConsoleStackTestScope : IDisposable
{
    public ConsoleStackTestScope()
    {
        _previousConsole = AnsiConsole.Console;
#pragma warning disable CA2000
        _console = new TestConsole().Interactive();
#pragma warning restore CA2000
        AnsiConsole.Console = _console;
    }

    public TestConsole Console => _console;

    public void Dispose()
    {
        AnsiConsole.Console = _previousConsole;
        _console.Dispose();
    }

    private readonly IAnsiConsole _previousConsole;
    private readonly TestConsole _console;
}
