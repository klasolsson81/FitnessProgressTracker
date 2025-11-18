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
                        Client freshClient = _clientService.GetClientById(client.Id); //REFRESHA KLIENTOBJEKTET FÖR ATT FÅ SENASTE MÅLET!

                        // 1. Hämta klientens redan sparade målbeskrivning
                        var goal = client.GoalDescription;

						// 2. Fråga PT om dagligt kalorimål
						var calories = AnsiConsole.Ask<int>("Ange dagligt kalorimål (kcal):");

						// 3. Visa laddnings-animation medan AI jobbar
						SpectreUIHelper.Loading("AI skapar kostschema, vänligen vänta...");

						// 4. Anropa ScheduleService - AI - spara - koppla till klienten
						var newDietPlan = _scheduleService
							.CreateAndLinkDietPlan(client.Id, goal, calories)
							.Result;

						// 5. Bekräfta att allt gick bra
						SpectreUIHelper.Success(
							$"Nytt kostschema '{newDietPlan.Name}' skapat!"
						);

						// 6. Vänta innan vi återgår till menyn
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

    }   
}
