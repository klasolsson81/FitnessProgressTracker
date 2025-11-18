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
                                "📊 Se framsteg för klienter",
                                "🚪 Logga ut"));

                    AnsiConsole.Clear();

                    switch (choice)
                    {
                        case "👤 Visa min klientlista":
                            // Ta bort Loading här, vi gör det i metoden
                            ShowClientListMenu(pt); // <-- ÄNDRA TILL DETTA
                            break;

                        case "📊 Se framsteg för klienter":
                            SpectreUIHelper.Loading("Hämtar klientdata...");
                            var table = new Table().AddColumns("Klient", "Mål", "Status");
                            table.AddRow("Alex", "Bygga styrka", "[green]Aktiv[/]");
                            table.AddRow("Maja", "Kondition", "[yellow]Under planering[/]");
                            AnsiConsole.Write(table);
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

        public PtMenu(ClientService clientService, ScheduleService scheduleService)
        {
            _clientService = clientService;
            _scheduleService = scheduleService;

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
                        // TODO: Detta kommer i Task #99
                        SpectreUIHelper.Error("Denna funktion kommer i nästa uppdatering (Task #99).");
                        Thread.Sleep(2000);
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
