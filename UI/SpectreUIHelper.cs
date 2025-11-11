using Spectre.Console;

namespace YourAppName.Client
{
    public static class SpectreUI
    {
        public static void Success(string message) => AnsiConsole.MarkupLine($"[green]{message}[/]");
        public static void Error(string message) => AnsiConsole.MarkupLine($"[red]{message}[/]");
        public static void Loading(string message) => AnsiConsole.MarkupLine($"[yellow]{message}[/]");
    }
}
