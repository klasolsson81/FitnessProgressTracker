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
                            SpectreUIHelper.Loading("Hämtar klientlista...");
                            SpectreUIHelper.Error("Denna funktion är inte implementerad än.");
                            SpectreUIHelper.Motivation();
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
            // Implementera meny för att hantera enskilda klienter

            SpectreUIHelper.AnimatedBanner($"MÅL FÖR: {client.FirstName.ToUpper()}", Color.Green);
            var goalDesc = AnsiConsole.Ask<string>("Beskriv klientens övergripande mål (t.ex. 'Gå ner i vikt'):");
            var targetWeight = AnsiConsole.Ask<double>($"Ange ny målvikt för {client.FirstName} (kg):");
            var workoutsPerWeek = AnsiConsole.Ask<int>($"Ange antal träningspass per vecka:");


            _clientService.UpdateClientGoals(client.Id, goalDesc, targetWeight, workoutsPerWeek);
            SpectreUIHelper.Success($"Mål uppdaterade för {client.FirstName}!");


        }
    }   
}
