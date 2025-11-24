using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using Spectre.Console;
using System.Security.Cryptography.X509Certificates;

namespace FitnessProgressTracker.UI
{
    public class ClientMenu
    {
        private readonly ProgressService _progressService;

        public ClientMenu(ProgressService progressService)
        {
            _progressService = progressService;
        }

        // Visar klientens meny
        public void Show(Client client)
        {
            try
            {
                bool isRunning = true;

                while (isRunning)
                {
                    AnsiConsole.Background = Color.Grey15;
                    AnsiConsole.Clear();

                    SpectreUIHelper.AnimatedBanner("CLIENT MODE", Color.Cyan1);

                    AnsiConsole.MarkupLine("[italic green]Stay consistent, stay strong![/]");
                    AnsiConsole.MarkupLine("[dim yellow]Välj vad du vill göra idag.[/]");

                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[bold cyan]Välj ett alternativ:[/]")
                            .AddChoices(
                                "💪 Visa träningsschema",
                                "🥗 Visa kostschema",
                                "🎯 Uppdatera mål",
                                "📘 Logga träning",
                                "📊 Se framsteg och statistik",
                                "💬 Skicka meddelande till PT",
                                "🚪 Logga ut"));

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "💪 Visa träningsschema":
                            SpectreUIHelper.Loading("Hämtar träningsschema...");
                            AnsiConsole.MarkupLine("[green]Ditt träningsschema visas här![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "🥗 Visa kostschema":
                            SpectreUIHelper.Loading("Hämtar kostschema...");
                            AnsiConsole.MarkupLine("[green]Din kostplan visas här![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "🎯 Uppdatera mål":
                            SpectreUIHelper.Loading("Uppdaterar mål...");
                            AnsiConsole.MarkupLine("[green]Dina mål har uppdaterats![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "📘 Logga träning":
                            SpectreUIHelper.Loading("Loggar dagens träning...");
                            AnsiConsole.MarkupLine("[green]Träning registrerad![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "📊 Se framsteg och statistik":
                            ShowProgressStats(client);
                            break;

                        case "💬 Skicka meddelande till PT":
                            SpectreUIHelper.Loading("Skickar meddelande...");
                            AnsiConsole.MarkupLine("[green]Meddelande skickat till din PT![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är utloggad. Bra jobbat idag! 💪");
                            isRunning = false;
                            continue;
                    }

                    AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i klientmenyn: {ex.Message}");
            }
        }
        private void ShowProgressStats(Client client)
        {
            _progressService.ShowClientProgress(client.Id);
            SpectreUIHelper.Motivation();
        }
    }
}
