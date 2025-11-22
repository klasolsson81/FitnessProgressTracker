using FitnessProgressTracker.Models;
using FitnessProgressTracker.Services;
using FitnessProgressTracker.Services.Interfaces;
using FitnessProgressTracker.UI;
using Spectre.Console;

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

                AiService aiService = new AiService();
                             
                ScheduleService scheduleService = new ScheduleService(clientStore, workoutStore, dietStore, logsStore, aiService);
                               
                ClientService clientService = new ClientService(clientStore, scheduleService);
                                
                ProgressService progressService = new ProgressService(logsStore, clientService);
                                
                clientService.SetProgressService(progressService);
                               
                LoginService loginService = new LoginService(clientStore, ptStore);

                
                
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