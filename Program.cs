using FitnessProgressTracker.Models;
using FitnessProgressTracker.UI;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using Spectre.Console;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using DotNetEnv;

namespace FitnessProgressTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {

            {
                // 1. Fixa encoding och ladda .env (Viktigt för AI)
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                DotNetEnv.Env.Load();

                // 2. Sökvägar
                string baseDirectory = AppContext.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../"));

                string clientPath = Path.Combine(projectRoot, "data/clients.json");
                string ptPath = Path.Combine(projectRoot, "data/pts.json");
                string workoutPath = Path.Combine(projectRoot, "data/workouts.json"); // <--- NY
                string dietPath = Path.Combine(projectRoot, "data/diets.json");       // <--- NY
                string logsPath = Path.Combine(projectRoot, "data/logs.json"); 

                // 3. Skapa ALLA DataStores (Dessa måste finnas först!)
                IDataStore<Client> clientStore = new JsonDataStore<Client>(clientPath);
                IDataStore<PT> ptStore = new JsonDataStore<PT>(ptPath);
                IDataStore<WorkoutPlan> workoutStore = new JsonDataStore<WorkoutPlan>(workoutPath); // <--- NY
                IDataStore<DietPlan> dietStore = new JsonDataStore<DietPlan>(dietPath);       // <--- NY
                IDataStore<ProgressLog> logsStore = new JsonDataStore<ProgressLog>(logsPath);

                // 4. Skapa Services (Beroende Injection)

                // AiService (behövs av ScheduleService)
                AiService aiService = new AiService();

                // ClientService (behövs av Menu och PtMenu)
                ClientService clientService = new ClientService(clientStore);

                // ScheduleService (behövs av PtMenu - denna behöver MASSOR av dependencies)
                ScheduleService scheduleService = new ScheduleService(clientStore, workoutStore, dietStore, aiService);

                // LoginService (behövs av Menu)
                LoginService loginService = new LoginService(clientStore, ptStore);

                // ProgressService (behövs av PtMenu för att visa klientens framsteg)
                ProgressService progressService = new ProgressService(logsStore);

                // 5. Skapa Huvudmenyn (Nu finns alla variabler!)
                Menu mainMenu = new Menu(loginService, clientService, scheduleService, progressService);

                // 6. Kör loopen
                while (true)
                {
                    mainMenu.ShowMainMenu();
                    AnsiConsole.MarkupLine("\nTryck [grey]ENTER[/] för att återgå till menyn...");
                    Console.ReadLine();
                }
            }
        }
    }
}