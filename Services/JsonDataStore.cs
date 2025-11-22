using FitnessProgressTracker.Services.Interfaces;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System;

namespace FitnessProgressTracker.Services
{
    public class JsonDataStore<T> : IDataStore<T>
    {
        // Vi behöver en privat variabel för att hålla filsökvägen
        private readonly string _filePath;

        // En konstruktor som tvingar oss att ange en filsökväg
        public JsonDataStore(string filePath)
        {
            _filePath = filePath;
        }

        public List<T> Load()
        {
            try
            {
                // 1. Kontrollera om filen existerar
                if (!File.Exists(_filePath))
                {
                    // Filen finns inte, returnera en ny, tom lista.
                    return new List<T>();
                }

                // 2. Läs all text från filen
                string json = File.ReadAllText(_filePath);

                //    Detta fångar "null", "" (tom sträng) och " " (mellanslag)
                if (string.IsNullOrWhiteSpace(json))
                {
                    // Filen är tom eller innehåller bara whitespace, returnera tom lista
                    return new List<T>();
                }

                // 4. Konvertera (Deserialisera) JSON-texten till en List<T> och returnera den
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (Exception ex)
            {
                // Hantera fel om filen är korrupt (t.ex. innehåller "{}")
                Console.WriteLine($"Kunde inte ladda data från {_filePath}: {ex.Message}");

                // VIKTIGT: Om filen är korrupt, rensa den genom att spara en tom lista
                Console.WriteLine($"Försöker reparera filen genom att rensa den...");
                Save(new List<T>()); // Skriv över den korrupta filen med en tom lista
                return new List<T>(); // Returnera en tom lista
            }
        }

        public void Save(List<T> data)
        {
            try
            {
                // 1. Kontrollera om mappen ("data/") existerar
                string? directory = Path.GetDirectoryName(_filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    // Skapa mappen om den inte finns
                    Directory.CreateDirectory(directory);
                }

                // 2. Ställ in snygg formatering för JSON-filen
                var options = new JsonSerializerOptions { WriteIndented = true };

                // 3. Konvertera (Serialisera) er List<T> till en JSON-sträng
                string json = JsonSerializer.Serialize(data, options);

                // 4. Skriv (spara) JSON-strängen till filen
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // Hantera fel om filen inte kan skrivas
                Console.WriteLine($"Kunde inte spara data till {_filePath}: {ex.Message}");
            }
        }
    }
}