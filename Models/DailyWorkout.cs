using System;
using System.Collections.Generic;

namespace FitnessProgressTracker.Models
{
    public class DailyWorkout
    {
        public string? Day { get; set; }
        public string? FocusArea { get; set; }
        public bool IsCompleted { get; set; }
        public List<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}