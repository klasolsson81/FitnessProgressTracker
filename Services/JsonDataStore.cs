using FitnessProgressTracker.Services.Interfaces; 
using System.Collections.Generic; 

namespace FitnessProgressTracker.Services
{
    public class JsonDataStore<T> : IDataStore<T>
    {
        
        public List<T> Load()
        {
            // TODO: Lägg till kod för att ladda från JSON här
            throw new System.NotImplementedException();
        }

        
        public void Save(List<T> data)
        {
            // TODO: Lägg till kod för att spara till JSON här
            throw new System.NotImplementedException();
        }
    }
}