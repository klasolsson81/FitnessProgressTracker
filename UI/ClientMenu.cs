
using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FitnessProgressTracker.UI
{
	public class ClientMenu
	{
		// ClientMenu - Menyn som klienten ser efter inloggning
		// Här kan klienten se scheman, logga träning och kolla framsteg
		
		
			// Services för att hantera data
			private readonly ClientService _clientService;
			private readonly ScheduleService _scheduleService;
			private readonly ProgressService _progressService;
			private readonly IDataStore<WorkoutPlan> _workoutStore;
			private readonly IDataStore<DietPlan> _dietStore;

			// Konstruktor - Förbereder alla verktyg som behövs
			public ClientMenu(
				ClientService clientService,
				ScheduleService scheduleService,
				ProgressService progressService)
			{
				_clientService = clientService;
				_scheduleService = scheduleService;
				_progressService = progressService;

				// VIKTIGT: Bygg samma sökväg som Program.cs använder
				// Annars hittar vi inte JSON-filerna!
				string baseDirectory = AppContext.BaseDirectory;
				string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));
				string workoutPath = Path.Combine(projectRoot, "data/workouts.json");
				string dietPath = Path.Combine(projectRoot, "data/diets.json");

				_workoutStore = new JsonDataStore<WorkoutPlan>(workoutPath);
				_dietStore = new JsonDataStore<DietPlan>(dietPath);
			}

			// Huvudmetod - Visar menyn och hanterar användarens val
			public void Show(Client client)
			{
				try
				{
					bool isRunning = true;

					while (isRunning)
					{
						// Hämta FÄRSK klientdata varje gång (så vi får senaste uppdateringarna)
						Client? freshClient = _clientService.GetClientById(client.Id);
						if (freshClient == null)
						{
							SpectreUIHelper.Error("Kunde inte hämta dina uppgifter.");
							return;
						}

						// Visa menyn
						AnsiConsole.Background = Color.Grey15;
						AnsiConsole.Clear();
						SpectreUIHelper.AnimatedBanner("CLIENT MODE", Color.Cyan1);
						AnsiConsole.MarkupLine("[italic green]Stay consistent, stay strong![/]");
						AnsiConsole.MarkupLine($"[dim yellow]Välkommen {freshClient.FirstName}! Välj vad du vill göra idag.[/]");

						// Låt användaren välja
						var choice = AnsiConsole.Prompt(
							new SelectionPrompt<string>()
								.Title("[bold cyan]Välj ett alternativ:[/]")
								.AddChoices(
									"💪 Visa träningsschema",
									"🥗 Visa kostschema",
									"🎯 Visa mina mål",
									"📘 Logga träning",
									"📊 Se framsteg och statistik",
									"🚪 Logga ut"));

						AnsiConsole.Clear();

						// Kör rätt metod baserat på valet
						switch (choice)
						{
							case "💪 Visa träningsschema":
								ShowWorkoutSchedule(freshClient);
								break;

							case "🥗 Visa kostschema":
								ShowDietSchedule(freshClient);
								break;

							case "🎯 Visa mina mål":
								ShowClientGoals(freshClient);
								break;

							case "📘 Logga träning":
								LogWorkout(freshClient);
								break;

							case "📊 Se framsteg och statistik":
								ShowProgressStats(freshClient);
								break;


							case "🚪 Logga ut":
								SpectreUIHelper.Success("Du är utloggad. Bra jobbat idag! 💪");
								isRunning = false;
								continue;
						}

						// Pausa innan vi visar menyn igen
						if (isRunning)
						{
							AnsiConsole.MarkupLine("\n[grey]Tryck på valfri tangent för att återgå till menyn...[/]");
							Console.ReadKey(true);
						}
					}
				}
				catch (Exception ex)
				{
					SpectreUIHelper.Error($"Ett fel uppstod i klientmenyn: {ex.Message}");
				}
			}

			// ========================================
			// VISA TRÄNINGSSCHEMA
			// ========================================
			private void ShowWorkoutSchedule(Client client)
			{
				SpectreUIHelper.Loading("Hämtar träningsschema...");

				// Hämta alla träningsscheman från databasen
				List<WorkoutPlan> allWorkouts = _workoutStore.Load();

				// Filtrera ut bara DENNA klients scheman (där ClientId matchar)
				var clientWorkouts = allWorkouts
					.Where(w => w.ClientId == client.Id)
					.OrderByDescending(w => w.Id)
					.ToList();

				// Om inga scheman finns
				if (clientWorkouts.Count == 0)
				{
					AnsiConsole.MarkupLine("[yellow]Du har inget träningsschema ännu.[/]");
					AnsiConsole.MarkupLine("[dim]Din PT har inte skapat ett schema för dig än.[/]");
					SpectreUIHelper.Motivation();
					return;
				}

				// Om flera scheman finns - låt klienten välja
				WorkoutPlan selectedWorkout;
				if (clientWorkouts.Count == 1)
				{
					selectedWorkout = clientWorkouts[0];
				}
				else
				{
					AnsiConsole.MarkupLine($"[green]Du har {clientWorkouts.Count} träningsscheman. Välj vilket du vill se:[/]\n");

					selectedWorkout = AnsiConsole.Prompt(
						new SelectionPrompt<WorkoutPlan>()
							.Title("[cyan]Välj träningsschema:[/]")
							.PageSize(10)
							.AddChoices(clientWorkouts)
							.UseConverter(w => $"📅 {w.Name ?? "Namnlöst schema"} [grey](ID: {w.Id})[/]")
					);
				}

				// Visa schemat
				DisplayWorkoutPlan(selectedWorkout);
				SpectreUIHelper.Motivation();
			}

			// Visar träningsschemat i en snygg tabell
			private void DisplayWorkoutPlan(WorkoutPlan plan)
			{
				AnsiConsole.Clear();
				var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };

				var table = new Table()
					.Border(TableBorder.Heavy)
					.Title($"[bold yellow]{plan.Name}[/]")
					.AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12))
					.AddColumn(new TableColumn("[bold blue]Fokusområde[/]").Centered().Width(15))
					.AddColumn(new TableColumn("[bold cyan]Övningar[/]").LeftAligned().Width(50));

				// Gå igenom varje veckodag och fyll i tabellen
				for (int i = 0; i < weekDays.Length; i++)
				{
					var dayName = weekDays[i];
					var workoutDay = plan.DailyWorkouts.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName));

					string focusAreaText;
					string textContent;

					// Om ingen träning finns för denna dag = vilodag
					if (workoutDay == null)
					{
						focusAreaText = "[magenta]Vilodag[/]";
						textContent = "[magenta]Återhämtning: Ta en promenad eller stretcha.[/]";
					}
					else
					{
						// Bygg en lista med alla övningar
						var exercisesToDisplay = workoutDay.Exercises.ToList();
						string exerciseTextRaw = string.Join("\n",
							exercisesToDisplay.Select(ex =>
								$"[cyan]{(ex.Name ?? "Okänd övning").Trim()}[/] — [grey]{(ex.SetsAndReps ?? "-").Trim()}[/]"
							));

						textContent = exerciseTextRaw;
						focusAreaText = $"[blue]{workoutDay.FocusArea}[/]";
					}

					table.AddRow($"[yellow]{dayName}[/]", focusAreaText, textContent);
				}

				AnsiConsole.Write(table);
			}

			// ========================================
			// VISA KOSTSCHEMA
			// ========================================
			private void ShowDietSchedule(Client client)
			{
				SpectreUIHelper.Loading("Hämtar kostschema...");

				// Hämta alla kostscheman
				List<DietPlan> allDiets = _dietStore.Load();

				// Filtrera ut bara DENNA klients scheman
				var clientDiets = allDiets
					.Where(d => d.ClientId == client.Id)
					.OrderByDescending(d => d.Id)
					.ToList();

				// Om inga scheman finns
				if (clientDiets.Count == 0)
				{
					AnsiConsole.MarkupLine("[yellow]Du har inget kostschema ännu.[/]");
					AnsiConsole.MarkupLine("[dim]Din PT har inte skapat ett schema för dig än.[/]");
					SpectreUIHelper.Motivation();
					return;
				}

				// Om flera scheman finns - låt klienten välja
				DietPlan selectedDiet;
				if (clientDiets.Count == 1)
				{
					selectedDiet = clientDiets[0];
				}
				else
				{
					AnsiConsole.MarkupLine($"[green]Du har {clientDiets.Count} kostscheman. Välj vilket du vill se:[/]\n");

					selectedDiet = AnsiConsole.Prompt(
						new SelectionPrompt<DietPlan>()
							.Title("[cyan]Välj kostschema:[/]")
							.PageSize(10)
							.AddChoices(clientDiets)
							.UseConverter(d => $"🍎 {d.Name ?? "Namnlöst schema"} [grey](ID: {d.Id})[/]")
					);
				}

				// Visa schemat
				DisplayDietPlan(selectedDiet);
				SpectreUIHelper.Motivation();
			}

			// Visar kostschemat i en snygg tabell
			private void DisplayDietPlan(DietPlan plan)
			{
				AnsiConsole.Clear();
				var weekDays = new[] { "Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag" };

				var table = new Table()
					.Border(TableBorder.Heavy)
					.Title($"[bold green]{plan.Name}[/]");

				table.AddColumn(new TableColumn("[bold yellow]DAG[/]").Centered().Width(12));
				table.AddColumn(new TableColumn("[bold green]MÅLTIDER[/]").LeftAligned().Width(15));
				table.AddColumn(new TableColumn("[bold white]KOSTPLAN[/]").LeftAligned().Width(50));

				// Gå igenom varje veckodag
				for (int i = 0; i < weekDays.Length; i++)
				{
					var dayName = weekDays[i];
					var mealDay = plan.DailyMeals.FirstOrDefault(d => d.Day != null && d.Day.Contains(dayName));

					var mealSlots = "Frukost\nLunch\nMiddag\nSnacks\n[bold white]Totalt:[/]";

					if (mealDay == null)
					{
						string mealDetails = "[grey]Ingen kostplan satt för denna dag.[/]";
						table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", mealDetails);
					}
					else
					{
						// Bygg en sträng med alla måltider + totala kalorier
						var mealDetails = $"[white]{mealDay.Breakfast}[/]\n" +
										$"[white]{mealDay.Lunch}[/]\n" +
										$"[white]{mealDay.Dinner}[/]\n" +
										$"[white]{mealDay.Snacks}[/]\n" +
										$"[bold yellow]{mealDay.TotalCalories} kcal[/]";

						table.AddRow($"[yellow]{dayName}[/]", $"[green]{mealSlots}[/]", mealDetails);
					}

					if (i < weekDays.Length - 1)
						table.AddEmptyRow();
				}

				AnsiConsole.Write(table);
			}

			// ========================================
			// VISA KLIENTENS MÅL
			// ========================================
			private void ShowClientGoals(Client client)
			{
				SpectreUIHelper.Loading("Hämtar dina mål...");

				// Hämta färsk klientdata
				Client? freshClient = _clientService.GetClientById(client.Id);
				if (freshClient == null)
				{
					SpectreUIHelper.Error("Kunde inte hämta dina uppgifter.");
					return;
				}

				AnsiConsole.Clear();
				SpectreUIHelper.AnimatedBanner("DINA MÅL", Color.Green);

				// Visa målen i en tabell
				var goalTable = new Table()
					.Border(TableBorder.Rounded)
					.AddColumn(new TableColumn("[bold cyan]Område[/]").Centered())
					.AddColumn(new TableColumn("[bold yellow]Värde[/]").LeftAligned());

				goalTable.AddRow("[cyan]Målbeskrivning[/]", freshClient.GoalDescription ?? "[grey]Ej angivet[/]");
				goalTable.AddRow("[cyan]Målvikt[/]", $"[yellow]{freshClient.TargetWeight} kg[/]");
				goalTable.AddRow("[cyan]Träningspass per vecka[/]", $"[yellow]{freshClient.WorkoutsPerWeek} pass[/]");
				goalTable.AddRow("[cyan]Dagligt kalorimål[/]", $"[yellow]{freshClient.TargetCalories} kcal[/]");

				AnsiConsole.Write(goalTable);
				AnsiConsole.WriteLine();

				// Visa de 3 senaste loggarna
				var logs = _progressService.GetLogsForClient(freshClient.Id)?.Take(3).ToList();
				if (logs != null && logs.Any())
				{
					AnsiConsole.MarkupLine("[bold underline green]📊 Senaste 3 loggarna:[/]");
					var logTable = new Table();
					logTable.AddColumn("Datum");
					logTable.AddColumn("Vikt (kg)");
					logTable.AddColumn("Anteckning");

					foreach (var log in logs)
					{
						logTable.AddRow(
							log.Date.ToShortDateString(),
							log.Weight.ToString("0.0"),
							log.Notes ?? ""
						);
					}

					AnsiConsole.Write(logTable);
				}

				SpectreUIHelper.Motivation();
			}

			// ========================================
			// LOGGA TRÄNING
			// ========================================
			private void LogWorkout(Client client)
			{
				try
				{
					AnsiConsole.Clear();
					SpectreUIHelper.AnimatedBanner("LOGGA TRÄNING", Color.Blue);

					// Fråga om datum (standard = idag)
					var date = AnsiConsole.Prompt(
						new TextPrompt<DateTime>("[cyan]Datum (yyyy-MM-dd):[/]")
							.DefaultValue(DateTime.Now)
					);

					// Fråga om vikt (måste vara mellan 0-300 kg)
					var weight = AnsiConsole.Prompt(
						new TextPrompt<double>("[cyan]Aktuell vikt (kg):[/]")
							.Validate(w => w > 0 && w < 300
								? ValidationResult.Success()
								: ValidationResult.Error("[red]Ange en giltig vikt.[/]"))
					);

					// Fråga om anteckning (valfritt)
					var notes = AnsiConsole.Ask<string>("[cyan]Anteckning (valfritt):[/]", string.Empty);

					SpectreUIHelper.Loading("Sparar träningslogg...");

					// Hämta befintliga loggar för att skapa rätt ID
					var allLogs = _progressService.GetLogsForClient(client.Id)?.ToList() ?? new List<ProgressLog>();

					// Skapa ny logg
					var newLog = new ProgressLog
					{
						Id = allLogs.Count > 0 ? allLogs.Max(l => l.Id) + 1 : 1,
						ClientId = client.Id,
						Date = date,
						Weight = weight,
						Notes = notes
					};

					// Spara loggen
					_progressService.AddProgressLog(newLog);

					SpectreUIHelper.Success($"Träning loggad! Vikt: {weight} kg, Datum: {date.ToShortDateString()}");
					SpectreUIHelper.Motivation();
				}
				catch (Exception ex)
				{
					SpectreUIHelper.Error($"Kunde inte logga träning: {ex.Message}");
				}
			}

			// ========================================
			// VISA FRAMSTEG OCH STATISTIK
			// ========================================
			private void ShowProgressStats(Client client)
			{
				SpectreUIHelper.Loading("Hämtar statistik...");

				// Anropar ProgressService som visar alla loggar i en tabell
				_progressService.ShowClientProgress(client.Id);

				SpectreUIHelper.Motivation();
			}

			
		}
	}


