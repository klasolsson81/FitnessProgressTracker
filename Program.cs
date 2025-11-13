using FitnessProgressTracker.Models;
using FitnessProgressTracker.UI;

namespace FitnessProgressTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");
            //Console.WriteLine("Vår GitHub Action fungerar!");

            //Console.WriteLine("\nTryck valfri tangent för att avsluta...");
            //Console.ReadKey();
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Menu menu = new Menu();
            menu.ShowMainMenu();

           
             PtMenu ptMenu = new PtMenu(); 
             ptMenu.Show(new PT());

            ClientMenu clientMenu = new ClientMenu();
            clientMenu.Show(new Client());











        }
    }
}
