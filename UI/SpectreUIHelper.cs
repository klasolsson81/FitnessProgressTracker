using FitnessProgressTracker.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}

