using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FitnessProgressTracker.Services
{
    public class LoginService
    {
        private readonly IDataStore<User> _userStore;

        // Måste vara IDataStore<User>
        public LoginService(IDataStore<User> userStore)
        {
            _userStore = userStore;
        }

        public void RegisterClient(string username, string password, string firstName, string lastName)
        {
            try
            {
                ValidateInput(username, "Användarnamn");
                ValidatePassword(password);
                ValidateInput(firstName, "Förnamn");
                ValidateInput(lastName, "Efternamn");

                List<User> allUsers = _userStore.Load();

                bool usernameExists = allUsers.Any(u =>
                    u.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));

                if (usernameExists)
                {
                    throw new InvalidOperationException("Användarnamnet är redan upptaget.");
                }

                var newClient = new Client();
                newClient.Username = username;
                newClient.PasswordHash = password; // ändra till PasswordHash
                newClient.FirstName = firstName;
                newClient.LastName = lastName;

                if (allUsers.Count == 0)
                {
                    newClient.Id = 1;
                }
                else
                {
                    newClient.Id = allUsers.Max(u => u.Id) + 1;
                }

                allUsers.Add(newClient);
                _userStore.Save(allUsers);
            }
            catch (Exception ex)
            {
                throw new Exception("Ett databasfel uppstod...", ex);
            }
        }

        private void ValidateInput(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{fieldName} får inte vara tomt eller endast innehålla blanksteg.");

            if (input.Contains(" "))

                throw new ArgumentException($"{fieldName} får inte innehålla blanksteg.");
        }

        private void ValidatePassword(string password)
        {
            if (password.Length < 8)
                throw new ArgumentException("Lösenordet måste vara minst 8 tecken långt.");

            if (!password.Any(char.IsUpper))
                throw new ArgumentException("Lösenordet måste innehålla minst en stor bokstav.");

            if (!password.Any(char.IsLower))
                throw new ArgumentException("Lösenordet måste innehålla minst en liten bokstav.");

            if (!password.Any(char.IsDigit))
                throw new ArgumentException("Lösenordet måste innehålla minst en siffra.");
        }
    }
}
