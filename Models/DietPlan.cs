using System;
using System.Collections.Generic;

namespace FitnessProgressTracker.Models
{
    public class DietPlan
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string? Name { get; set; }
        public int Week { get; set; }
        public List<DailyMealPlan> DailyMeals { get; set; } = new List<DailyMealPlan>();
    }
}