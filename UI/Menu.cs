using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Models;

namespace FitnessProgressTracker.UI
{
    public class Menu
    {
        private readonly LoginService _loginService;

        public Menu(LoginService loginService)
        {
            _loginService = loginService;
        }

        public void ShowMainMenu()
        {
            try
            {
                AnsiConsole.Background = Color.Grey15;
                AnsiConsole.Clear();

                SpectreUIHelper.AnimatedBanner("FITNESS PROGRESS TRACKER", Color.Yellow);
                AnsiConsole.MarkupLine("[italic green]No Pain, No Gain![/]");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold cyan]Välj ett alternativ:[/]")
                        .AddChoices("Registrera konto", "Logga in", "Avsluta"));

                switch (choice)
                {
                    case "Registrera konto":
                        try
                        {
                            AnsiConsole.MarkupLine("\n[bold blue]Ange uppgifter för ditt nya konto:[/]");

                            // 1. Ställ frågor
                            var username = AnsiConsole.Ask<string>("[cyan1]Ange Användarnamn[/] [italic grey](inga mellanslag):[/]");
                            var firstName = AnsiConsole.Ask<string>("[cyan1]Ange Förnamn[/] [italic grey](inga mellanslag):[/]");
                            var lastName = AnsiConsole.Ask<string>("[cyan1]Ange Efternamn[/] [italic grey](inga mellanslag):[/]");

                            // 2. Ställ lösenordsfrågan med INBYGGD validering
                            var password = AnsiConsole.Prompt(
                                new TextPrompt<string>("[cyan1]Ange Lösenord[/] [italic grey](min 8 tecken, en siffra, en stor/liten bokstav):[/]")
                                    .Secret() // <-- Döljer lösenordet
                                    .Validate(pass =>
                                    {
                                        if (pass.Length < 8)
                                            return ValidationResult.Error("[red]Lösenordet måste vara minst 8 tecken.[/]");
                                        if (!pass.Any(char.IsUpper))
                                            return ValidationResult.Error("[red]Måste innehålla minst en stor bokstav.[/]");
                                        if (!pass.Any(char.IsLower))
                                            return ValidationResult.Error("[red]Måste innehålla minst en liten bokstav.[/]");
                                        if (!pass.Any(char.IsDigit))
                                            return ValidationResult.Error("[red]Måste innehålla minst en siffra.[/]");

                                        return ValidationResult.Success();
                                    })
                            );

                            // 3. Visa laddning
                            SpectreUIHelper.Loading("Registrerar konto...");

                            // 4. Anropa LoginService (som kör sin EGEN validering som en sista säkerhetskoll)
                            _loginService.RegisterClient(username, password, firstName, lastName);

                            // 5. Visa framgång
                            SpectreUIHelper.Success($"Konto skapat för {firstName}! Välkommen!");
                            SpectreUIHelper.Motivation();
                        }
                        catch (ArgumentException ex)
                        {
                            SpectreUIHelper.Error(ex.Message);
                        }
                        catch (InvalidOperationException ex)
                        {
                            SpectreUIHelper.Error(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            SpectreUIHelper.Error($"Ett oväntat fel uppstod: {ex.Message}");
                        }
                        break;


                    case "Logga in":
                        try
                        {
                            AnsiConsole.MarkupLine("\n[bold blue]Ange dina inloggningsuppgifter:[/]");

                            // 1. Ställ frågor
                            var username = AnsiConsole.Ask<string>("[cyan1]Ange Användarnamn[/]:");
                            var password = AnsiConsole.Prompt(
                                new TextPrompt<string>("[cyan1]Ange Lösenord[/]:").Secret()
                            );

                            SpectreUIHelper.Loading("Loggar in...");

                            // 2. Anropa Login-metoden (som returnerar en User)
                            User loggedInUser = _loginService.Login(username, password);

                            SpectreUIHelper.Success($"Välkommen tillbaka, {loggedInUser.FirstName}!");
                            Thread.Sleep(1000); // Kort paus

                            // 3. KONTROLLERA ROLLEN och visa rätt meny
                            if (loggedInUser.Role == "Client")
                            {
                                // Skapa och visa Klient-menyn
                                ClientMenu clientMenu = new ClientMenu();
                                clientMenu.Show((Client)loggedInUser); 
                            }
                            else if (loggedInUser.Role == "PT")
                            {
                                // Skapa och visa PT-menyn
                                PtMenu ptMenu = new PtMenu(_clientService, _scheduleService, _progressService);
                                ptMenu.Show((PT)loggedInUser);
                            }
                            else
                            {
                                SpectreUIHelper.Error("Användarrollen är okänd. Loggar ut.");
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            SpectreUIHelper.Error(ex.Message); // T.ex. "Fel användarnamn eller lösenord."
                        }
                        catch (Exception ex)
                        {
                            SpectreUIHelper.Error($"Ett oväntat fel uppstod: {ex.Message}");
                        }
                        break;

                    case "Avsluta":
                        SpectreUIHelper.Success("Tack för idag! Håll dig stark! 💪");
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"Ett kritiskt fel uppstod i huvudmenyn: {ex.Message}");
            }
        }

        // Lägg till nya fält
        private readonly ClientService _clientService;
        private readonly ScheduleService _scheduleService;
        private readonly ProgressService _progressService;


        // Uppdatera konstruktorn
        public Menu(LoginService loginService, ClientService clientService,
             ScheduleService scheduleService, ProgressService progressService)
        {
            _loginService = loginService;
            _clientService = clientService;
            _scheduleService = scheduleService;
            _progressService = progressService;
        }

    }
}
