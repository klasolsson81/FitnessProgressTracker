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
                            LogWorkout(client);
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
       
            private void LogWorkout(Client client)
        {
            try
            {
                // Fråga om datum
                var date = AnsiConsole.Prompt(
                    new TextPrompt<DateTime>("[cyan]Datum (yyyy-MM-dd):[/]")
                        .DefaultValue(DateTime.Now)
                );

                // Fråga om vikt med validering
                var weight = AnsiConsole.Prompt(
                    new TextPrompt<double>("[cyan]Vikt (kg):[/]")
                        .Validate(w => w > 0 && w < 300
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Ogiltig vikt[/]"))
                );

                // Fråga om anteckning
                var notes = AnsiConsole.Ask<string>("[cyan]Anteckning:[/]", string.Empty);

                SpectreUIHelper.Loading("Sparar...");

                // Hämta befintliga loggar för att skapa unikt ID
                var allLogs = _progressService.GetLogsForClient(client.Id)?.ToList() ?? new List<ProgressLog>();

                // Skapa ny logg
                var newLog = new ProgressLog
                {
                    Id = allLogs.Count > 0 ? allLogs.Max(l => l.Id) + 1 : 1,
                    ClientId = client.Id,
                    Date = date,
                    Weight = weight,
                    Notes = notes
                };

                // Spara loggen
                _progressService.AddProgressLog(newLog);

                SpectreUIHelper.Success($"Loggat! Vikt: {weight} kg");
                SpectreUIHelper.Motivation();
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Kunde inte logga träning: {ex.Message}");
            }
        }
    }
}  

