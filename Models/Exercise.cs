using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
    public class Exercise
    {
        public string? Name { get; set; } // t.ex. "Bänkpress"
        public string? SetsAndReps { get; set; } // t.ex. "3 set x 10 reps"
    }
}
