using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
    public class DailyWorkout
    {
        public string? Day { get; set; } // t.ex. "Måndag" eller "Dag 1"
        public string? FocusArea { get; set; } // t.ex. "Bröst/Triceps"
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
