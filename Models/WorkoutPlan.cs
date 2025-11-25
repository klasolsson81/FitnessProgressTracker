using System;
using System.Collections.Generic;

namespace FitnessProgressTracker.Models
{
    public class WorkoutPlan
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string? Name { get; set; }
        public int Week { get; set; }
        public List<DailyWorkout> DailyWorkouts { get; set; } = new List<DailyWorkout>();
    }
}