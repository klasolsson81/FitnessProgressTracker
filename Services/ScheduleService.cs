using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Services
{
	// ScheduleService ansvarar för att skapa och koppla tränings- och kostscheman till klienter
	
	public class ScheduleService
	{
		// Datalager för klienter
		private readonly IDataStore<Client> _clientStore;


		// Datalager för träningsplaner
		private readonly IDataStore<WorkoutPlan> _workoutStore;


		// Datalager för kostplaner
		private readonly IDataStore<DietPlan> _dietStore;


		// AI-tjänst som genererar planer
		private readonly AiService _aiService;
        private readonly IDataStore<ProgressLog> _logStore;


        // NYTT: temporära fält för review/ACCEPT-flödet
        // NYTT: Dessa håller senaste AI-genererade planer i minnet tills PT accepterar.
        private WorkoutPlan _pendingWorkoutPlan; // NYTT: temporär träningsplan
		private DietPlan _pendingDietPlan;       // NYTT: temporär kostplan


        // Konstruktor: tar emot alla nödvändiga services och datalager
        public ScheduleService(
            IDataStore<Client> clientStore,
            IDataStore<WorkoutPlan> workoutStore,
            IDataStore<DietPlan> dietStore,
            IDataStore<ProgressLog> logStore,
            AiService aiService)

        {
            _clientStore = clientStore;
            _workoutStore = workoutStore;
            _dietStore = dietStore;
            _logStore = logStore;
            _aiService = aiService;
        }

        // Skapar ett träningsschema via AI men sparar EJ till filen.
        // Istället sparas resultatet i _pendingWorkoutPlan för granskning.
        public async Task<WorkoutPlan> CreateAndLinkWorkoutPlan(int clientId, string goal, int daysPerWeek)
		{
			// 1) Anropa AI: be om ett förslag
			WorkoutPlan plan = await _aiService.GenerateWorkoutPlan(goal, daysPerWeek);

			// 2) Kontrollera om AI:n lyckades
			if (plan == null)
			{
				// Returnera null så UI kan visa fel — anropande kod bestämmer beteendet
				return null;
			}

			// 3) För review: sätt ID = 0 (riktigt ID sätts vid commit)
			plan.Id = 0;
			plan.ClientId = clientId;

			// 4) Spara i temporärt fält (ingen filändring sker här)
			_pendingWorkoutPlan = plan; // NYTT: temporärt lagrad

			// 5) Returnera för att UI ska kunna visa/recensera
			return plan;
		}


		// NYTT: Denna metod sparar det pending-schemat till fil och kopplar till klient.
		public WorkoutPlan CommitPendingWorkoutPlan(int clientId)
		{
			if (_pendingWorkoutPlan == null)
				return null;

			// 1) Ladda alla befintliga planer
			List<WorkoutPlan> allPlans = _workoutStore.Load();

			// 2) Sätt ett unikt ID (last id + 1)
			_pendingWorkoutPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;

			// 3) Koppla planen till klienten
			_pendingWorkoutPlan.ClientId = clientId;

			// 4) Spara planen i datalagret
			allPlans.Add(_pendingWorkoutPlan);
			_workoutStore.Save(allPlans);

			// 5) Uppdatera klientens lista med nya plan-id
			List<Client> allClients = _clientStore.Load();
			Client clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

			if (clientToUpdate != null)
			{
				if (clientToUpdate.WorkoutPlanIds == null)
					clientToUpdate.WorkoutPlanIds = new List<int>();

				clientToUpdate.WorkoutPlanIds.Add(_pendingWorkoutPlan.Id);
				_clientStore.Save(allClients);
			}

			// 6) Returnera sparad plan och töm pending
			var saved = _pendingWorkoutPlan;
			_pendingWorkoutPlan = null; // NYTT: töm temporärt fält
			return saved;
		}

		// Genererar kostplan via AI men sparar EJ i fil.
		public async Task<DietPlan> CreateAndLinkDietPlan(int clientId, string goalDescription, int targetCalories)
		{
			// 1) Anropa AI:n för att generera en dietplan
			DietPlan plan = await _aiService.GenerateDietPlan(goalDescription, targetCalories);

			// 2) Kontrollera om AI:n lyckades
			if (plan == null)
			{
				return null;
			}

			// 3) Markera som pending (ID sätts först vid commit)
			plan.Id = 0;
			plan.ClientId = clientId;
			_pendingDietPlan = plan; // NYTT: temporärt lagrad

			// 4) Returnera för review i UI
			return plan;
		}

		
	
		
		// NYTT: Sparar pending dietplan till fil och länkar till klient
		public DietPlan CommitPendingDietPlan(int clientId)
		{
			if (_pendingDietPlan == null)
				return null;

			// 1) Ladda alla kostplaner
			List<DietPlan> allPlans = _dietStore.Load();

			// 2) Sätt nytt ID
			_pendingDietPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;

			// 3) Koppla plan till klient
			_pendingDietPlan.ClientId = clientId;

			// 4) Spara till diet-store
			allPlans.Add(_pendingDietPlan);
			_dietStore.Save(allPlans);

			// 5) Uppdatera klientens DietPlanIds
			List<Client> allClients = _clientStore.Load();
			Client clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

			if (clientToUpdate != null)
			{
				if (clientToUpdate.DietPlanIds == null)
					clientToUpdate.DietPlanIds = new List<int>();

				clientToUpdate.DietPlanIds.Add(_pendingDietPlan.Id);
				_clientStore.Save(allClients);
			}

			// 6) Töm temporär och returnera sparad plan
			var saved = _pendingDietPlan;
			_pendingDietPlan = null; // NYTT: töm temporärt fält
			return saved;
		}

        public void CleanUpClientData(List<int> clientIdsToDelete)
        {
            // === 1. Rensa träningsscheman ===
            List<WorkoutPlan> workouts = _workoutStore.Load();
            // Tar bort alla scheman där schemats ClientId finns i clientIdsToDelete-listan
            workouts.RemoveAll(wp => clientIdsToDelete.Contains(wp.ClientId));
            _workoutStore.Save(workouts);

            // === 2. Rensa kostscheman ===
            List<DietPlan> diets = _dietStore.Load();
            diets.RemoveAll(dp => clientIdsToDelete.Contains(dp.ClientId));
            _dietStore.Save(diets);

            // === 3. Rensa framstegsloggar ===
            List<ProgressLog> logs = _logStore.Load();
            logs.RemoveAll(log => clientIdsToDelete.Contains(log.ClientId));
            _logStore.Save(logs);
        }


    }

}
	

