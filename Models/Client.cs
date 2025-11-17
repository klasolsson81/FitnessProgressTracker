using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{

    // skapat client klass som ärver från user klassen 
    public class Client : User
    {
        
		public WorkoutPlan CurrentWorkoutPlan { get; set; }
		public DietPlan CurrentDietPlan { get; set; }// Koppling till PT
        public int AssignedPtId { get; set; }

        // Kopplingar till scheman och loggar (bara ID:n)
        public List<int> WorkoutPlanIds { get; set; } = new List<int>();
        public List<int> DietPlanIds { get; set; } = new List<int>();
        public List<int> ProgressLogIds { get; set; } = new List<int>();
    }
}
