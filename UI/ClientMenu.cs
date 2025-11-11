using System;
using System.Threading;
using Spectre.Console;
using YourAppName.Client;

namespace YourAppName.Client
{
    public class ClientMenu
    {
        public void RunClientMenu()
        {
            bool running = true;

            while (running)
            {
                Console.Clear();

                var choiceString = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[Green]KlientMeny:[/]")
                        .AddChoices(new[]
                        {
                            "📄 Visa träningsschema",
                            "🥗 Visa kostschema",
                            "🎯 Uppdatera mål",
                            "🏋️ Logga träning",
                            "📊 Se framsteg och statistik",
                            "✉️ Skicka meddelande till PT",
                            "🚪 Logga ut"
                        })
                );

                ClientMenuOption choice = choiceString switch
                {
                    "📄 Visa träningsschema" => ClientMenuOption.VisaTräningsschema,
                    "🥗 Visa kostschema" => ClientMenuOption.VisaKostschema,
                    "🎯 Uppdatera mål" => ClientMenuOption.UppdateraMål,
                    "🏋️ Logga träning" => ClientMenuOption.LoggaTräning,
                    "📊 Se framsteg och statistik" => ClientMenuOption.SeFramstegOchStatistik,
                    "✉️ Skicka meddelande till PT" => ClientMenuOption.SkickaMeddelandeTillPT,
                    "🚪 Logga ut" => ClientMenuOption.LoggaUt,
                    _ => throw new Exception("Ogiltigt val")
                };

                var menuTexts = ClientMenuTexts.GetTexts(choice);

                if (choice == ClientMenuOption.LoggaUt)
                {
                    SpectreUI.Loading(menuTexts.Loading);
                    Thread.Sleep(800);
                    SpectreUI.Success(menuTexts.Result);
                    Thread.Sleep(800);
                    running = false;
                    break;
                }

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .Start(menuTexts.Loading, ctx => Thread.Sleep(1200));

                var resultPanel = new Panel(menuTexts.Result)
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Aqua),
                    Padding = new Padding(1, 1, 1, 1),
                    Header = new PanelHeader("[Yellow]Resultat[/]")
                };
                AnsiConsole.Write(resultPanel);

                AnsiConsole.MarkupLine("[grey]Tryck valfri tangent för att fortsätta...[/]");
                Console.ReadKey();
            }
        }
    }
}
