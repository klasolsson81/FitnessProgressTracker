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


		// Konstruktor: tar emot alla nödvändiga services och datalager
		public ScheduleService(
		IDataStore<Client> clientStore,
		IDataStore<WorkoutPlan> workoutStore,
		IDataStore<DietPlan> dietStore,
		AiService aiService)
		{
			_clientStore = clientStore;   // Sätter klient-databasen
			_workoutStore = workoutStore; // Sätter träningsschema-databasen
			_dietStore = dietStore;       // Sätter kostschema-databasen
			_aiService = aiService;       // Sätter AI-tjänsten
		}

		// Skapar ett träningsschema via AI, sparar det och länkar det till klienten.
		// Returnerar det skapade WorkoutPlan-objektet.
		public async Task<WorkoutPlan> CreateAndLinkWorkoutPlan(int clientId, string goal, int daysPerWeek)
		{
			// 1) Anropa AI-tjänsten för att generera ett förslag
			WorkoutPlan plan = await _aiService.GenerateWorkoutPlan(goal, daysPerWeek);


			// 2) Kontrollera om AI:n lyckades
			if (plan == null)
			{
				throw new System.Exception("AI-tjänsten kunde inte generera en plan.");
			}


			// 3) Ladda alla befintliga träningsplaner från lagret
			List<WorkoutPlan> allPlans = _workoutStore.Load();


			// 4) Sätt ett unikt ID (last id + 1). Om inga planer finns, starta på 1.
			plan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;


			// 5) Koppla planen till klienten
			plan.ClientId = clientId;


			// 6) Spara det nya schemat i workouts.json (via datalagret)
			allPlans.Add(plan);
			_workoutStore.Save(allPlans);


			// 7) Uppdatera klientens lista med det nya schemat
			List<Client> allClients = _clientStore.Load();
			Client clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);


			if (clientToUpdate != null)
			{
				// Lägg till schemats ID i klientens lista
				clientToUpdate.WorkoutPlanIds.Add(plan.Id);


				// Spara ändringarna i klientlagret
				_clientStore.Save(allClients);
			}


			// 8) Returnera det färdiga schemat
			return plan;
		}


			// Skapar ett kostschema via AI, sparar det och länkar det till klienten.
			// Returnerar det skapade DietPlan-objektet.
      public async Task<DietPlan> CreateAndLinkDietPlan(int clientId, string goalDescription, int targetCalories)
		{
			// 1) Anropa AI-tjänsten för att generera ett kostschema
			DietPlan plan = await _aiService.GenerateDietPlan(goalDescription, targetCalories);


			// 2) Kontrollera om AI:n lyckades
			if (plan == null)
			{
				throw new System.Exception("AI-tjänsten kunde inte generera en plan.");
			}


			// 3) Ladda alla befintliga kostplaner
			List<DietPlan> allPlans = _dietStore.Load();


			// 4) Sätt ett unikt ID
			plan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;


			// 5) Koppla planen till klienten
			plan.ClientId = clientId;


			// 6) Spara det nya kostschemat
			allPlans.Add(plan);
			_dietStore.Save(allPlans);


			// 7) Uppdatera klientens lista med dietplanens ID
			List<Client> allClients = _clientStore.Load();
			Client clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);


			if (clientToUpdate != null)
			{
				clientToUpdate.DietPlanIds.Add(plan.Id);
				_clientStore.Save(allClients);
			}


			// 8) Returnera det färdiga kostschemat
			return plan;
		}
	}
	}

