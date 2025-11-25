using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using FitnessProgressTracker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Services
{
    public class ScheduleService
    {
        private readonly IDataStore<Client> _clientStore;
        private readonly IDataStore<WorkoutPlan> _workoutStore;
        private readonly IDataStore<DietPlan> _dietStore;
        private readonly IDataStore<ProgressLog> _logStore;
        private readonly AiService _aiService;

        private WorkoutPlan? _previousWorkoutProposal;
        private WorkoutPlan? _pendingWorkoutPlan;
        private DietPlan? _previousDietProposal;
        private DietPlan? _pendingDietPlan;

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

        public async Task<WorkoutPlan?> CreateAndLinkWorkoutPlan(int clientId, string goal, int daysPerWeek, int week)
        {
            WorkoutPlan? plan = await _aiService.GenerateWorkoutPlan(goal, daysPerWeek);
            if (plan == null) return null;
            plan.Id = 0;
            plan.ClientId = clientId;
            plan.Week = week;
            _pendingWorkoutPlan = plan;
            return plan;
        }

        public WorkoutPlan? CommitPendingWorkoutPlan(int clientId)
        {
            if (_pendingWorkoutPlan == null) return null;
            List<WorkoutPlan> allPlans = _workoutStore.Load();
            _pendingWorkoutPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;
            _pendingWorkoutPlan.ClientId = clientId;
            allPlans.Add(_pendingWorkoutPlan);
            _workoutStore.Save(allPlans);

            List<Client> allClients = _clientStore.Load();
            Client? clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);
            if (clientToUpdate != null)
            {
                if (clientToUpdate.WorkoutPlanIds == null)
                    clientToUpdate.WorkoutPlanIds = new List<int>();
                clientToUpdate.WorkoutPlanIds.Add(_pendingWorkoutPlan.Id);
                _clientStore.Save(allClients);
            }
            var saved = _pendingWorkoutPlan;
            _pendingWorkoutPlan = null;
            return saved;
        }

        public async Task<DietPlan?> CreateAndLinkDietPlan(int clientId, string goalDescription, int targetCalories, int week)
        {
            DietPlan? plan = await _aiService.GenerateDietPlan(goalDescription, targetCalories);
            if (plan == null) return null;
            plan.Id = 0;
            plan.ClientId = clientId;
            plan.Week = week;
            _pendingDietPlan = plan;
            return plan;
        }

        public DietPlan? CommitPendingDietPlan(int clientId)
        {
            if (_pendingDietPlan == null) return null;
            _previousDietProposal = null;
            List<DietPlan> allPlans = _dietStore.Load();
            _pendingDietPlan.Id = allPlans.Count > 0 ? allPlans.Max(p => p.Id) + 1 : 1;
            _pendingDietPlan.ClientId = clientId;
            allPlans.Add(_pendingDietPlan);
            _dietStore.Save(allPlans);

            List<Client> allClients = _clientStore.Load();
            Client? clientToUpdate = allClients.FirstOrDefault(c => c.Id == clientId);
            if (clientToUpdate != null)
            {
                if (clientToUpdate.DietPlanIds == null)
                    clientToUpdate.DietPlanIds = new List<int>();
                clientToUpdate.DietPlanIds.Add(_pendingDietPlan.Id);
                _clientStore.Save(allClients);
            }
            var saved = _pendingDietPlan;
            _pendingDietPlan = null;
            return saved;
        }

        public void CleanUpClientData(List<int> clientIdsToDelete)
        {
            List<WorkoutPlan> workouts = _workoutStore.Load();
            workouts.RemoveAll(wp => clientIdsToDelete.Contains(wp.ClientId));
            _workoutStore.Save(workouts);

            List<DietPlan> diets = _dietStore.Load();
            diets.RemoveAll(dp => clientIdsToDelete.Contains(dp.ClientId));
            _dietStore.Save(diets);

            List<ProgressLog> logs = _logStore.Load();
            logs.RemoveAll(log => clientIdsToDelete.Contains(log.ClientId));
            _logStore.Save(logs);
        }

        public WorkoutPlan? RevertToPreviousWorkoutProposal()
        {
            if (_previousWorkoutProposal == null) return _pendingWorkoutPlan;
            var temp = _pendingWorkoutPlan;
            _pendingWorkoutPlan = _previousWorkoutProposal;
            _previousWorkoutProposal = temp;
            return _pendingWorkoutPlan;
        }

        public DietPlan? RevertToPreviousDietProposal()
        {
            if (_previousDietProposal == null) return _pendingDietPlan;
            var temp = _pendingDietPlan;
            _pendingDietPlan = _previousDietProposal;
            _previousDietProposal = temp;
            return _pendingDietPlan;
        }

        public WorkoutPlan? GetPreviousWorkoutProposal() => _previousWorkoutProposal;
        public DietPlan? GetPreviousDietProposal() => _previousDietProposal;

        public void SavePendingAsPreviousDietProposal()
        {
            if (_pendingDietPlan != null) _previousDietProposal = _pendingDietPlan;
        }

        public void SavePendingAsPreviousWorkoutProposal()
        {
            if (_pendingWorkoutPlan != null) _previousWorkoutProposal = _pendingWorkoutPlan;
        }

        public void DeleteAllPlans()
        {
            _workoutStore.Save(new List<WorkoutPlan>());
            _dietStore.Save(new List<DietPlan>());
        }

        // ==========================================
        // Statistikberäkning (Gemensam logik)
        // ==========================================
        public (int total, int completed, double percentage) GetWorkoutStatistics(int clientId)
        {
            var allWorkouts = _workoutStore.Load();
            var clientPlans = allWorkouts.Where(w => w.ClientId == clientId).ToList();

            int totalPass = 0;
            int completedPass = 0;

            foreach (var plan in clientPlans)
            {
                totalPass += plan.DailyWorkouts.Count;
                completedPass += plan.DailyWorkouts.Count(d => d.IsCompleted);
            }

            double percentage = totalPass > 0 ? (double)completedPass / totalPass * 100 : 0;
            return (totalPass, completedPass, percentage);
        }
    }
}