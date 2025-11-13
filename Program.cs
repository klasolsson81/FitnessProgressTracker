using FitnessProgressTracker.Models;
using FitnessProgressTracker.UI;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System;
using System.IO;
using System.Collections.Generic;

namespace FitnessProgressTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Bygg en korrekt sökväg till projekt-roten
            string baseDirectory = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));

            // 1. Skapa sökvägar till BÅDA filerna
            string clientFilePath = Path.Combine(projectRoot, "data/clients.json");
            string ptFilePath = Path.Combine(projectRoot, "data/pts.json");

            // 2. Skapa TVÅ DataStores, en för varje typ
            IDataStore<Client> clientStore = new JsonDataStore<Client>(clientFilePath);
            IDataStore<PT> ptStore = new JsonDataStore<PT>(ptFilePath);

            // 3. Skapa vår LoginService och "injicera" BÅDA stores
            LoginService loginService = new LoginService(clientStore, ptStore);

            // 4. Skapa vår Huvudmeny
            Menu mainMenu = new Menu(loginService);

 
            //KOD FÖR ATT RENSA ENDAST KLIENTER (Avkommentera för att köra)
            //List<Client> emptyClientList = new List<Client>();
            //clientStore.Save(emptyClientList); 


            // 5. Kör huvudmenyn i en oändlig loop
            while (true)
            {
                mainMenu.ShowMainMenu();
                AnsiConsole.MarkupLine("\nTryck [grey]ENTER[/] för att återgå till menyn...");
                Console.ReadLine();
            }
        }
    }
}