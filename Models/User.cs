using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Models
{

	// skapat user klass som är abstrakt den kommer att ärvas av andra klasser, alltså denna är bas klass 
	public abstract class User
	{
		public int Id { get; set; }
		public string Username { get; set; }
		public string PasswordHash { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
        public string Role { get; set; }


    }
}
