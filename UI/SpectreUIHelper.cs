using FitnessProgressTracker.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FitnessProgressTracker.UI
{
    public class SpectreUIHelper
    {
        // Visar en animerad banner med färg
        public static void AnimatedBanner(string text, Color color)
        {
            AnsiConsole.MarkupLine("");
            foreach (char c in text)
            {
                AnsiConsole.Markup($"[{color.ToMarkup()}]{c}[/]");
                Thread.Sleep(20);
            }
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{color.ToMarkup()}]====================================[/]");
        }

        // Visar laddningsanimation med text
        public static void Loading(string message)
        {
            AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start(message, ctx => Thread.Sleep(800));
        }

        // Visar framgångsmeddelande
        public static void Success(string message)
        {
            AnsiConsole.MarkupLine($"[bold green]{message}[/]");
        }

        // Visar felmeddelande
        public static void Error(string message)
        {
            AnsiConsole.MarkupLine($"[bold red]{message}[/]");
        }

        // Visar slumpmässig motivationsfras
        public static void Motivation()
        {
            string[] quotes =
            {
            "Push yourself because no one else is going to do it for you!",
            "No excuses. Just results.",
            "It never gets easier, you just get stronger.",
            "Train like a beast, look like a beauty."
            };
            var random = new Random();
            var quote = quotes[random.Next(quotes.Length)];
            AnsiConsole.MarkupLine($"[italic yellow]{quote}[/]");
        }

        // ============================================================
        // NY GEMENSAM METOD FÖR ATT RITA DASHBOARD
        // ============================================================
        public static void ShowDashboardVisuals(Client client, List<ProgressLog> logs, (int total, int completed, double percentage) stats)
        {
            AnsiConsole.Clear();
            SpectreUIHelper.AnimatedBanner($"STATISTIK: {client.FirstName}", Color.Green);

            // 1. MÅLÖVERSIKT
            // Hämta aktuell vikt (senaste loggen där vikt > 0)
            string currentWeight = "Ingen logg";
            var weightLog = logs.OrderByDescending(l => l.Date).FirstOrDefault(l => l.Weight > 0);
            if (weightLog != null) currentWeight = $"{weightLog.Weight} kg";

            AnsiConsole.MarkupLine("[bold underline green]🎯 Mål och Nuläge[/]");
            var goalTable = new Table();
            goalTable.AddColumn("Målbeskrivning");
            goalTable.AddColumn("Målvikt");
            goalTable.AddColumn("Nuvarande vikt");
            goalTable.AddColumn("Pass/v");
            goalTable.AddColumn("Kcal/dag");

            goalTable.AddRow(
                client.GoalDescription ?? "-",
                $"{client.TargetWeight} kg",
                $"[green]{currentWeight}[/]",
                client.WorkoutsPerWeek.ToString(),
                client.TargetCalories.ToString()
            );
            AnsiConsole.Write(goalTable);
            AnsiConsole.WriteLine();

            // 2. VIKT-GRAF
            var recentWeightLogs = logs.Where(l => l.Weight > 0).OrderBy(l => l.Date).TakeLast(10).ToList();

            if (recentWeightLogs.Any())
            {
                AnsiConsole.MarkupLine("[bold underline green]⚖️ Viktutveckling[/]");
                var chart = new BarChart()
                    .Width(60)
                    .Label("[green bold]Vikt (kg)[/]")
                    .CenterLabel();

                foreach (var log in recentWeightLogs)
                {
                    chart.AddItem(log.Date.ToString("MM-dd"), Math.Round(log.Weight, 1), Color.Yellow);
                }
                AnsiConsole.Write(chart);
                AnsiConsole.WriteLine();
            }

            // 3. TRÄNINGS-STATISTIK
            var statGrid = new Grid();
            statGrid.AddColumn();
            statGrid.AddColumn();

            int bars = (int)(stats.percentage / 5);
            string progressBar = $"[[{new string('|', bars)}{new string('.', 20 - bars)}]]";
            string color = stats.percentage == 100 ? "green" : (stats.percentage > 50 ? "yellow" : "red");

            statGrid.AddRow(new Markup("[bold]Totalt antal pass:[/]"), new Markup($"{stats.total}"));
            statGrid.AddRow(new Markup("[bold]Genomförda pass:[/]"), new Markup($"[green]{stats.completed}[/]"));
            statGrid.AddRow(new Markup("[bold]Genomförandegrad:[/]"), new Markup($"[{color}]{stats.percentage:0}% {progressBar}[/]"));

            AnsiConsole.MarkupLine("[bold underline green]💪 Träningsdisciplin[/]");
            AnsiConsole.Write(statGrid);
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            // 4. SENASTE AKTIVITETER
            AnsiConsole.MarkupLine("[bold underline green]📋 Senaste aktiviteter[/]");
            // Sortera nyast först för urval, sen äldst->nyast för visning
            var latestLogs = logs.OrderByDescending(l => l.Date).Take(5).OrderBy(l => l.Date).ToList();

            if (!latestLogs.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inga aktiviteter loggade ännu.[/]");
            }
            else
            {
                var logTable = new Table().Border(TableBorder.Rounded);
                logTable.AddColumn("Datum");
                logTable.AddColumn("Händelse");

                foreach (var log in latestLogs)
                {
                    string desc = log.Weight > 0
                        ? $"Vägning: [yellow]{log.Weight} kg[/] ({log.Notes})"
                        : log.Notes ?? "";

                    logTable.AddRow(log.Date.ToShortDateString(), desc);
                }
                AnsiConsole.Write(logTable);
            }

            AnsiConsole.MarkupLine("\n[grey]Tryck tangent för att fortsätta...[/]");
            Console.ReadKey(true);
        }
    }
}