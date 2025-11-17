
using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FitnessProgressTracker.Services
{
    public class ClientService
    {
        private readonly IDataStore<Client> _clientStore;

        public ClientService(IDataStore<Client> clientStore)
        {
            _clientStore = clientStore;
        }

        // Hämta alla klienter som tillhör en specifik PT
        public List<Client> GetClientsForPT(int ptId)
        {
            var allClients = _clientStore.Load();
            return allClients.Where(c => c.AssignedPtId == ptId).ToList();
        }

        public void UpdateClientGoals(int clientId, string goalDesc, double targetWeight, int workoutsPerWeek)
        {
            List<Client> allClients = _clientStore.Load();
            Client clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

            if (clientToUpdate != null)
            {
                clientToUpdate.GoalDescription = goalDesc;
                clientToUpdate.TargetWeight = targetWeight;
                clientToUpdate.WorkoutsPerWeek = workoutsPerWeek;

                _clientStore.Save(allClients); // Spara ändringarna
            }
            
        }
    }
}