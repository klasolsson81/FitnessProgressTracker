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
                                "📊 Se klientloggar",
                                "🗑️ Ta bort klient(er)",
                                "🚪 Logga ut")
                    );

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "👤 Visa min klientlista":
                            ShowClientListMenu(pt);
                            break;

                        case "📊 Se klientloggar":
                            ShowAllClientsProgressOverview(pt);
                            break;

                        case "🗑️ Ta bort klient(er)":
                            ShowDeleteClientPrompt(pt);
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är nu utloggad. Grymt jobbat coach! 💪");
                            AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att avsluta...[/]");
                            Console.ReadKey(true);
                            isRunning = false;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i PT-menyn: {ex.Message}");
                Console.ReadKey(true);
            }
        }

        private void ShowClientListMenu(PT pt)
        {
            SpectreUIHelper.Loading("Hämtar dina klienter...");

            var clients = _clientService.GetClientsForPT(pt.Id) ?? new List<Client>();

            if (!clients.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Du har inga klienter kopplade till dig ännu.[/]");
                AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att gå tillbaka...[/]");
                Console.ReadKey(true);
                return;
            }

            Client goBackChoice = new Client { Id = -1, FirstName = "↩️ Gå tillbaka", LastName = "" };
            var choices = clients.ToList();
            choices.Add(goBackChoice);

            var selectedClient = AnsiConsole.Prompt(
                new SelectionPrompt<Client>()
                    .Title("Välj en [cyan]klient[/] att hantera:")
                    .PageSize(15)
                    .AddChoices(choices)
                    .UseConverter(c => c.Id == -1
                        ? $"[yellow]{c.FirstName}[/]"
                        : $"👤 [white]{c.FirstName} {c.LastName}[/] [grey](ID: {c.Id})[/]")
            );

            if (selectedClient.Id == -1) return;

            ShowClientActionMenu(selectedClient);
        }

        private void ShowClientActionMenu(Client client)
        {
            bool inSubMenu = true;

            while (inSubMenu)
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner($"HANTERA: {(client.FirstName ?? "N/A").ToUpper()}", Color.Green);

                Client? freshClient = _clientService.GetClientById(client.Id);
                if (freshClient == null) return;

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Vad vill du göra med [green]{freshClient.FirstName}[/]?")
                        .AddChoices(
                            "🎯 Sätt upp mål (Skapa nytt mål, tränings- och kostschema)",
                            "✏️ Redigera mål (Redigera/ändra mål, tränings- och kostschema)",
                            "📊 Se framsteg och statistik",
                            "↩️ Gå tillbaka"
                        )
                );

                switch (choice)
                {
                    case "🎯 Sätt upp mål (Skapa nytt mål, tränings- och kostschema)":
                        SetGoalAndScheduleFlow(freshClient).Wait();
                        break;

                    case "✏️ Redigera mål (Redigera/ändra mål, tränings- och kostschema)":
                        ShowEditGoalHub(freshClient);
                        break;

                    case "📊 Se framsteg och statistik":
                        SpectreUIHelper.Loading("Hämtar statistik...");
                        var logs = _progressService.GetLogsForClient(freshClient.Id);
                        var stats = _scheduleService.GetWorkoutStatistics(freshClient.Id);

                        // Anropa den gemensamma rit-metoden
                        SpectreUIHelper.ShowDashboardVisuals(freshClient, logs, stats);

                        continue;

                    case "↩️ Gå tillbaka":
                        inSubMenu = false;
                        break;
                }

                if (inSubMenu)
                {
                    AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
        }

        private async Task SetGoalAndScheduleFlow(Client client)
        {
            var updatedClient = EditGoalDetails(client);
            if (updatedClient == null) return;
            client = updatedClient;
            await RunWorkoutReviewLoop(client);
            AnsiConsole.MarkupLine("\n[bold green]Går vidare till KOSTSCHEMA.[/]");
            await RunDietReviewLoop(client, client.TargetCalories);
        }

        private void ShowEditGoalHub(Client client)
        {
            bool inHub = true;
            while (inHub)
            {
                Client? freshClient = _clientService.GetClientById(client.Id);
                if (freshClient == null) return;
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner($"REDIGERA: {(freshClient.FirstName ?? "Klient").ToUpper()}", Color.Yellow);
                AnsiConsole.MarkupLine($"\n[bold yellow]Nuvarande mål:[/]");
                AnsiConsole.MarkupLine($"- Pass/vecka: [cyan]{freshClient.WorkoutsPerWeek}[/], Målvikt: [cyan]{freshClient.TargetWeight} kg[/], Kalorier: [cyan]{freshClient.TargetCalories} kcal / dag[/]");
                AnsiConsole.WriteLine();
                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("[bold cyan]Vad vill du ändra på?[/]").AddChoices("🎯 Ändra Målet (Målvikt, Kalorier, Pass/vecka)", "🏋️ Ändra Träningsplan (Generera nytt schema)", "🍎 Ändra Kostplan (Generera nytt schema)", "↩️ Gå tillbaka till Åtgärdsmenyn"));
                switch (choice)
                {
                    case "🎯 Ändra Målet (Målvikt, Kalorier, Pass/vecka)": EditGoalDetails(freshClient); break;
                    case "🏋️ Ändra Träningsplan (Generera nytt schema)": RunWorkoutReviewLoop(freshClient).Wait(); break;
                    case "🍎 Ändra Kostplan (Generera nytt schema)":
                        if (freshClient.TargetCalories == 0) { SpectreUIHelper.Error("Du måste sätta Dagligt Kalorimål i 'Ändra Målet' först."); Console.ReadKey(true); break; }
                        RunDietReviewLoop(freshClient, freshClient.TargetCalories).Wait(); break;
                    case "↩️ Gå tillbaka till Åtgärdsmenyn": inHub = false; break;
                }
            }
        }

        private Client? EditGoalDetails(Client client)
        {
            try
            {
                SpectreUIHelper.AnimatedBanner($"MÅL: {(client.FirstName ?? "Klient").ToUpper()}", Color.Yellow);
                var goalDesc = AnsiConsole.Prompt(new TextPrompt<string>("[cyan]Beskriv målet:[/]").DefaultValue(client.GoalDescription ?? "").AllowEmpty());
                var targetWeight = AnsiConsole.Prompt(new TextPrompt<double>("[cyan]Ange ny målvikt (kg):[/]").DefaultValue(client.TargetWeight).Validate(weight => { if (weight < 30 || weight > 300) return ValidationResult.Error("[red]Målvikt måste vara mellan 30 och 300 kg.[/]"); return ValidationResult.Success(); }));
                var workoutsPerWeek = AnsiConsole.Prompt(new TextPrompt<int>("[cyan]Antal pass per vecka:[/]").DefaultValue(client.WorkoutsPerWeek).Validate(days => { if (days < 0 || days > 7) return ValidationResult.Error("[red]Antal pass per vecka måste vara mellan 0 och 7.[/]"); return ValidationResult.Success(); }));
                var targetCalories = AnsiConsole.Prompt(new TextPrompt<int>("[cyan]Dagligt Kalorimål (kcal):[/]").DefaultValue(client.TargetCalories > 0 ? client.TargetCalories : 2000).Validate(calories => { if (calories < 800 || calories > 5000) return ValidationResult.Error("[red]Kalorimål måste vara mellan 800 och 5000 kcal.[/]"); return ValidationResult.Success(); }));
                _clientService.UpdateClientGoals(client.Id, goalDesc, targetWeight, workoutsPerWeek, targetCalories);
                SpectreUIHelper.Success($"Mål har uppdaterats för {client.FirstName}!");
                Thread.Sleep(1500);
                return _clientService.GetClientById(client.Id);
            }
            catch (Exception ex) { SpectreUIHelper.Error($"Kunde inte uppdatera mål: {ex.Message}"); Console.ReadKey(true); return null; }
        }

        private async Task RunWorkoutReviewLoop(Client client)
        {
            WorkoutPlan? plan = null;
            bool reviewing = true;
            AnsiConsole.MarkupLine("\n[green]Träningsschema kommer strax genereras...[/]");
            int week = AnsiConsole.Prompt(new TextPrompt<int>("[cyan]Vilken vecka gäller detta schema för (t.ex. 45)?[/]").Validate(w => w > 0 && w < 54 ? ValidationResult.Success() : ValidationResult.Error("Ogiltig vecka")));
            while (reviewing)
            {
                if (plan == null)
                {
                    AnsiConsole.Status().Spinner(Spinner.Known.Dots).SpinnerStyle(Style.Parse("green")).Start("AI skapar träningsschema... vänligen vänta...", ctx => { try { int days = client.WorkoutsPerWeek; string goal = client.GoalDescription ?? "Allmän träning"; plan = _scheduleService.CreateAndLinkWorkoutPlan(client.Id, goal, days, week).Result; } catch (Exception ex) { SpectreUIHelper.Error($"AI-fel: {ex.InnerException?.Message ?? ex.Message}"); AnsiConsole.MarkupLine("[bold red]Tryck tangent för att fortsätta...[/]"); Console.ReadKey(true); plan = null; } });
                }
                if (plan == null) { SpectreUIHelper.Error("Kunde inte skapa träningsschema."); return; }
                ShowWorkoutPlanReviewTable(plan);
                var choices = new List<string> { "✔ Acceptera och gå vidare", "🔄 Generera nytt" };
                if (_scheduleService.GetPreviousWorkoutProposal() != null) choices.Add("↩️ Ångra till föregående");
                choices.Add("❌ Avbryt");
                var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Välj åtgärd:").AddChoices(choices));
                switch (action)
                {
                    case "✔ Acceptera och gå vidare": var saved = _scheduleService.CommitPendingWorkoutPlan(client.Id); if (saved != null) SpectreUIHelper.Success($"Träningsschema '{saved.Name}' (Vecka {saved.Week}) sparat!"); else SpectreUIHelper.Error("Kunde inte spara schemat."); reviewing = false; return;
                    case "🔄 Generera nytt": _scheduleService.SavePendingAsPreviousWorkoutProposal(); plan = null; AnsiConsole.MarkupLine("[yellow]Genererar nytt träningsschema...[/]"); break;
                    case "↩️ Ångra till föregående": plan = _scheduleService.RevertToPreviousWorkoutProposal(); break;
                    case "❌ Avbryt": reviewing = false; return;
                }
            }
        }

        private async Task RunDietReviewLoop(Client client, int targetCalories)
        {
            DietPlan? plan = null;
            bool reviewing = true;
            AnsiConsole.MarkupLine("\n[green]Kostschema kommer strax genereras...[/]");
            int week = AnsiConsole.Prompt(new TextPrompt<int>("[cyan]Vilken vecka gäller detta kostschema för (t.ex. 45)?[/]").Validate(w => w > 0 && w < 54 ? ValidationResult.Success() : ValidationResult.Error("Ogiltig vecka")));
            while (reviewing)
            {
                if (plan == null)
                {
                    AnsiConsole.Status().Spinner(Spinner.Known.Dots).SpinnerStyle(Style.Parse("green")).Start("AI skapar kostschema... vänligen vänta...", ctx => { try { string goal = client.GoalDescription ?? "Balanserad kost"; plan = _scheduleService.CreateAndLinkDietPlan(client.Id, goal, targetCalories, week).Result; } catch (Exception ex) { SpectreUIHelper.Error($"AI-fel: {ex.InnerException?.Message ?? ex.Message}"); plan = null; } });
                }
                if (plan == null) { SpectreUIHelper.Error("AI kunde inte skapa ett kostschema."); return; }
                ShowDietPlanReviewTable(plan);
                var choices = new List<string> { "✔ Acceptera och spara", "🔄 Generera nytt" };
                if (_scheduleService.GetPreviousDietProposal() != null) choices.Add("↩️ Ångra till föregående");
                choices.Add("❌ Avbryt");
                var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Välj åtgärd:").AddChoices(choices));
                switch (action)
                {
                    case "✔ Acceptera och spara": var saved = _scheduleService.CommitPendingDietPlan(client.Id); if (saved != null) SpectreUIHelper.Success($"Kostschema '{saved.Name}' (Vecka {saved.Week}) sparat!"); else SpectreUIHelper.Error("Kunde inte spara schemat."); reviewing = false; break;
                    case "🔄 Generera nytt": _scheduleService.SavePendingAsPreviousDietProposal(); plan = null; AnsiConsole.MarkupLine("[yellow]Genererar nytt förslag...[/]"); break;
                    case "↩️ Ångra till föregående": plan = _scheduleService.RevertToPreviousDietProposal(); break;
                    case "❌ Avbryt": reviewing = false; return;
                }
            }
        }

        private void ShowDietPlanReviewTable(DietPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };
            var table = new Table().Border(TableBorder.Heavy).Title($"[bold green]{plan.Name}[/]").AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12)).AddColumn(new TableColumn("[bold green]MÅLTIDER[/]").LeftAligned().Width(15)).AddColumn(new TableColumn("[bold white]KOSTPLAN[/]").LeftAligned().Width(50));
            for (int i = 0; i < weekDays.Length; i++) { var dayName = weekDays[i]; var mealDay = plan.DailyMeals.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName)); var mealSlots = "Frukost\nLunch\nMiddag\nSnacks\n[bold white]Totalt:[/]"; if (mealDay == null) { table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", "[grey]Ingen plan[/]"); } else { var details = $"{mealDay.Breakfast}\n{mealDay.Lunch}\n{mealDay.Dinner}\n{mealDay.Snacks}\n[bold yellow]{mealDay.TotalCalories} kcal[/]"; table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", details); } if (i < weekDays.Length - 1) table.AddEmptyRow(); }
            AnsiConsole.Write(table);
        }

        private void ShowWorkoutPlanReviewTable(WorkoutPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };
            var table = new Table().Border(TableBorder.Heavy).Title($"[bold yellow]{plan.Name}[/]").AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12)).AddColumn(new TableColumn("[bold blue]Fokusområde[/]").Centered().Width(15)).AddColumn(new TableColumn("[bold cyan]Övningar[/]").Centered().Width(50));
            for (int i = 0; i < weekDays.Length; i++) { var dayName = weekDays[i]; var workoutDay = plan.DailyWorkouts.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName)); string focusAreaText = workoutDay == null ? "[magenta]Vilodag[/]" : $"[blue]{workoutDay.FocusArea}[/]"; string textContent = workoutDay == null ? "[magenta]Återhämtning[/]" : string.Join("\n", workoutDay.Exercises.Select(ex => $"[cyan]{ex.Name.Replace('\n', ' ').Trim()}[/] — [grey]{ex.SetsAndReps.Replace('\n', ' ').Trim()}[/]")); table.AddRow($"[yellow]{dayName}[/]", focusAreaText, textContent); }
            AnsiConsole.Write(table);
        }

        private void ShowDeleteClientPrompt(PT pt)
        {
            try
            {
                SpectreUIHelper.Loading("Laddar klientdata...");
                var clients = _clientService.GetClientsForPT(pt.Id);
                var cancelChoice = new Client { Id = -1, FirstName = "↩️ Avbryt / Gå tillbaka", LastName = "" };
                var nukeChoice = new Client { Id = -999, FirstName = "🚨 RADERA ALL DATA", LastName = "" };
                var allChoices = new List<Client>(); allChoices.AddRange(clients); allChoices.Add(nukeChoice); allChoices.Add(cancelChoice);
                var selectedClients = AnsiConsole.Prompt(new MultiSelectionPrompt<Client>().Title("[bold red]HANTERA BORTTAGNING[/]").InstructionsText("[grey](Tryck [blue]<space>[/] för att välja, [green]<enter>[/] för att bekräfta)[/]").PageSize(12).AddChoices(allChoices).UseConverter(c => c.Id == -1 ? $"[yellow]{c.FirstName}[/]" : c.Id == -999 ? $"[bold red blink]{c.FirstName}[/]" : $"❌ {c.FirstName} {c.LastName} (ID: {c.Id})"));
                if (selectedClients.Any(c => c.Id == -1)) { AnsiConsole.MarkupLine("[yellow]Åtgärd avbruten.[/]"); return; }
                if (selectedClients.Any(c => c.Id == -999)) { if (AnsiConsole.Confirm("Är du helt säker? Detta raderar allt.", false)) { if (AnsiConsole.Confirm("Sista chansen:", false)) { _clientService.DeleteAllData(); SpectreUIHelper.Success("Systemet rensat."); Console.ReadKey(true); } } return; }
                if (selectedClients.Count > 0) { if (AnsiConsole.Confirm($"Ta bort {selectedClients.Count} klient(er)?", false)) { _clientService.DeleteClients(selectedClients.Select(c => c.Id).ToList()); SpectreUIHelper.Success("Klienter raderade."); Console.ReadKey(true); } }
            }
            catch (Exception ex) { SpectreUIHelper.Error($"Fel: {ex.Message}"); Console.ReadKey(true); }
        }

        private void ShowAllClientsProgressOverview(PT pt)
        {
            SpectreUIHelper.Loading("Hämtar klientdata...");
            var clients = _clientService.GetClientsForPT(pt.Id) ?? new List<Client>();
            foreach (var client in clients)
            {
                if (client == null) continue;
                var logs = _progressService.GetLogsForClient(client.Id) ?? new List<ProgressLog>();
                if (logs.Count == 0) { AnsiConsole.MarkupLine($"[yellow]{client.FirstName} {client.LastName} har inga framsteg loggade ännu.[/]"); continue; }
                var table = new Table().AddColumns("Datum", "Vikt (kg)", "Noteringar");
                foreach (var log in logs) table.AddRow(log.Date.ToShortDateString(), log.Weight.ToString("0.0"), log.Notes ?? "");
                AnsiConsole.MarkupLine($"[bold underline]{client.FirstName} {client.LastName}[/]");
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
            SpectreUIHelper.Motivation();
            AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
            Console.ReadKey(true);
        }
    }
}