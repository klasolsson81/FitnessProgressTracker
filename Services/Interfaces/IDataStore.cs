using FitnessProgressTracker.Models; 
using System.Collections.Generic;

namespace FitnessProgressTracker.Services.Interfaces
{
    
    public interface IDataStore<T>
    {
        List<T> Load();
        void Save(List<T> data);
    }
}
