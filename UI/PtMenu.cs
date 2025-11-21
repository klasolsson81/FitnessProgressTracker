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
						// — Review-flöde för träningsschema —
						try
						{
							// 1) Hämta senaste version av klienten från clientService
							Client freshClient = _clientService.GetClientById(client.Id);

							// 2) Hämta klientens målbeskrivning (ex: "Bygga styrka")
							string goal = freshClient.GoalDescription;

							// 3) Fråga PT om antal träningspass per vecka
							int daysPerWeek = AnsiConsole.Ask<int>("Ange antal träningspass per vecka:");

							// 4) Visa loading/spinner tills AI har skapat schemat
							WorkoutPlan plan = null; // Här sparas resultatet från AI

							AnsiConsole.Status()
								.Spinner(Spinner.Known.Dots)
								.SpinnerStyle(Style.Parse("green"))
								.Start("AI skapar träningsschema... vänligen vänta...", ctx =>
								{
									// Anropa ScheduleService för att skapa ett förslag (sparas som pending)
									plan = _scheduleService.CreateAndLinkWorkoutPlan(freshClient.Id, goal, daysPerWeek).Result;
								});

							// 5) Kontrollera att planen verkligen skapades
							if (plan == null)
							{
								SpectreUIHelper.Error("AI kunde inte skapa ett träningsschema. Försök igen senare.");
								break;
							}


							// 5) UI-loop där PT kan acceptera eller generera nytt
							bool reviewing = true;
							while (reviewing)
							{
								// Visa tabellen
								ShowWorkoutPlanReviewTable(plan);

								// Välj handling
								var action = AnsiConsole.Prompt(
									new SelectionPrompt<string>()
										.Title("Välj åtgärd:")
										.AddChoices("✔ Acceptera och spara", "🔄 Generera nytt", "↩️ Avbryt"));

								switch (action)
								{
									case "✔ Acceptera och spara":
										var saved = _scheduleService.CommitPendingWorkoutPlan(freshClient.Id);
										if (saved != null)
											SpectreUIHelper.Success($"Träningsschema '{saved.Name}' sparat!");
										else
											SpectreUIHelper.Error("Kunde inte spara träningsschemat.");

										reviewing = false;
										break;

									case "🔄 Generera nytt":
										{
											WorkoutPlan newPlan = null;

											// Spinner medan AI jobbar
											AnsiConsole.Status()
												.Spinner(Spinner.Known.Dots)
												.SpinnerStyle(Style.Parse("green"))
												.Start("AI skapar ett nytt träningsschema... vänligen vänta...", ctx =>
												{
													newPlan = _scheduleService
														.CreateAndLinkWorkoutPlan(freshClient.Id, goal, daysPerWeek)
														.Result;
												});

											if (newPlan == null)
											{
												SpectreUIHelper.Error("AI kunde inte generera ett nytt schema.");
												reviewing = false;
												break;
											}

											// Uppdatera aktiv plan
											plan = newPlan;
											break;
										}

									case "↩️ Avbryt":
										SpectreUIHelper.Error("Inget träningsschema sparades.");
										reviewing = false;
										break;






								}
							} 

						}
						catch (Exception ex)
						{
							SpectreUIHelper.Error($"Fel: {ex.Message}");
						}

						// 8) Vänta innan återgång till klientmenyn
						AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
						Console.ReadKey(true);
						break;



					case "🥗 Skapa kostschema (AI-hjälp)":
						//  — review-flöde för kostschema //
						try
						{
							// 1) Hämta frisk (uppdaterad) klient från clientService
							Client freshClient = _clientService.GetClientById(client.Id);

							// 2) Hämta klientens målbeskrivning
							string goal = freshClient.GoalDescription;

							// 3) Fråga PT om dagligt kalorimål
							int calories = AnsiConsole.Ask<int>("Ange dagligt kalorimål (kcal):");


							// 🔥 AI loading (visas tills planen är klar)

							DietPlan plan = null;

							AnsiConsole.Status()
								.Spinner(Spinner.Known.Dots)
								.SpinnerStyle(Style.Parse("green"))
								.Start("AI skapar kostschema... vänligen vänta...", ctx =>
								{

									// 4) Be ScheduleService skapa ett förslag (sparas som pending i service)
									plan = _scheduleService
										.CreateAndLinkDietPlan(freshClient.Id, goal, calories)
										.Result;
								});



							
							if (plan == null)
							{
								SpectreUIHelper.Error("AI kunde inte skapa ett kostschema. Försök igen senare.");
								break;
							}

							// 5) Review-loop: visa plan och låt PT acceptera / generera nytt / avbryta
							bool reviewing = true;
							while (reviewing)
							{
								// Visa schemat i en tabell
								ShowDietPlanReviewTable(plan);

								// Erbjud val
								var action = AnsiConsole.Prompt(
									new SelectionPrompt<string>()
										.Title("Välj åtgärd:")
										.AddChoices("✔ Acceptera och spara", "🔄 Generera nytt", "↩️ Avbryt"));

								switch (action)
								{
									case "✔ Acceptera och spara":
										// NYTT: commit sparar pending-plan till fil och länkar till klient
										var saved = _scheduleService.CommitPendingDietPlan(freshClient.Id);
										if (saved != null)
											SpectreUIHelper.Success($"Kostschema '{saved.Name}' sparat!");
										else
											SpectreUIHelper.Error("Kunde inte spara kostschemat.");
										reviewing = false;
										break;

									case "🔄 Generera nytt":
										// NYTT: anropa AI igen för ett nytt förslag (ersätt plan)
										plan = _scheduleService.CreateAndLinkDietPlan(freshClient.Id, goal, calories).Result;
										if (plan == null)
										{
											SpectreUIHelper.Error("AI kunde inte generera ett nytt schema.");
											reviewing = false;
										}
										// loop fortsätter och visar nya plan
										break;

									case "↩️ Avbryt":
										// Kassera pending-plan (töm sker i service inte här), visa meddelande
										SpectreUIHelper.Error("Inget kostschema sparades.");
										reviewing = false;
										break;
								}
							} // end review loop
						}
						catch (Exception ex)
						{
							SpectreUIHelper.Error($"Fel: {ex.Message}");
						}
						
						// Pausa innan återgång till meny
						AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
						Console.ReadKey(true);
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

        
        // Lista klienter
        
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

		// NYTT: Metod som visar dietplan i en Spectre.Console-tabell så PT kan granska.
		private void ShowDietPlanReviewTable(DietPlan plan)
		{
			// Rensa skärmen och visa planens namn
			AnsiConsole.Clear();

			var table = new Table().Title($"[bold green]{plan.Name}[/]");

			// Kolumner: Dag + Måltider
			table.AddColumn("Dag");
			table.AddColumn("Måltider");

			// Loop genom varje DailyMealPlan
			foreach (var daily in plan.DailyMeals)
			{
				// Samla alla måltider i en sträng
				var mealsText = $"Frukost: {daily.Breakfast}\n" +
								$"Lunch: {daily.Lunch}\n" +
								$"Middag: {daily.Dinner}\n" +
								$"Snacks: {daily.Snacks}\n" +
								$"Totalt: {daily.TotalCalories} kcal";

				// Lägg till en rad i tabellen
				table.AddRow(daily.Day, mealsText);
			}

			AnsiConsole.Write(table);
		}


		// Denna metod ritar upp ett veckoschema i en Spectre.Console-tabell
		// så att PT: kan granska varje dag och övning innan den sparas.

		private void ShowWorkoutPlanReviewTable(WorkoutPlan plan)
		{
			
			AnsiConsole.Clear();

			// Skapar en snygg Spectre.Console-tabell med rundad ram
			var table = new Table()
				.Border(TableBorder.Rounded)
				.Title($"[bold blue]{plan.Name}[/]"); // Visar planens namn högst upp

			// Lägger till kolumner i tabellen
			table.AddColumn(new TableColumn("[yellow]Dag[/]").Centered());        // Kolumn 1: Dag
			table.AddColumn(new TableColumn("[green]Fokusområde[/]").Centered()); // Kolumn 2: Fokusområde
			table.AddColumn(new TableColumn("[cyan]Övningar[/]").LeftAligned());  // Kolumn 3: Listan med övningar

            // Går igenom varje dags träningspass i träningsschemat
            foreach (var day in plan.DailyWorkouts)
            {
                // Bygger en textlista med alla övningar för dagen
                // Format: "Bänkpress — 4 set × 10 reps"
                string exerciseText = string.Join("\n",
                    day.Exercises.Select(ex => $"{ex.Name} — {ex.SetsAndReps}"));



                // Lägger in en rad i tabellen med dagens info
                table.AddRow(
                    $"[bold]{day.Day}[/]",    // Dagens namn (ex: Måndag)
                    day.FocusArea,            // Fokusområde (ex: Bröst/Triceps)
                    exerciseText              // Alla övningar för dagen
                );
            
			}

			// Skriver ut tabellen på skärmen
			AnsiConsole.Write(table);
		}




	}
}