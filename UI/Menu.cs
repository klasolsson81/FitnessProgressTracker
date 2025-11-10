using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace FitnessProgressTracker.UI
{
    public class Menu
    {
        public void ShowMainMenu()
        {
            {
                try
                {
                    // Sätter mörk bakgrund för gymkänsla
                    AnsiConsole.Background = Color.Grey15;
                    AnsiConsole.Clear();

                    //  Visar animerad banner
                    SpectreUIHelper.AnimatedBanner("FITNESS PROGRESS TRACKER", Color.Yellow);

                    //  Liten motiverande text under bannern
                    AnsiConsole.MarkupLine("[italic green]No Pain, No Gain![/]");

                    //  Skapar menyval för användaren
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[bold cyan]Välj ett alternativ:[/]")
                            .AddChoices("Registrera konto", "Logga in", "Logga ut"));

                    //  Hanterar användarens val
                    switch (choice)
                    {
                        case "Registrera konto":
                            SpectreUIHelper.Loading("Skapar nytt konto...");
                            // LoginService.Register();
                            AnsiConsole.MarkupLine("[green]Konto skapat! Dags att träna![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "Logga in":
                            SpectreUIHelper.Loading("Loggar in...");
                            // LoginService.Login();
                            AnsiConsole.MarkupLine("[green]Inloggning lyckades![/]");
                            SpectreUIHelper.Motivation();
                            break;

                        case "Logga ut":
                            SpectreUIHelper.Success("Tack för idag! Håll dig stark! 💪");
                            Environment.Exit(0);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    //  Fångar fel och visar det med SpectreUIHelper
                    SpectreUIHelper.Error($"Ett fel uppstod i huvudmenyn: {ex.Message}");
                }
            }
        }
    }
}
              



