using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{
	// skapat pt klass som ärver från user klassen
	public class PT: User
	{
		public List<int> ClientIds { get; set; } = new List<int>();
		public List<int> AppointmentIds { get; set; } = new List<int>();

    }
}
