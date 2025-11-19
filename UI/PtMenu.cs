using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace FitnessProgressTracker.UI
{
    public class PtMenu
    {
        

        // Visar PT-menyn
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
                                "🚪 Logga ut"));

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "👤 Visa min klientlista":
                            // Ta bort Loading här, vi gör det i metoden
                            ShowClientListMenu(pt); // <-- ÄNDRA TILL DETTA
                            break;

                        case "📊 Se framsteg och statistik":
                            SpectreUIHelper.Loading("Hämtar klientdata...");

                            // Hämta alla klienter som PT ansvarar för
                            var clients = _clientService.GetClientsForPT(pt.Id);

                            foreach (var client in clients)
                            {
                                // Hämta framsteg för klienten
                                List<ProgressLog> logs = _progressService.GetLogsForClient(client.Id);

                                if (logs.Count == 0)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]{client.FirstName} {client.LastName} har inga framsteg loggade ännu.[/]");
                                    continue;
                                }

                                // Skapa tabell
                                var table = new Table().AddColumns("Datum", "Vikt (kg)", "Noteringar");

                                // Fyll tabellen med loggar
                                foreach (var log in logs)
                                {
                                    table.AddRow(
                                        log.Date.ToShortDateString(),
                                        log.Weight.ToString(),
                                        log.Notes
                                    );
                                }

                                // Skriv ut klientnamn + tabell
                                AnsiConsole.MarkupLine($"[bold underline]{client.FirstName} {client.LastName}[/]");
                                AnsiConsole.Write(table);
                                AnsiConsole.WriteLine(); // tom rad mellan klienter
                            }

                            SpectreUIHelper.Motivation();
                            break;

                        case "🗑️ Ta bort klient(er)":
                            ShowDeleteClientPrompt(pt);
                            break;

                        case "🚪 Logga ut":
                            SpectreUIHelper.Success("Du är nu utloggad. Grymt jobbat coach! 💪");
                            isRunning = false;
                            continue; // hoppar över "tryck för att fortsätta"
                    }
                    // Vänta på att användaren trycker en tangent innan menyn visas igen
                    AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
                    Console.ReadKey(true);
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i PT-menyn: {ex.Message}");
            }
        }

        private readonly ClientService _clientService;
        private readonly ScheduleService _scheduleService;
        private readonly ProgressService _progressService;


        public PtMenu(ClientService clientService, ScheduleService scheduleService, ProgressService progressService)
        {
            _clientService = clientService;
            _scheduleService = scheduleService;
            _progressService = progressService;
        }

        private void ShowClientActionMenu(Client client)
        {
            bool inSubMenu = true;
            while (inSubMenu)
            {
                AnsiConsole.Clear();
                SpectreUIHelper.AnimatedBanner($"HANTERA: {client.FirstName.ToUpper()}", Color.Green);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Vad vill du göra med [green]{client.FirstName}[/]?")
                        .AddChoices(
                            "🎯 Sätt upp mål",
                            "🤖 Skapa träningsschema (AI-hjälp)",
                            "🥗 Skapa kostschema (AI-hjälp)",
                            "📊 Se framsteg och statistik",
                            "↩️ Gå tillbaka"
                        ));

                switch (choice)
                {
                    case "🎯 Sätt upp mål":
                        // Logiken vi redan har gjort
                        var goalDesc = AnsiConsole.Ask<string>("Beskriv klientens övergripande mål (t.ex. 'Gå ner i vikt'):");
                        var targetWeight = AnsiConsole.Ask<double>($"Ange ny målvikt för {client.FirstName} (kg):");
                        var workoutsPerWeek = AnsiConsole.Ask<int>($"Ange antal träningspass per vecka:");

                        _clientService.UpdateClientGoals(client.Id, goalDesc, targetWeight, workoutsPerWeek);
                        client = _clientService.GetClientById(client.Id); //Uppdatera klientens mål

                        SpectreUIHelper.Success($"Mål uppdaterade för {client.FirstName}!");

                        AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
                        Console.ReadKey(true);
                        break;

                    case "🤖 Skapa träningsschema (AI-hjälp)":
                        // TODO: Detta kommer i Task #97 (när ScheduleService är klar)
                        SpectreUIHelper.Error("Denna funktion kommer i nästa uppdatering (Task #97).");
                        Thread.Sleep(2000);
                        break;

					case "🥗 Skapa kostschema (AI-hjälp)":
						// ===== NYTT: START — review-flöde för kostschema =====
						try
						{
							// 1) Hämta frisk (uppdaterad) klient från clientService
							Client freshClient = _clientService.GetClientById(client.Id);

							// 2) Hämta klientens målbeskrivning
							string goal = freshClient.GoalDescription;

							// 3) Fråga PT om dagligt kalorimål
							int calories = AnsiConsole.Ask<int>("Ange dagligt kalorimål (kcal):");

							// 4) Be ScheduleService skapa ett förslag (sparas som pending i service)
							var plan = _scheduleService.CreateAndLinkDietPlan(freshClient.Id, goal, calories).Result;
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
                        // TODO: Detta kommer i Task #100
                        SpectreUIHelper.Error("Denna funktion kommer i nästa uppdatering (Task #100).");
                        Thread.Sleep(2000);
                        break;

                    case "↩️ Gå tillbaka":
                        inSubMenu = false;
                        break;
                }
            }
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

        		

		private void ShowClientListMenu(PT pt)
        {
            SpectreUIHelper.Loading("Hämtar dina klienter...");

            // 1. Hämta listan från databasen
            var clients = _clientService.GetClientsForPT(pt.Id);

            if (clients.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Du har inga klienter kopplade till dig än.[/]");
                return;
            }

            // 2. Visa listan och låt PT välja
            var selectedClient = AnsiConsole.Prompt(
                new SelectionPrompt<Client>()
                    .Title("Välj en [cyan]klient[/] att hantera:")
                    .AddChoices(clients)
                    .UseConverter(c => $"{c.FirstName} {c.LastName}") // Visar namnet snyggt
            );

            // 3. NU har vi en vald klient! Skicka den vidare.
            ShowClientActionMenu(selectedClient);
        }

        private void ShowDeleteClientPrompt(PT pt)
        {
            try
            {
                // 1. Hämta klienter att välja från
                var clients = _clientService.GetClientsForPT(pt.Id);

                if (clients.Count == 0)
                {
                    SpectreUIHelper.Error("Det finns inga klienter att ta bort.");
                    return;
                }

                // 2. Använd MultiSelectionPrompt för att välja 0 till många klienter
                var selectedClients = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<Client>()
                        .Title("[bold red]Välj KLIENTER som ska tas bort:[/]")
                        .InstructionsText("[grey](Tryck [blue]<space>[/] för att välja, [green]<enter>[/] för att bekräfta)[/]")
                        .PageSize(10)
                        .AddChoices(clients)
                        .UseConverter(c => $"❌ {c.FirstName} {c.LastName}") // Visar namn snyggt
                );

                if (selectedClients.Count == 0)
                    return; // Ingen vald, gå tillbaka

                // 3. Bekräftelsefråga (Viktigt säkerhetssteg)
                bool confirm = AnsiConsole.Confirm($"Vill du verkligen ta bort {selectedClients.Count} klient(er)? Denna åtgärd går inte att ångra.");

                if (confirm)
                {
                    // 4. Kalla servicen med alla valda ID:n
                    List<int> clientIdsToDelete = selectedClients.Select(c => c.Id).ToList();
                    _clientService.DeleteClients(clientIdsToDelete);

                    SpectreUIHelper.Success($"Borttagning lyckades! {selectedClients.Count} klient(er) raderades.");
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error(ex.Message);
            }
        }



    }   
}
