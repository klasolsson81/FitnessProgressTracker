using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
	// denna klass kommer att representera en loggpost för användarens träningsframsteg i applikationen
	public class ProgressLog
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public double Weight { get; set; }
		public string Notes { get; set; }

        public int ClientId { get; set; }


    }
}
