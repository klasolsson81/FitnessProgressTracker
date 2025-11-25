using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FitnessProgressTracker.UI
{
    public class ClientMenu
    {
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

            string baseDirectory = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));
            string workoutPath = Path.Combine(projectRoot, "data/workouts.json");
            string dietPath = Path.Combine(projectRoot, "data/diets.json");

            _workoutStore = new JsonDataStore<WorkoutPlan>(workoutPath);
            _dietStore = new JsonDataStore<DietPlan>(dietPath);
        }

        public void Show(Client client)
        {
            try
            {
                bool isRunning = true;

                while (isRunning)
                {
                    Client? freshClient = _clientService.GetClientById(client.Id);
                    if (freshClient == null) { SpectreUIHelper.Error("Kunde inte hämta dina uppgifter."); return; }

                    AnsiConsole.Background = Color.Grey15;
                    AnsiConsole.Clear();
                    SpectreUIHelper.AnimatedBanner("CLIENT MODE", Color.Cyan1);
                    AnsiConsole.MarkupLine($"[dim yellow]Välkommen {freshClient.FirstName}! Välj vad du vill göra idag.[/]");

                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[bold cyan]Välj ett alternativ:[/]")
                            .AddChoices(
                                "💪 Visa träningsschema",
                                "🥗 Visa kostschema",
                                "🎯 Visa mina mål",
                                "✅ Logga träning (Bocka av pass)",
                                "⚖️ Uppdatera vikt",
                                "📊 Se framsteg och statistik",
                                "🚪 Logga ut"));

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "💪 Visa träningsschema": ShowWorkoutScheduleHub(freshClient); break;
                        case "🥗 Visa kostschema": ShowDietScheduleHub(freshClient); break;
                        case "🎯 Visa mina mål": ShowClientGoals(freshClient); break;
                        case "✅ Logga träning (Bocka av pass)": LogWorkoutActivity(freshClient); break;
                        case "⚖️ Uppdatera vikt": LogWeight(freshClient); break;

                        case "📊 Se framsteg och statistik":
                            SpectreUIHelper.Loading("Hämtar statistik...");
                            var logs = _progressService.GetLogsForClient(freshClient.Id);
                            var stats = _scheduleService.GetWorkoutStatistics(freshClient.Id);
                            SpectreUIHelper.ShowDashboardVisuals(freshClient, logs, stats);
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är utloggad. Bra jobbat idag! 💪");
                            isRunning = false;
                            continue;
                    }
                }
            }
            catch (Exception ex) { SpectreUIHelper.Error($"Ett fel uppstod i klientmenyn: {ex.Message}"); Console.ReadKey(true); }
        }

        private void LogWeight(Client client)
        {
            try
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner("UPPDATERA VIKT", Color.Blue);
                double currentWeight = AnsiConsole.Prompt(new TextPrompt<double>("[cyan]Vikt (kg):[/]").Validate(w => w > 30 && w < 300 ? ValidationResult.Success() : ValidationResult.Error("Orimlig vikt")));
                string note = AnsiConsole.Prompt(new TextPrompt<string>("[cyan]Anteckning:[/]").AllowEmpty().DefaultValue(""));
                _progressService.AddProgressLog(new ProgressLog { ClientId = client.Id, Date = DateTime.Now, Weight = currentWeight, Notes = string.IsNullOrWhiteSpace(note) ? "Viktuppdatering" : note });
                SpectreUIHelper.Success($"Ny vikt på {currentWeight} kg sparad!");
                Thread.Sleep(1500);
            }
            catch (Exception ex) { SpectreUIHelper.Error($"Fel: {ex.Message}"); Console.ReadKey(true); }
        }

        private void ShowClientGoals(Client client)
        {
            Client? freshClient = _clientService.GetClientById(client.Id);
            if (freshClient == null) return;
            var logs = _progressService.GetLogsForClient(client.Id);
            string currentWeight = "Ingen logg";
            if (logs != null && logs.Any())
            {
                var latestLog = logs.OrderByDescending(l => l.Date).FirstOrDefault(l => l.Weight > 0);
                if (latestLog != null) currentWeight = $"{latestLog.Weight} kg";
            }

            AnsiConsole.Clear();
            SpectreUIHelper.AnimatedBanner("DINA MÅL", Color.Green);
            var goalTable = new Table().Border(TableBorder.Rounded).AddColumn(new TableColumn("[bold cyan]Område[/]").Centered()).AddColumn(new TableColumn("[bold yellow]Målvärde[/]").LeftAligned());
            goalTable.AddRow("[cyan]Målbeskrivning[/]", freshClient.GoalDescription ?? "[grey]Ej angivet[/]");
            goalTable.AddRow("[cyan]Målvikt[/]", $"[yellow]{freshClient.TargetWeight} kg[/]");
            goalTable.AddRow("[cyan]Aktuell vikt[/]", $"[green]{currentWeight}[/]");
            goalTable.AddRow("[cyan]Träningspass per vecka[/]", $"[yellow]{freshClient.WorkoutsPerWeek} pass[/]");
            goalTable.AddRow("[cyan]Dagligt kalorimål[/]", $"[yellow]{freshClient.TargetCalories} kcal[/]");
            AnsiConsole.Write(goalTable);
            SpectreUIHelper.Motivation();
            WaitForKey();
        }

        private void ShowWorkoutScheduleHub(Client client)
        {
            List<WorkoutPlan> allWorkouts = _workoutStore.Load();
            var clientWorkouts = allWorkouts.Where(w => w.ClientId == client.Id).OrderByDescending(w => w.Week).ToList();
            if (clientWorkouts.Count == 0) { AnsiConsole.MarkupLine("[yellow]Inget schema.[/]"); WaitForKey(); return; }

            bool inMenu = true;
            while (inMenu)
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner("TRÄNINGSSCHEMA", Color.Blue);
                var currentPlan = clientWorkouts.First();
                AnsiConsole.MarkupLine($"[bold green]Aktuellt schema: {currentPlan.Name} (Vecka {currentPlan.Week})[/]\n");

                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Val:").AddChoices("📅 Visa aktuellt schema", "📂 Se äldre scheman", "↩️ Gå tillbaka"));

                switch (choice)
                {
                    case "📅 Visa aktuellt schema": DisplayWorkoutPlan(currentPlan); WaitForKey(); break;
                    case "📂 Se äldre scheman":
                        var historyChoices = new List<WorkoutPlan>(clientWorkouts);
                        historyChoices.Add(new WorkoutPlan { Id = -1, Name = "↩️ Gå tillbaka" });
                        var selected = AnsiConsole.Prompt(new SelectionPrompt<WorkoutPlan>().Title("Välj schema:").AddChoices(historyChoices).UseConverter(w => w.Id == -1 ? $"[yellow]{w.Name}[/]" : $"Vecka {w.Week}: {w.Name}"));
                        if (selected.Id != -1) { DisplayWorkoutPlan(selected); WaitForKey(); }
                        break;
                    case "↩️ Gå tillbaka": inMenu = false; break;
                }
            }
        }

        private void ShowDietScheduleHub(Client client)
        {
            List<DietPlan> allDiets = _dietStore.Load();
            var clientDiets = allDiets.Where(d => d.ClientId == client.Id).OrderByDescending(d => d.Week).ToList();
            if (clientDiets.Count == 0) { AnsiConsole.MarkupLine("[yellow]Inget kostschema.[/]"); WaitForKey(); return; }

            bool inMenu = true;
            while (inMenu)
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner("KOSTSCHEMA", Color.Green);
                var currentPlan = clientDiets.First();
                AnsiConsole.MarkupLine($"[bold green]Aktuellt schema: {currentPlan.Name} (Vecka {currentPlan.Week})[/]\n");

                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Val:").AddChoices("🍎 Visa aktuellt schema", "📂 Se äldre scheman", "↩️ Gå tillbaka"));

                switch (choice)
                {
                    case "🍎 Visa aktuellt schema": DisplayDietPlan(currentPlan); WaitForKey(); break;
                    case "📂 Se äldre scheman":
                        var historyChoices = new List<DietPlan>(clientDiets);
                        historyChoices.Add(new DietPlan { Id = -1, Name = "↩️ Gå tillbaka" });
                        var selected = AnsiConsole.Prompt(new SelectionPrompt<DietPlan>().Title("Välj schema:").AddChoices(historyChoices).UseConverter(d => d.Id == -1 ? $"[yellow]{d.Name}[/]" : $"Vecka {d.Week}: {d.Name}"));
                        if (selected.Id != -1) { DisplayDietPlan(selected); WaitForKey(); }
                        break;
                    case "↩️ Gå tillbaka": inMenu = false; break;
                }
            }
        }

        private void LogWorkoutActivity(Client client)
        {
            try
            {
                var allWorkouts = _workoutStore.Load();
                var clientWorkouts = allWorkouts.Where(w => w.ClientId == client.Id).OrderByDescending(w => w.Week).ToList();
                if (clientWorkouts.Count == 0) { AnsiConsole.MarkupLine("[red]Inga scheman.[/]"); WaitForKey(); return; }

                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner("LOGGA TRÄNING", Color.Blue);
                var schemaChoices = new List<WorkoutPlan>(clientWorkouts);
                schemaChoices.Add(new WorkoutPlan { Id = -1, Name = "↩️ Gå tillbaka" });
                var selectedPlan = AnsiConsole.Prompt(new SelectionPrompt<WorkoutPlan>().Title("[cyan]Vilken vecka?[/]").AddChoices(schemaChoices).UseConverter(w => w.Id == -1 ? $"[yellow]{w.Name}[/]" : $"Vecka {w.Week} ({w.Name})"));
                if (selectedPlan.Id == -1) return;

                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner($"VECKA {selectedPlan.Week}", Color.Blue);
                var availableDays = selectedPlan.DailyWorkouts.ToList();
                var dayChoices = new List<DailyWorkout>(availableDays);
                dayChoices.Add(new DailyWorkout { Day = "↩️ Gå tillbaka" });
                var selectedDay = AnsiConsole.Prompt(new SelectionPrompt<DailyWorkout>().Title("Vilket pass?").AddChoices(dayChoices).UseConverter(d => d.Day == "↩️ Gå tillbaka" ? $"[yellow]{d.Day}[/]" : $"{d.Day} ({d.FocusArea}) - {(d.IsCompleted ? "[green]✅ KLAR[/]" : "[red]❌ EJ KLAR[/]")}"));
                if (selectedDay.Day == "↩️ Gå tillbaka") return;

                bool markAsDone = AnsiConsole.Confirm($"Markera [green]{selectedDay.Day}[/] som genomförd?");
                if (markAsDone)
                {
                    selectedDay.IsCompleted = true;
                    _progressService.AddProgressLog(new ProgressLog { ClientId = client.Id, Date = DateTime.Now, Weight = 0, Notes = $"Genomförde pass: {selectedDay.Day} (Vecka {selectedPlan.Week})" });
                    SpectreUIHelper.Success("Pass markerat som klart.");
                }
                else { selectedDay.IsCompleted = false; AnsiConsole.MarkupLine("[yellow]Pass markerat som ej genomfört.[/]"); }
                _workoutStore.Save(allWorkouts);
                WaitForKey();
            }
            catch (Exception ex) { SpectreUIHelper.Error($"Fel: {ex.Message}"); WaitForKey(); }
        }

        private void DisplayWorkoutPlan(WorkoutPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };
            var table = new Table().Border(TableBorder.Heavy).Title($"[bold yellow]{plan.Name} (Vecka {plan.Week})[/]").AddColumn("DAG").AddColumn("Fokus").AddColumn("Status").AddColumn("Övningar");
            foreach (var day in weekDays)
            {
                var wDay = plan.DailyWorkouts.FirstOrDefault(d => d.Day != null && d.Day.Contains(day));
                if (wDay == null) table.AddRow($"[yellow]{day}[/]", "Vila", "-", "Återhämtning");
                else table.AddRow($"[yellow]{day}[/]", wDay.FocusArea, wDay.IsCompleted ? "[green]✅[/]" : "[red]❌[/]", string.Join(", ", wDay.Exercises.Select(e => e.Name.Trim())));
            }
            AnsiConsole.Write(table);
        }

        private void DisplayDietPlan(DietPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };
            var table = new Table().Border(TableBorder.Heavy).Title($"[bold green]{plan.Name} (Vecka {plan.Week})[/]").AddColumn("DAG").AddColumn("Måltider");
            foreach (var day in weekDays)
            {
                var mDay = plan.DailyMeals.FirstOrDefault(d => d.Day != null && d.Day.Contains(day));
                if (mDay == null) table.AddRow($"[yellow]{day}[/]", "Ingen plan");
                else table.AddRow($"[yellow]{day}[/]", $"Frukost: {mDay.Breakfast}\nLunch: {mDay.Lunch}\nMiddag: {mDay.Dinner}");
            }
            AnsiConsole.Write(table);
        }

        private void WaitForKey()
        {
            AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att gå tillbaka...[/]");
            Console.ReadKey(true);
        }
    }
}