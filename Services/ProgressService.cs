using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;

namespace FitnessProgressTracker.Services
{
    public class ProgressService
    {
        private readonly IDataStore<ProgressLog> _logStore;
        private readonly ClientService _clientService;

        public ProgressService(IDataStore<ProgressLog> logStore, ClientService clientService)
        {
            _logStore = logStore;
            _clientService = clientService;
        }

        // Hämta alla loggar för en specifik klient (null-säker)
        public List<ProgressLog> GetLogsForClient(int clientId)
        {
            var allLogs = _logStore.Load() ?? new List<ProgressLog>();

            return allLogs
                .Where(log => log != null && log.ClientId == clientId)
                .OrderByDescending(log => log.Date)
                .ToList();
        }

		// LÄGG TILL DENNA METOD UNDER GetLogsForClient:
		public void AddProgressLog(ProgressLog log)
		{
			var allLogs = _logStore.Load() ?? new List<ProgressLog>();
			allLogs.Add(log);
			_logStore.Save(allLogs);
		}

		// Visa loggar i tabell
		public void ShowClientProgress(int clientId)
        {
            // 1. Hämta klient
            var client = _clientService.GetClientById(clientId);
            if (client == null)
            {
                AnsiConsole.MarkupLine("[red]Klient kunde inte hittas.[/]");
                return;
            }

            // 2. Hämta loggar
            var logs = GetLogsForClient(clientId);



            // 3. Visa tabell
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold underline green]Progress för {client.FirstName ?? "N/A"} {client.LastName ?? ""}[/]");

            if (logs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Inga progress-loggar registrerade ännu.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Datum");
            table.AddColumn("Vikt (kg)");
            table.AddColumn("Anteckning");

            foreach (var log in logs)
            {
                table.AddRow(
                    log.Date.ToShortDateString(),
                    log.Weight.ToString("0.0"),
                    log.Notes ?? ""
                );
            }

            AnsiConsole.Write(table);
        }

        public void AddProgressLog(ProgressLog log)
        {
            try
            {
                var allLogs = _logStore.Load() ?? new List<ProgressLog>();
                allLogs.Add(log);
                _logStore.Save(allLogs);
            }
            catch (Exception ex)
            {
                throw new Exception("Kunde inte spara träningslogg.", ex);
            }
        }



        public void DeleteAllProgress()
        {
            // Använd _logStore istället för _progressStore
            _logStore.Save(new List<ProgressLog>());
        }
    }


}
