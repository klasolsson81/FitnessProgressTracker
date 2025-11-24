using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System.IO;


namespace FitnessProgressTracker.UI
{
    public class ClientMenu
    {
        // Visar klientens meny
        private readonly ClientService _clientService;
        private readonly ScheduleService _scheduleService;
        private readonly ProgressService _progressService;
        private readonly IDataStore<WorkoutPlan> _workoutStore;
        private readonly IDataStore<DietPlan> _dietStore;

        public ClientMenu(
            ClientService clientService,
            ScheduleService scheduleService,
            ProgressService progressService)
        {
            _clientService = clientService;
            _scheduleService = scheduleService;
            _progressService = progressService;

            try
            {
                
                string baseDirectory = AppContext.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));
                string workoutPath = Path.Combine(projectRoot, "data/workouts.json");
                string dietPath = Path.Combine(projectRoot, "data/diets.json");

                _workoutStore = new JsonDataStore<WorkoutPlan>(workoutPath);
                _dietStore = new JsonDataStore<DietPlan>(dietPath);
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Kunde inte initiera ClientMenu: {ex.Message}");
                throw;
            }
        }



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
                                "🎯 visa mina mål",
                                "📘 Logga träning",
                                "📊 Se framsteg och statistik",
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

                        case "🎯 Visa mina mål":
                            SpectreUIHelper.Loading("Hämtar mål...");
                            AnsiConsole.MarkupLine("[green]Dina mål har uppdaterats![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "📘 Logga träning":
                            SpectreUIHelper.Loading("Loggar dagens träning...");
                            AnsiConsole.MarkupLine("[green]Träning registrerad![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "📊 Se framsteg och statistik":
                            SpectreUIHelper.Loading("Hämtar statistik...");
                            AnsiConsole.MarkupLine("[blue]Här är dina framsteg och statistik![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är utloggad. Bra jobbat idag! 💪");
                            isRunning = false;
                            continue; // hoppa över "tryck för att fortsätta"
                    }

                    // Vänta på att användaren trycker innan menyn visas igen
                    AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i klientmenyn: {ex.Message}");
            }
        }
    }
}
