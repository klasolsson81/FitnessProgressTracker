using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{

	// skapat client klass som ärver från user klassen 
	public class Client: User
	{
		public int AssignedPtId { get; set; }
		public WorkoutPlan CurrentWorkoutPlan { get; set; }
		public DietPlan CurrentDietPlan { get; set; }

    }
}
