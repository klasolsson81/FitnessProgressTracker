using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
	// denna klass kommer att representera en bokad tid för användaren i applikationen
	public class Appointment
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public int ClientId { get; set; }
		public int PtId { get; set; }
		public string Notes { get; set; }

    }
}
