using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
    // skapat workoutplan klass som kommer att användas för att skapa träningsplaner för klienter
    public class WorkoutPlan
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string? Name { get; set; } // t.ex. "Viktnedgång 4 dagar"
        public List<DailyWorkout> DailyWorkouts { get; set; } = new List<DailyWorkout>();
    }
}
