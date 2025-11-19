using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using Spectre.Console;

namespace FitnessProgressTracker.UI
{
    public class PtMenu
    {
        private readonly ClientService _clientService;
        private readonly ScheduleService _scheduleService;
        private readonly ProgressService _progressService;

        public PtMenu(ClientService clientService, ScheduleService scheduleService, ProgressService progressService)
        {
            _clientService = clientService;
            _scheduleService = scheduleService;
            _progressService = progressService;
        }

        // ---------------------------
        // Huvudmeny för PT
        // ---------------------------
        public void Show(PT pt)
        {
            try
            {
                bool isRunning = true;

                while (isRunning)
                {
                    AnsiConsole.Background = Color.Grey15;
                    AnsiConsole.Clear();

                    SpectreUIHelper.AnimatedBanner("COACH MODE", Color.Blue);

                    AnsiConsole.MarkupLine("[italic green]Train hard, coach smart![/]");
                    AnsiConsole.MarkupLine("[dim yellow]Välj vad du vill göra idag, coach.[/]");

                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[bold cyan]Välj ett alternativ:[/]")
                            .AddChoices(
                                "👤 Visa min klientlista",
                                "📊 Se framsteg och statistik",
                                "🚪 Logga ut")
                    );

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "👤 Visa min klientlista":
                            ShowClientListMenu(pt);
                            break;

                        case "📊 Se framsteg och statistik":
                            SpectreUIHelper.Loading("Hämtar klientdata...");

                            var clients = _clientService.GetClientsForPT(pt.Id) ?? new List<Client>();

                            foreach (var client in clients)
                            {
                                if (client == null) continue;

                                var logs = _progressService.GetLogsForClient(client.Id) ?? new List<ProgressLog>();

                                if (logs.Count == 0)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]{client.FirstName} {client.LastName} har inga framsteg loggade ännu.[/]");
                                    continue;
                                }

                                var table = new Table().AddColumns("Datum", "Vikt (kg)", "Noteringar");

                                foreach (var log in logs)
                                {
                                    table.AddRow(
                                        log.Date.ToShortDateString(),
                                        log.Weight.ToString("0.0"),
                                        log.Notes ?? ""
                                    );
                                }

                                AnsiConsole.MarkupLine($"[bold underline]{client.FirstName} {client.LastName}[/]");
                                AnsiConsole.Write(table);
                                AnsiConsole.WriteLine();
                            }

                            SpectreUIHelper.Motivation();
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är nu utloggad. Grymt jobbat coach! 💪");
                            isRunning = false;
                            continue;
                    }

                    AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i PT-menyn: {ex.Message}");
            }
        }

        // ---------------------------
        // Hantera en specifik klient
        // ---------------------------
        private void ShowClientActionMenu(Client client)
        {
            bool inSubMenu = true;

            while (inSubMenu)
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner($"HANTERA: {client.FirstName?.ToUpper() ?? "N/A"}", Color.Green);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Vad vill du göra med [green]{client.FirstName}[/]?")
                        .AddChoices(
                            "🎯 Sätt upp mål",
                            "🤖 Skapa träningsschema (AI-hjälp)",
                            "🥗 Skapa kostschema (AI-hjälp)",
                            "📊 Se framsteg och statistik",
                            "↩️ Gå tillbaka"
                        )
                );

                switch (choice)
                {
                    case "🎯 Sätt upp mål":
                        var goalDesc = AnsiConsole.Ask<string>("Beskriv klientens mål:");
                        var targetWeight = AnsiConsole.Ask<double>($"Ange målvikt för {client.FirstName} (kg):");
                        var workoutsPerWeek = AnsiConsole.Ask<int>("Antal pass/vecka:");

                        _clientService.UpdateClientGoals(client.Id, goalDesc, targetWeight, workoutsPerWeek);
                        client = _clientService.GetClientById(client.Id);

                        SpectreUIHelper.Success($"Mål uppdaterade för {client.FirstName}!");
                        Console.ReadKey(true);
                        break;

                    case "🤖 Skapa träningsschema (AI-hjälp)":
                    case "🥗 Skapa kostschema (AI-hjälp)":
                        SpectreUIHelper.Error("Kommer i Task #97.");
                        Thread.Sleep(2000);
                        break;

                    case "📊 Se framsteg och statistik":
                        ShowClientDashboard(client);
                        break;

                    case "↩️ Gå tillbaka":
                        inSubMenu = false;
                        break;
                }
            }
        }

        // ---------------------------
        // Lista klienter
        // ---------------------------
        private void ShowClientListMenu(PT pt)
        {
            SpectreUIHelper.Loading("Hämtar dina klienter...");

            var clients = _clientService.GetClientsForPT(pt.Id) ?? new List<Client>();

            if (!clients.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Du har inga klienter kopplade till dig ännu.[/]");
                return;
            }

            var selectedClient = AnsiConsole.Prompt(
                new SelectionPrompt<Client>()
                    .Title("Välj en [cyan]klient[/] att hantera:")
                    .AddChoices(clients.Where(c => c != null))
                    .UseConverter(c => $"{c.FirstName} {c.LastName}")
            );

            ShowClientActionMenu(selectedClient);
        }

        // ---------------------------
        // Klient-dashboard
        // ---------------------------
        private void ShowClientDashboard(Client client)
        {
            AnsiConsole.Clear();
            SpectreUIHelper.AnimatedBanner($"DASHBOARD: {client.FirstName}", Color.Green);

            // PT-mål
            AnsiConsole.MarkupLine("[bold underline green]🎯 PT:s satta mål[/]");
            var goalTable = new Table();
            goalTable.AddColumn("Målbeskrivning");
            goalTable.AddColumn("Målvikt (kg)");
            goalTable.AddColumn("Pass/vecka");

            goalTable.AddRow(
                client.GoalDescription ?? "Ej angivet",
                client.TargetWeight.ToString(),
                client.WorkoutsPerWeek.ToString()
            );

            AnsiConsole.Write(goalTable);
            AnsiConsole.WriteLine();

            // Senaste 5 progress-loggar
            AnsiConsole.MarkupLine("[bold underline green]📊 Senaste 5 framsteg[/]");
            var logs = _progressService.GetLogsForClient(client.Id)?.Take(5).ToList() ?? new List<ProgressLog>();

            if (!logs.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inga loggar registrerade ännu.[/]");
            }
            else
            {
                var logTable = new Table();
                logTable.AddColumn("Datum");
                logTable.AddColumn("Vikt (kg)");
                logTable.AddColumn("Anteckning");

                foreach (var log in logs)
                {
                    logTable.AddRow(
                        log.Date.ToShortDateString(),
                        log.Weight.ToString("0.0"),
                        log.Notes ?? ""
                    );
                }

                AnsiConsole.Write(logTable);
            }

            AnsiConsole.WriteLine();

            // Scheman
            AnsiConsole.MarkupLine("[bold underline green]📅 Klientens scheman[/]");

            var scheduleTable = new Table();
            scheduleTable.AddColumn("WorkoutPlan ID");
            scheduleTable.AddColumn("DietPlan ID");

            int max = Math.Max(
                client.WorkoutPlanIds?.Count ?? 0,
                client.DietPlanIds?.Count ?? 0
            );

            for (int i = 0; i < max; i++)
            {
                string workoutId = (client.WorkoutPlanIds != null && i < client.WorkoutPlanIds.Count)
                    ? client.WorkoutPlanIds[i].ToString()
                    : "-";

                string dietId = (client.DietPlanIds != null && i < client.DietPlanIds.Count)
                    ? client.DietPlanIds[i].ToString()
                    : "-";

                scheduleTable.AddRow(workoutId, dietId);
            }

            AnsiConsole.Write(scheduleTable);
            AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
            Console.ReadKey(true);
        }

        // ---------------------------
        // Dietplan-tabell (valfri visning)
        // ---------------------------
        private void ShowDietPlanReviewTable(DietPlan plan)
        {
            AnsiConsole.Clear();

            var table = new Table().Title($"[bold green]{plan?.Name ?? "N/A"}[/]");
            table.AddColumn("Dag");
            table.AddColumn("Måltider");

            if (plan?.DailyMeals != null)
            {
                foreach (var daily in plan.DailyMeals)
                {
                    var mealsText = $"Frukost: {daily.Breakfast}\n" +
                                    $"Lunch: {daily.Lunch}\n" +
                                    $"Middag: {daily.Dinner}\n" +
                                    $"Snacks: {daily.Snacks}\n" +
                                    $"Totalt: {daily.TotalCalories} kcal";

                    table.AddRow(daily.Day ?? "-", mealsText);
                }
            }

            AnsiConsole.Write(table);
        }
    }
}