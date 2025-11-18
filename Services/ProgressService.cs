using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;

namespace FitnessProgressTracker.Services
{
    public class ProgressService
    {
            private readonly IDataStore<ProgressLog> _logStore;
            public ProgressService(IDataStore<ProgressLog> logStore)
            {
                _logStore = logStore;
            }

            // Hämta alla loggar för en specifik klient
            public List<ProgressLog> GetLogsForClient(int clientId)
            {
                var allLogs = _logStore.Load();
                return allLogs.Where(log => log.ClientId == clientId)
                              .OrderByDescending(log => log.Date) // Nyaste först
                              .ToList();
            }
        


    }
}
 