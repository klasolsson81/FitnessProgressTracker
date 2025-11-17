using DotNetEnv;
using FitnessProgressTracker.Models;
using FitnessProgressTracker.UI;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FitnessProgressTracker.Services
{
    public class AiService
    {
        private readonly OpenAIClient _client;
        private readonly string _modelName = "gpt-4o-mini";

        public AiService()
        {
            // Hämta nyckeln från .env-filen
            string apiKey = Env.GetString("OPENAI_API_KEY");
            _client = new OpenAIClient(apiKey);
        }

        public async Task<WorkoutPlan> GenerateWorkoutPlan(string goal, int daysPerWeek)
        {
            // 1. Skapa en *mycket* detaljerad System Prompt
            string systemPrompt = @"
        Du är en expert-PT. Din uppgift är att skapa ett träningsschema.
        Användaren kommer att ge dig ett mål och antal dagar.
        Du ska ENDAST svara med en JSON-struktur som matchar följande C# klasser:

        public class WorkoutPlan {
            public int Id { get; set; } // Använd ALLTID 0 som Id
            public string Name { get; set; } // t.ex. 'Viktnedgång 4-dagars'
            public List<DailyWorkout> DailyWorkouts { get; set; }
        }
        public class DailyWorkout {
            public string Day { get; set; } // t.ex. 'Dag 1 (Måndag)'
            public string FocusArea { get; set; } // t.ex. 'Bröst & Triceps'
            public List<Exercise> Exercises { get; set; }
        }
        public class Exercise {
            public string Name { get; set; } // t.ex. 'Bänkpress'
            public string SetsAndReps { get; set; } // t.ex. '3 set x 10 reps'
        }
    ";

            string userPrompt = $"Skapa ett träningsschema för målet '{goal}' med {daysPerWeek} träningspass per vecka. Dela upp passen i logiska dagar (t.ex. 'Dag 1 (Måndag)', 'Dag 2 (Tisdag)' osv.).";

            // 2. Skapa anropet
            var chatClient = _client.GetChatClient(_modelName);
            var chatMessages = new List<ChatMessage>
    {
        new SystemChatMessage(systemPrompt),
        new UserChatMessage(userPrompt)
    };
            var responseOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            try
            {
                // 3. Anropa AI:n
                ChatCompletion response = await chatClient.CompleteChatAsync(chatMessages, responseOptions);
                string aiResponseJson = response.Content.ToString();

                // 4. Omvandla AI:ns JSON-svar till vårt NYA C#-objekt
                WorkoutPlan plan = JsonSerializer.Deserialize<WorkoutPlan>(aiResponseJson);
                return plan;
            }
            catch (Exception ex)
            {
                SpectreUIHelper.Error($"OpenAI API-anrop misslyckades: {ex.Message}");
                return null;
            }
        }

        public async Task<DietPlan> GenerateDietPlan(string goalDescription, int targetCalories)
        {
            // 1. Skapa en *mycket* detaljerad System Prompt
            // Denna definierar AI:ns roll och det EXAKTA JSON-formatet vi kräver.
            string systemPrompt = $@"
        Du är en expert-dietist och PT. Din uppgift är att skapa en 7-dagars kostplan.
        Användaren kommer att ge dig ett mål och ett dagligt kalorimål.
        Du ska ENDAST svara med en JSON-struktur som matchar följande C# klasser:

        public class DietPlan {{
            public int Id {{ get; set; }} // Använd ALLTID 0 som Id
            public string Name {{ get; set; }} // t.ex. 'Viktnedgång 2200 kcal'
            public List<DailyMealPlan> DailyMeals {{ get; set; }}
        }}
        public class DailyMealPlan {{
            public string Day {{ get; set; }} // t.ex. 'Måndag'
            public string Breakfast {{ get; set; }} // t.ex. 'Havregrynsgröt med bär'
            public string Lunch {{ get; set; }} // t.ex. 'Kycklingsallad'
            public string Dinner {{ get; set; }} // t.ex. 'Lax med sötpotatis'
            public string Snacks {{ get; set; }} // t.ex. 'En frukt och en näve nötter'
            public int TotalCalories {{ get; set; }} // Ska vara så nära {targetCalories} som möjligt
        }}

        INSTRUKTIONER:
        1. Skapa en plan för exakt 7 dagar ('Måndag', 'Tisdag', ..., 'Söndag').
        2. 'Name'-fältet ska reflektera målet och kalorierna.
        3. 'TotalCalories' för varje dag ska vara så nära {targetCalories} kcal som möjligt.
        4. Ge varierade och hälsosamma måltidsförslag.
    ";

            string userPrompt = $"Skapa en 7-dagars kostplan för målet '{goalDescription}' med ett totalt kaloriintag på cirka {targetCalories} kcal per dag.";

            // 2. Skapa anropet
            var chatClient = _client.GetChatClient(_modelName);
            var chatMessages = new List<ChatMessage>
    {
        new SystemChatMessage(systemPrompt),
        new UserChatMessage(userPrompt)
    };

            // 3. Tvinga JSON-svar
            var responseOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            try
            {
                // 4. Anropa AI:n
                ChatCompletion response = await chatClient.CompleteChatAsync(chatMessages, responseOptions);
                string aiResponseJson = response.Content.ToString();

                // 5. Omvandla AI:ns JSON-svar till vårt NYA C#-objekt
                DietPlan plan = JsonSerializer.Deserialize<DietPlan>(aiResponseJson);

                return plan;
            }
            catch (Exception ex)
            {
                // Hantera fel om API:n misslyckas
                SpectreUIHelper.Error($"OpenAI API-anrop misslyckades: {ex.Message}");
                return null; // Returnera null vid fel
            }
        }

    }
}
