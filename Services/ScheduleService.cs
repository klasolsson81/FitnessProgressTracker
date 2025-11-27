using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using FitnessProgressTracker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Services
{
	// denna klass hanterar skapande, sparande och radering av tränings- och kostplaner.

    public class ScheduleService
    {
		// Dessa variabler håller kopplingar till olika "databaser"
		// så vi kan läsa och spara klienter, träningsplaner, kostplaner och loggar.
		private readonly IDataStore<Client> _clientStore;
        private readonly IDataStore<WorkoutPlan> _workoutStore;
        private readonly IDataStore<DietPlan> _dietStore;
        private readonly IDataStore<ProgressLog> _logStore;

		// AI-tjänst som genererar tränings- och kostplane
		private readonly AiService _aiService;

		// TEMPORÄRA PLANER for dem två kost och träningsplaner

		private WorkoutPlan? _previousWorkoutProposal;
        private WorkoutPlan? _pendingWorkoutPlan;
        private DietPlan? _previousDietProposal;
        private DietPlan? _pendingDietPlan;

		//  kontruktör : Klassen behöver dessa för att kunna spara och läsa data.
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

		// 1 SKAPA TRÄNINGSPLAN (men spara inte än)
		public async Task<WorkoutPlan?> CreateAndLinkWorkoutPlan(int clientId, string goal, int daysPerWeek, int week)
        {
            WorkoutPlan? plan = await _aiService.GenerateWorkoutPlan(goal, daysPerWeek);
            if (plan == null) return null;

			// Sätt grundinformation innan den sparas
			plan.Id = 0;
            plan.ClientId = clientId;
            plan.Week = week;

			// Lägg den i pending = något som väntar på att sparas
			_pendingWorkoutPlan = plan;
            return plan;
        }

		// 2. SPARA TRÄNINGSPLANEN PERMANENT
		
		public WorkoutPlan? CommitPendingWorkoutPlan(int clientId)
		{
			if (_pendingWorkoutPlan == null) return null; // om inget finns att spara

			// Hämta alla redan sparade träningsplaner
			List<WorkoutPlan> allPlans = _workoutStore.Load();

			// Ge planen ett nytt ID (max befintligt + 1)
			_pendingWorkoutPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;

			// Se till att klient-ID stämmer
			_pendingWorkoutPlan.ClientId = clientId;

			// Lägg in den i listan och spara datan
			allPlans.Add(_pendingWorkoutPlan);
			_workoutStore.Save(allPlans);

			// Länka planens ID till rätt klient
			List<Client> allClients = _clientStore.Load();
			Client? clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

			if (clientToUpdate != null)
			{
				// Skapa lista om den inte finns
				if (clientToUpdate.WorkoutPlanIds == null)
					clientToUpdate.WorkoutPlanIds = new List<int>();

				// Lägg till denna plans ID
				clientToUpdate.WorkoutPlanIds.Add(_pendingWorkoutPlan.Id);
				_clientStore.Save(allClients);
			}

			// Rensa pending och returnera sparade planen
			var saved = _pendingWorkoutPlan;
			_pendingWorkoutPlan = null;
			return saved;
		}

		
		// 3. SKAPA KOSTPLAN (men spara inte än)
		
		public async Task<DietPlan?> CreateAndLinkDietPlan(int clientId, string goalDescription, int targetCalories, int week)
		{
			// AI genererar kostplan
			DietPlan? plan = await _aiService.GenerateDietPlan(goalDescription, targetCalories);
			if (plan == null) return null;

			// Grundinfo
			plan.Id = 0;
			plan.ClientId = clientId;
			plan.Week = week;

			_pendingDietPlan = plan; // Lägg i vänteläge
			return plan;
		}

	
		// . SPARA KOSTPLANEN PERMANENT
		
		public DietPlan? CommitPendingDietPlan(int clientId)
		{
			if (_pendingDietPlan == null) return null;

			// Nollställ tidigare förslag
			_previousDietProposal = null;

			List<DietPlan> allPlans = _dietStore.Load();

			// Sätt nytt ID
			_pendingDietPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;
			_pendingDietPlan.ClientId = clientId;

			allPlans.Add(_pendingDietPlan);
			_dietStore.Save(allPlans);

			// Länka plan till klient
			List<Client> allClients = _clientStore.Load();
			Client? clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);

			if (clientToUpdate != null)
			{
				if (clientToUpdate.DietPlanIds == null)
					clientToUpdate.DietPlanIds = new List<int>();

				clientToUpdate.DietPlanIds.Add(_pendingDietPlan.Id);
				_clientStore.Save(allClients);
			}

			// Rensa pending och returnera sparade planen
			var saved = _pendingDietPlan;
			_pendingDietPlan = null;
			return saved;
		}

		
		// . RADERA ALLA PLANER FÖR VISSA KLIENTER
		
		public void CleanUpClientData(List<int> clientIdsToDelete)
		{
			// Radera deras träningsplaner
			List<WorkoutPlan> workouts = _workoutStore.Load();
			workouts.RemoveAll(wp => clientIdsToDelete.Contains(wp.ClientId));
			_workoutStore.Save(workouts);

			// Radera deras kostplaner
			List<DietPlan> diets = _dietStore.Load();
			diets.RemoveAll(dp => clientIdsToDelete.Contains(dp.ClientId));
			_dietStore.Save(diets);

			// Radera deras loggar
			List<ProgressLog> logs = _logStore.Load();
			logs.RemoveAll(log => clientIdsToDelete.Contains(log.ClientId));
			_logStore.Save(logs);
		}

		
		//  ÅNGRA: ÅTERGÅ TILL TIDIGARE TRÄNINGSFÖRSLAG
		
		public WorkoutPlan? RevertToPreviousWorkoutProposal()
		{
			if (_previousWorkoutProposal == null) return _pendingWorkoutPlan;

			// Byt plats mellan previous och pending (swap)
			var temp = _pendingWorkoutPlan;
			_pendingWorkoutPlan = _previousWorkoutProposal;
			_previousWorkoutProposal = temp;

			return _pendingWorkoutPlan;
		}

		// Ångra kostplan
		public DietPlan? RevertToPreviousDietProposal()
		{
			if (_previousDietProposal == null) return _pendingDietPlan;

			var temp = _pendingDietPlan;
			_pendingDietPlan = _previousDietProposal;
			_previousDietProposal = temp;

			return _pendingDietPlan;
		}

		// Hämta tidigare förslag
		public WorkoutPlan? GetPreviousWorkoutProposal() => _previousWorkoutProposal;
		public DietPlan? GetPreviousDietProposal() => _previousDietProposal;

		// Spara pending → previous (för ångra-funktion)
		public void SavePendingAsPreviousDietProposal()
		{
			if (_pendingDietPlan != null) _previousDietProposal = _pendingDietPlan;
		}

		public void SavePendingAsPreviousWorkoutProposal()
		{
			if (_pendingWorkoutPlan != null) _previousWorkoutProposal = _pendingWorkoutPlan;
		}

		
		//  RADERA ALLA PLANER I SYSTEMET
		
		public void DeleteAllPlans()
		{
			_workoutStore.Save(new List<WorkoutPlan>());
			_dietStore.Save(new List<DietPlan>());
		}

		
		//  STATISTIK: HUR MÅNGA PASS KLARENDE?
		
		public (int total, int completed, double percentage) GetWorkoutStatistics(int clientId)
		{
			// Hämta alla träningsplaner
			var allWorkouts = _workoutStore.Load();

			// Filtrera fram klientens planer
			var clientPlans = allWorkouts.Where(w => w.ClientId == clientId).ToList();

			int totalPass = 0;      // Alla träningspass
			int completedPass = 0;  // De som är markerade som klara

			// Gå igenom varje plan
			foreach (var plan in clientPlans)
			{
				totalPass += plan.DailyWorkouts.Count;                   // Lägg till totalen
				completedPass += plan.DailyWorkouts.Count(d => d.IsCompleted); // Räkna klara pass
			}

			// Räkna ut procent (klar / total)
			double percentage = totalPass > 0 ? (double)completedPass / totalPass * 100 : 0;

			return (totalPass, completedPass, percentage);
		}
	}
}