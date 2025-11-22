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
                                "🗑️ Ta bort klient(er)",
                                "🚪 Logga ut")
                    );

                    // Rensa skärmen direkt efter val för renare look
                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "👤 Visa min klientlista":
                            ShowClientListMenu(pt);
                            break;

                        case "📊 Se framsteg och statistik":
                            ShowAllClientsProgressOverview(pt);
                            break;

                        case "🗑️ Ta bort klient(er)":
                            ShowDeleteClientPrompt(pt);
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är nu utloggad. Grymt jobbat coach! 💪");
                            // Vi pausar här så man hinner se hejdå-meddelandet innan programmet stängs
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

        // ---------------------------
        // Metoder för klienthantering
        // ---------------------------

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

            // Lägg till alternativ för att gå tillbaka
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

            // Om "Gå tillbaka" valdes, returnera direkt utan paus
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

                // Hämta färsk data
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
                        ShowClientDashboard(freshClient);
                        break;

                    case "↩️ Gå tillbaka":
                        inSubMenu = false;
                        break;
                }

                // Pausa BARA om vi är kvar i menyn (dvs vi har gjort en action).
                // Om inSubMenu är false (vi valde Gå tillbaka), skippas detta.
                if (inSubMenu)
                {
                    AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
        }

        private void ShowClientDashboard(Client client)
        {
            AnsiConsole.Clear();
            SpectreUIHelper.AnimatedBanner($"DASHBOARD: {client.FirstName}", Color.Green);

            // 1. Målöversikt
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

            // 2. Senaste loggar
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

            // 3. Schema-IDn
            AnsiConsole.MarkupLine("[bold underline green]📅 Klientens scheman[/]");
            var scheduleTable = new Table();
            scheduleTable.AddColumn("WorkoutPlan ID");
            scheduleTable.AddColumn("DietPlan ID");

            int max = Math.Max(client.WorkoutPlanIds?.Count ?? 0, client.DietPlanIds?.Count ?? 0);

            for (int i = 0; i < max; i++)
            {
                string workoutId = (client.WorkoutPlanIds != null && i < client.WorkoutPlanIds.Count)
                    ? client.WorkoutPlanIds[i].ToString() : "-";

                string dietId = (client.DietPlanIds != null && i < client.DietPlanIds.Count)
                    ? client.DietPlanIds[i].ToString() : "-";

                scheduleTable.AddRow(workoutId, dietId);
            }
            AnsiConsole.Write(scheduleTable);

        }

        // ---------------------------
        // Wizards och Flöden
        // ---------------------------

        private async Task SetGoalAndScheduleFlow(Client client)
        {
            // 1. Sätt/Redigera mål
            var updatedClient = EditGoalDetails(client);
            if (updatedClient == null) return; // Avbrutet
            client = updatedClient;

            // 2. Skapa och granska träningsschema
            await RunWorkoutReviewLoop(client);

            // 3. Skapa och granska kostschema
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

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]Vad vill du ändra på?[/]")
                        .AddChoices(
                            "🎯 Ändra Målet (Målvikt, Kalorier, Pass/vecka)",
                            "🏋️ Ändra Träningsplan (Generera nytt schema)",
                            "🍎 Ändra Kostplan (Generera nytt schema)",
                            "↩️ Gå tillbaka till Åtgärdsmenyn"
                        ));

                switch (choice)
                {
                    case "🎯 Ändra Målet (Målvikt, Kalorier, Pass/vecka)":
                        EditGoalDetails(freshClient);
                        break;

                    case "🏋️ Ändra Träningsplan (Generera nytt schema)":
                        RunWorkoutReviewLoop(freshClient).Wait();
                        break;

                    case "🍎 Ändra Kostplan (Generera nytt schema)":
                        if (freshClient.TargetCalories == 0)
                        {
                            SpectreUIHelper.Error("Du måste sätta Dagligt Kalorimål i 'Ändra Målet' först.");
                            // Paus här så man hinner läsa felet
                            Console.ReadKey(true);
                            break;
                        }
                        RunDietReviewLoop(freshClient, freshClient.TargetCalories).Wait();
                        break;

                    case "↩️ Gå tillbaka till Åtgärdsmenyn":
                        inHub = false;
                        break;
                }
            }
        }

        private Client? EditGoalDetails(Client client)
        {
            try
            {
                SpectreUIHelper.AnimatedBanner($"MÅL: {(client.FirstName ?? "Klient").ToUpper()}", Color.Yellow);

                var goalDesc = AnsiConsole.Prompt(
                    new TextPrompt<string>("[cyan]Beskriv målet:[/]")
                        .DefaultValue(client.GoalDescription ?? "")
                        .AllowEmpty()
                );

                var targetWeight = AnsiConsole.Prompt(
                    new TextPrompt<double>("[cyan]Ange ny målvikt (kg):[/]")
                        .DefaultValue(client.TargetWeight)
                        .Validate(weight =>
                        {
                            if (weight < 30 || weight > 300)
                                return ValidationResult.Error("[red]Målvikt måste vara mellan 30 och 300 kg.[/]");
                            return ValidationResult.Success();
                        })
                );

                var workoutsPerWeek = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Antal pass per vecka:[/]")
                        .DefaultValue(client.WorkoutsPerWeek)
                        .Validate(days =>
                        {
                            if (days < 0 || days > 7)
                                return ValidationResult.Error("[red]Antal pass per vecka måste vara mellan 0 och 7.[/]");
                            return ValidationResult.Success();
                        })
                );

                var targetCalories = AnsiConsole.Prompt(
                    new TextPrompt<int>("[cyan]Dagligt Kalorimål (kcal):[/]")
                        .DefaultValue(client.TargetCalories > 0 ? client.TargetCalories : 2000)
                        .Validate(calories =>
                        {
                            if (calories < 800 || calories > 5000)
                                return ValidationResult.Error("[red]Kalorimål måste vara mellan 800 och 5000 kcal.[/]");
                            return ValidationResult.Success();
                        })
                );

                _clientService.UpdateClientGoals(client.Id, goalDesc, targetWeight, workoutsPerWeek, targetCalories);
                SpectreUIHelper.Success($"Mål har uppdaterats för {client.FirstName}!");

                // Kort paus för att visa "Success"-meddelandet, sen gå vidare
                Thread.Sleep(1500);

                return _clientService.GetClientById(client.Id);
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Kunde inte uppdatera mål: {ex.Message}");
                Console.ReadKey(true);
                return null;
            }
        }

        // ---------------------------
        // AI Review Loops
        // ---------------------------

        private async Task RunWorkoutReviewLoop(Client client)
        {
            WorkoutPlan? plan = null;
            bool reviewing = true;

            while (reviewing)
            {
                if (plan == null)
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("green"))
                        .Start("AI skapar träningsschema... vänligen vänta...", ctx =>
                        {
                            try
                            {
                                int days = client.WorkoutsPerWeek;
                                string goal = client.GoalDescription ?? "Allmän träning";
                                plan = _scheduleService.CreateAndLinkWorkoutPlan(client.Id, goal, days).Result;
                            }
                            catch (Exception ex)
                            {
                                SpectreUIHelper.Error($"AI-fel: {ex.InnerException?.Message ?? ex.Message}");
                                AnsiConsole.MarkupLine("[bold red]Tryck tangent för att fortsätta...[/]");
                                Console.ReadKey(true);
                                plan = null;
                            }
                        });
                }

                if (plan == null)
                {
                    SpectreUIHelper.Error("Kunde inte skapa träningsschema. Försök igen.");
                    return;
                }

                ShowWorkoutPlanReviewTable(plan);

                var choices = new List<string> { "✔ Acceptera och gå vidare", "🔄 Generera nytt" };
                if (_scheduleService.GetPreviousWorkoutProposal() != null) choices.Add("↩️ Ångra till föregående");
                choices.Add("❌ Avbryt");

                var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Välj åtgärd:").AddChoices(choices));

                switch (action)
                {
                    case "✔ Acceptera och gå vidare":
                        var saved = _scheduleService.CommitPendingWorkoutPlan(client.Id);
                        if (saved != null) SpectreUIHelper.Success($"Träningsschema '{saved.Name}' sparat!");
                        else SpectreUIHelper.Error("Kunde inte spara schemat.");
                        reviewing = false;
                        return;

                    case "🔄 Generera nytt":
                        _scheduleService.SavePendingAsPreviousWorkoutProposal();
                        plan = null;
                        AnsiConsole.MarkupLine("[yellow]Genererar nytt träningsschema...[/]");
                        break;

                    case "↩️ Ångra till föregående":
                        plan = _scheduleService.RevertToPreviousWorkoutProposal();
                        break;

                    case "❌ Avbryt":
                        reviewing = false;
                        return;
                }
            }
        }

        private async Task RunDietReviewLoop(Client client, int targetCalories)
        {
            DietPlan? plan = null;
            bool reviewing = true;

            while (reviewing)
            {
                if (plan == null)
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("green"))
                        .Start("AI skapar kostschema... vänligen vänta...", ctx =>
                        {
                            try
                            {
                                string goal = client.GoalDescription ?? "Balanserad kost";
                                plan = _scheduleService.CreateAndLinkDietPlan(client.Id, goal, targetCalories).Result;
                            }
                            catch (Exception ex)
                            {
                                SpectreUIHelper.Error($"AI-fel: {ex.InnerException?.Message ?? ex.Message}");
                                plan = null;
                            }
                        });
                }

                if (plan == null)
                {
                    SpectreUIHelper.Error("AI kunde inte skapa ett kostschema. Försök igen.");
                    return;
                }

                ShowDietPlanReviewTable(plan);

                var choices = new List<string> { "✔ Acceptera och spara", "🔄 Generera nytt" };
                if (_scheduleService.GetPreviousDietProposal() != null) choices.Add("↩️ Ångra till föregående");
                choices.Add("❌ Avbryt");

                var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Välj åtgärd:").AddChoices(choices));

                switch (action)
                {
                    case "✔ Acceptera och spara":
                        var saved = _scheduleService.CommitPendingDietPlan(client.Id);
                        if (saved != null) SpectreUIHelper.Success($"Kostschema '{saved.Name}' sparat!");
                        else SpectreUIHelper.Error("Kunde inte spara schemat.");
                        reviewing = false;
                        break;

                    case "🔄 Generera nytt":
                        _scheduleService.SavePendingAsPreviousDietProposal();
                        plan = null;
                        AnsiConsole.MarkupLine("[yellow]Genererar nytt förslag...[/]");
                        break;

                    case "↩️ Ångra till föregående":
                        plan = _scheduleService.RevertToPreviousDietProposal();
                        break;

                    case "❌ Avbryt":
                        reviewing = false;
                        return;
                }
            }
        }

        // ---------------------------
        // Visualisering (Tabeller)
        // ---------------------------

        private void ShowDietPlanReviewTable(DietPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };

            var table = new Table()
                .Border(TableBorder.Heavy)
                .Title($"[bold green]{plan.Name}[/]");

            table.AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12));
            table.AddColumn(new TableColumn("[bold green]MÅLTIDER[/]").LeftAligned().Width(15));
            table.AddColumn(new TableColumn("[bold white]KOSTPLAN[/]").LeftAligned().Width(50));

            for (int i = 0; i < weekDays.Length; i++)
            {
                var dayName = weekDays[i];
                var mealDay = plan.DailyMeals.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName));
                var mealSlots = "Frukost\nLunch\nMiddag\nSnacks\n[bold white]Totalt:[/]";

                if (mealDay == null)
                {
                    string mealDetails = "[grey]Ingen kostplan satt för denna dag.[/]" + new string('\n', 4);
                    table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", mealDetails);
                }
                else
                {
                    var mealDetails = $"[white]{mealDay.Breakfast}[/]\n" +
                                      $"[white]{mealDay.Lunch}[/]\n" +
                                      $"[white]{mealDay.Dinner}[/]\n" +
                                      $"[white]{mealDay.Snacks}[/]\n" +
                                      $"[bold yellow]{mealDay.TotalCalories} kcal[/]";

                    table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", mealDetails);
                }

                if (i < weekDays.Length - 1) table.AddEmptyRow();
            }
            AnsiConsole.Write(table);
        }

        private void ShowWorkoutPlanReviewTable(WorkoutPlan plan)
        {
            AnsiConsole.Clear();
            var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };

            var table = new Table()
                .Border(TableBorder.Heavy)
                .Title($"[bold yellow]{plan.Name}[/]")
                .AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12))
                .AddColumn(new TableColumn("[bold blue]Fokusområde[/]").Centered().Width(15))
                .AddColumn(new TableColumn("[bold cyan]Övningar[/]").Centered().Width(50));

            for (int i = 0; i < weekDays.Length; i++)
            {
                var dayName = weekDays[i];
                var workoutDay = plan.DailyWorkouts.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName));

                string focusAreaText;
                string textContent;

                if (workoutDay == null)
                {
                    focusAreaText = "[magenta]Vilodag[/]";
                    textContent = "[magenta]Återhämtning: Aktivitet kan vara promenad/yoga.[/]" + new string('\n', 5);
                }
                else
                {
                    var exercisesToDisplay = workoutDay.Exercises.ToList();
                    string exerciseTextRaw = string.Join("\n",
                        exercisesToDisplay.Select(ex =>
                            $"[cyan]{(ex.Name ?? "Okänd övning").Replace('\n', ' ').Trim()}[/] — [grey]{(ex.SetsAndReps ?? "-").Replace('\n', ' ').Trim()}[/]"
                        ));

                    int lineCount = exercisesToDisplay.Count;
                    string padding = new string('\n', Math.Max(0, 6 - lineCount));

                    textContent = exerciseTextRaw + padding;
                    focusAreaText = $"[blue]{workoutDay.FocusArea}[/]";
                }

                table.AddRow($"[yellow]{dayName}[/]", focusAreaText, textContent);
            }
            AnsiConsole.Write(table);
        }

        // ---------------------------
        // Administration
        // ---------------------------

        private void ShowDeleteClientPrompt(PT pt)
        {
            try
            {
                SpectreUIHelper.Loading("Laddar klientdata...");
                var clients = _clientService.GetClientsForPT(pt.Id);

                var cancelChoice = new Client { Id = -1, FirstName = "↩️ Avbryt / Gå tillbaka", LastName = "" };
                var nukeChoice = new Client { Id = -999, FirstName = "🚨 RADERA ALL DATA (Klienter, Scheman, Mål)", LastName = "" };

                var allChoices = new List<Client>();
                allChoices.AddRange(clients);
                allChoices.Add(nukeChoice);
                allChoices.Add(cancelChoice);

                var selectedClients = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<Client>()
                        .Title("[bold red]HANTERA BORTTAGNING[/]")
                        .InstructionsText("[grey](Tryck [blue]<space>[/] för att välja, [green]<enter>[/] för att bekräfta)[/]")
                        .PageSize(12)
                        .AddChoices(allChoices)
                        .UseConverter(c =>
                        {
                            if (c.Id == -1) return $"[yellow]{c.FirstName}[/]";
                            if (c.Id == -999) return $"[bold red blink]{c.FirstName}[/]";
                            return $"❌ {c.FirstName} {c.LastName} (ID: {c.Id})";
                        })
                );

                // Hantera "Avbryt" - Returnera direkt utan paus
                if (selectedClients.Any(c => c.Id == -1))
                {
                    AnsiConsole.MarkupLine("[yellow]Åtgärd avbruten.[/]");
                    return;
                }

                // Hantera "RADERA ALLT"
                if (selectedClients.Any(c => c.Id == -999))
                {
                    AnsiConsole.MarkupLine("[bold white on red]VARNING: DU HÅLLER PÅ ATT RADERA ALLA KLIENTER OCH ALL DATA![/]");
                    if (AnsiConsole.Confirm("Är du helt säker? Detta går INTE att ångra.", false))
                    {
                        AnsiConsole.Write(new FigletText("ÄR DU SÄKER?").Color(Color.Red));
                        if (AnsiConsole.Confirm("Bekräfta en sista gång för att radera databasen:"))
                        {
                            SpectreUIHelper.Loading("Raderar systemdata...");
                            _clientService.DeleteAllData();
                            SpectreUIHelper.Success("Systemet är rensat. Alla klienter och scheman är borta.");
                            // Paus vid lyckad radering så man hinner se det
                            Console.ReadKey(true);
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]Puh! Radering avbruten.[/]");
                    }
                    return;
                }

                // Hantera vanliga klienter
                if (selectedClients.Count > 0)
                {
                    if (AnsiConsole.Confirm($"Vill du verkligen ta bort {selectedClients.Count} klient(er)?", false))
                    {
                        List<int> clientIdsToDelete = selectedClients.Select(c => c.Id).ToList();
                        _clientService.DeleteClients(clientIdsToDelete);
                        SpectreUIHelper.Success($"Borttagning lyckades! {selectedClients.Count} klient(er) raderades.");
                        // Paus vid lyckad borttagning
                        Console.ReadKey(true);
                    }
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod: {ex.Message}");
                Console.ReadKey(true);
            }
        }

        private void ShowAllClientsProgressOverview(PT pt)
        {
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

            // VIKTIGT: Här måste vi ha en paus, annars rensas tabellen direkt när man kommer till huvudmenyn
            AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
            Console.ReadKey(true);
        }
    }
}