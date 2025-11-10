using FitnessProgressTracker.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.UI
{
    public class PtMenu
    {
        // Hej teamet! Visar PT-meny
        public void Show(PT pt)
        {
            try
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
                            "Visa alla klienters utveckling",
                            "Sätt upp mål för klient",
                            "Skapa träningsschema (AI-hjälp)",
                            "Skapa kostschema (AI-hjälp)",
                            "Uppdatera träningsschema",
                            "Ta bort träningsschema",
                            "Ångra senaste ändring",
                            "Skicka meddelande till klient",
                            "Visa grafer och statistik",
                            "Logga ut"));

                switch (choice)
                {
                    case "Visa alla klienters utveckling":
                        SpectreUIHelper.Loading("Hämtar klientdata...");
                        var table = new Table().AddColumns("Klient", "Mål", "Status");
                        table.AddRow("Alex", "Bygga styrka", "[green]Aktiv[/]");
                        table.AddRow("Maja", "Kondition", "[yellow]Under planering[/]");
                        AnsiConsole.Write(table);
                        SpectreUIHelper.Motivation();
                        break;

                    case "Sätt upp mål för klient":
                        SpectreUIHelper.Loading("Sätter upp mål...");
                        AnsiConsole.MarkupLine("[green]Nytt mål sparat![/]");
                        SpectreUIHelper.Motivation();
                        break;

                    case "Skapa träningsschema (AI-hjälp)":
                        SpectreUIHelper.Loading("AI skapar träningsschema...");
                        AnsiConsole.MarkupLine("[green]Schema genererat![/]");
                        SpectreUIHelper.Motivation();
                        break;

                    case "Skapa kostschema (AI-hjälp)":
                        SpectreUIHelper.Loading("AI skapar kostplan...");
                        AnsiConsole.MarkupLine("[green]Kostplan klar![/]");
                        SpectreUIHelper.Motivation();
                        break;

                    case "Uppdatera träningsschema":
                        SpectreUIHelper.Loading("Uppdaterar schema...");
                        AnsiConsole.MarkupLine("[green]Schemat uppdaterat![/]");
                        break;

                    case "Ta bort träningsschema":
                        SpectreUIHelper.Loading("Tar bort schema...");
                        AnsiConsole.MarkupLine("[yellow]Schema borttaget.[/]");
                        break;

                    case "Ångra senaste ändring":
                        SpectreUIHelper.Loading("Ångrar senaste ändring...");
                        AnsiConsole.MarkupLine("[yellow]Senaste ändring återställd.[/]");
                        break;

                    case "Skicka meddelande till klient":
                        SpectreUIHelper.Loading("Skickar meddelande...");
                        AnsiConsole.MarkupLine("[green]Meddelande skickat![/]");
                        SpectreUIHelper.Motivation();
                        break;

                    case "Visa grafer och statistik":
                        SpectreUIHelper.Loading("Hämtar statistik...");
                        AnsiConsole.MarkupLine("[blue]Visar framsteg för alla klienter...[/]");
                        SpectreUIHelper.Motivation();
                        break;

                    case "Logga ut":
                        SpectreUIHelper.Success("Du är nu utloggad. Grymt jobbat coach! 💪");
                        break;
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett fel uppstod i PT-menyn: {ex.Message}");
            }
        }

    }
}
