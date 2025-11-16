using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
	// denna klass representerar en dietplan för användaren 
	public class DietPlan
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

        public int ClientId { get; set; }

    }
}
