
using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;


namespace FitnessProgressTracker.Services
{
    public class ClientService
    {
        private readonly IDataStore<Client> _clientStore;
        private readonly ScheduleService _scheduleService;
        private ProgressService? _progressService;

        public ClientService(IDataStore<Client> clientStore, ScheduleService scheduleService)
        {
            _clientStore = clientStore;
            _scheduleService = scheduleService;
        }

        public void SetProgressService(ProgressService progressService)
        {
            _progressService = progressService;
        }

        // Hämta alla klienter som tillhör en specifik PT
        public List<Client> GetClientsForPT(int ptId)
        {
            var allClients = _clientStore.Load();
            return allClients.Where(c => c.AssignedPtId == ptId).ToList();
        }

        public void UpdateClientGoals(int clientId, string goalDesc, double targetWeight, int workoutsPerWeek, int targetCalories)
        {
            List<Client> allClients = _clientStore.Load();
            Client? clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

            if (clientToUpdate != null)
            {
                clientToUpdate.GoalDescription = goalDesc;
                clientToUpdate.TargetWeight = targetWeight;
                clientToUpdate.WorkoutsPerWeek = workoutsPerWeek;

                // LÄGG TILL DENNA RAD:
                clientToUpdate.TargetCalories = targetCalories;

                _clientStore.Save(allClients); 
            }
        }

        public Client? GetClientById(int clientId)
        {
            var allClients = _clientStore.Load();
            // Returnera den första klienten som matchar ID, annars null
            return allClients.FirstOrDefault(c => c.Id == clientId);
        }

        public void DeleteClients(List<int> clientIds)
        {
            // 1. Rensa all associerad data (anropar ScheduleService)
            _scheduleService.CleanUpClientData(clientIds);

            // === 2. Huvudlogik (Ta bort klienter) ===
            try
            {
                List<Client> allClients = _clientStore.Load();
                int initialCount = allClients.Count;

                // Använder RemoveAll för att ta bort alla klienter vars ID matchar listan
                allClients.RemoveAll(c => clientIds.Contains(c.Id));

                if (allClients.Count == initialCount)
                {
                    // Om listan inte krympte, betyder det att inga matchande ID:n hittades.
                    throw new InvalidOperationException("Hittade inga klienter att ta bort.");
                }

                _clientStore.Save(allClients); // Spara den uppdaterade listan
            }
            catch (Exception ex)
            {
                // Kasta ett generellt fel uppåt för att UI:t ska kunna hantera det
                throw new Exception("Ett kritiskt fel uppstod vid borttagning av klient(er).", ex);
            }
        }

        public void DeleteAllData()
        {
            // 1. Töm scheman
            _scheduleService.DeleteAllPlans();

            // 2. Töm progress-loggar
            _progressService?.DeleteAllProgress();

            // 3. Töm klientlistan
            _clientStore.Save(new List<Client>());
        }
    }
}