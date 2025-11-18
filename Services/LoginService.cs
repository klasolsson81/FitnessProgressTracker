using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitnessProgressTracker.Services
{
    public class LoginService
    {
        // Nu har vi TVÅ stores, en för varje konkret typ
        private readonly IDataStore<Client> _clientStore;
        private readonly IDataStore<PT> _ptStore;

        // Konstruktorn tar emot båda
        public LoginService(IDataStore<Client> clientStore, IDataStore<PT> ptStore)
        {
            _clientStore = clientStore;
            _ptStore = ptStore;
        }


        // Hjälpmetod för att ladda BÅDE klienter och PTs i en enda lista
        private List<User> LoadAllUsers()
        {
            // Ladda båda listorna
            List<Client> clients = _clientStore.Load();
            List<PT> pts = _ptStore.Load();

            // Slå ihop dem till en gemensam List<User> och returnera
            List<User> allUsers = new List<User>();
            allUsers.AddRange(clients);
            allUsers.AddRange(pts);

            return allUsers;
        }

        public void RegisterClient(string username, string password, string firstName, string lastName)
        {
            // Validera input
            ValidateInput(username, "Användarnamn");
            ValidatePassword(password);
            ValidateInput(firstName, "Förnamn");
            ValidateInput(lastName, "Efternamn");

            try
            {
                // Ladda ALLA användare (både klienter och PTs)
                List<User> allUsers = LoadAllUsers();

                bool usernameExists = allUsers.Any(u =>
                    u.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));

                if (usernameExists)
                {
                    throw new InvalidOperationException("Användarnamnet är redan upptaget.");
                }

                // Ladda BARA klient-listan igen, för att spara
                List<Client> clients = _clientStore.Load();

                var newClient = new Client();
                newClient.Username = username;
                newClient.PasswordHash = password;
                newClient.FirstName = firstName;
                newClient.LastName = lastName;
                newClient.Role = "Client";

                newClient.AssignedPtId = 1; // Kopplar alla nya till PT 1

                // Sätt ID baserat på ALLA användare
                newClient.Id = allUsers.Count > 0 ? allUsers.Max(u => u.Id) + 1 : 1;

                // Lägg till den nya klienten BARA i klient-listan
                clients.Add(newClient);

                // Spara BARA klient-listan
                _clientStore.Save(clients);
            }
            catch (Exception ex)
            {
                throw new Exception("Ett databasfel uppstod...", ex);
            }
        }

        public User Login(string username, string password)
        {
            try
            {
                // Ladda ALLA användare
                List<User> allUsers = LoadAllUsers();

                User user = allUsers.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase) &&
                    u.PasswordHash == password);

                if (user == null)
                {
                    throw new InvalidOperationException("Fel användarnamn eller lösenord.");
                }

                //Eftersom vi laddade från Client/PT-listorna,
                //är detta objekt redan en "Client" eller "PT" under huven.
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ett databasfel uppstod vid inloggning: {ex.Message}", ex);
            }
        }

        //Valideringsmetoder
        private void ValidateInput(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{fieldName} får inte vara tomt eller endast innehålla blanksteg.");

            if (input.Contains(" "))
                throw new ArgumentException($"{fieldName} får inte innehålla blanksteg.");
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Lösenord får inte vara tomt.");

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