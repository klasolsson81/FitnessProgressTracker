using FitnessProgressTracker.Models;
using FitnessProgressTracker.UI;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FitnessProgressTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
           
            Console.OutputEncoding = System.Text.Encoding.UTF8; //För att kunna visa symboler i menyerna. 

            //DEPENDENCY INJECTION-KEDJAN

            // 1. Bygg sökvägarna
            string baseDirectory = AppContext.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));
            string clientFilePath = Path.Combine(projectRoot, "data/clients.json");
            string ptFilePath = Path.Combine(projectRoot, "data/pts.json");

            // 2. Skapa dina DataStores
            IDataStore<Client> clientStore = new JsonDataStore<Client>(clientFilePath);
            IDataStore<PT> ptStore = new JsonDataStore<PT>(ptFilePath);

            // 3. Skapa LoginService
            LoginService loginService = new LoginService(clientStore, ptStore);

            // 4. Skapa din Huvudmeny (med din LoginService)
            Menu menu = new Menu(loginService);



            // 5. Kör menyn i en oändlig loop
            while (true)
            {
                menu.ShowMainMenu();
                AnsiConsole.MarkupLine("\nTryck [grey]ENTER[/] för att återgå till menyn...");
                Console.ReadLine();
            }
        }
    }
}