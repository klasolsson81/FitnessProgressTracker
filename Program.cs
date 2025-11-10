using FitnessProgressTracker.UI;

namespace FitnessProgressTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
			Console.WriteLine("Hello, World!");
			Console.WriteLine("Vår GitHub Action fungerar!");

			Console.WriteLine("\nTryck valfri tangent för att avsluta...");
			Console.ReadKey();
            Menu menu = new Menu();
            menu.ShowMainMenu();









        }
    }
}
