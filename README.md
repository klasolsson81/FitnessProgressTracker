# 🏋️‍♂️ Fitness Progress Tracker

Ett avancerat terminalbaserat system byggt i C# .NET för att hantera kommunikation, planering och uppföljning mellan Personliga Tränare (PT) och deras klienter.

Projektet demonstrerar objektorienterad programmering (OOP), användning av externa API:er (AI), generiska datastrukturer och ett modernt konsol-gränssnitt.

## 👥 Team
* **Klas Olsson**
* **Mohammed Yusur**
* **Sacad Elmi**
* **Sajad Azizi**
* **Yonis Bashir**

---

## 🚀 Huvudfunktioner (Features)

### 🤖 AI-Drivna Scheman
* Systemet använder AI för att automatiskt generera skräddarsydda **tränings- och kostscheman**.
* Baserat på klientens mål (t.ex. "Gå ner i vikt", "Bygga muskler") och förutsättningar skapar systemet detaljerade veckoplaner.

### 🖥️ Interaktivt UI (Spectre.Console)
* **Animerade menyer:** Snygg och tydlig navigering.
* **Tabeller & Dashboards:** Visuell presentation av träningsprogram och kostplaner.
* **Interaktiva Prompts:** Enkel inmatning och validering av data.

### 🔐 Rollbaserad Inloggning
* **PT-vy:** Dashboard för att hantera klienter, skapa/redigera mål, granska AI-förslag och följa upp statistik. Inkluderar administrationsverktyg för att ta bort klienter eller rensa systemdata.
* **Klient-vy:** Dashboard för att se sina scheman, dagliga uppgifter och logga sina framsteg (vikt, noteringar).

### 💾 Datahantering (JSON)
* All data (Användare, Scheman, Loggar) sparas persistent i JSON-filer.
* Implementerat med **Generiska Repositories** (`IDataStore<T>`) för återanvändbar kod.

---

## 🛠️ Teknisk Arkitektur

Projektet är uppdelat enligt **Service-Repository Pattern** för att hålla koden ren och modulär (Separation of Concerns).

### 📂 Struktur
* **Models:** Innehåller datamodeller (POCOs) som `Client`, `PT`, `WorkoutPlan`, `DietPlan`. Alla modeller är "Null-safe".
* **Services:** Innehåller affärslogiken.
    * `AiService`: Hanterar API-anrop för generering av scheman.
    * `ScheduleService`: Kopplar ihop klienter med scheman och hanterar "Review"-flödet.
    * `ClientService` & `LoginService`: Hanterar användare och autentisering.
    * `ProgressService`: Hanterar loggning av vikt och noteringar.
* **Data:** Generisk `JsonDataStore<T>` som hanterar läsning/skrivning till JSON-filer.
* **UI:** Hanterar all visuell representation via `Spectre.Console`.

---

## 🧠 Arbetsprocess & Metodik
* **Agilt arbetssätt:** Vi har arbetat med Pull Requests, kodgranskning och feature-brancher på GitHub.
* **Felhantering:** Robust felhantering (Try/Catch) vid kritiska moment (t.ex. filhantering och API-anrop) för att förhindra krascher.
* **DRY (Don't Repeat Yourself):** Återanvändning av kod genom helper-klasser och generiska metoder.

## 📦 Installation & Körning

1.  Klona repot:
    ```bash
    git clone [https://github.com/DittAnvändarnamn/FitnessProgressTracker.git](https://github.com/DittAnvändarnamn/FitnessProgressTracker.git)
    ```
2.  Navigera till mappen och kör:
    ```bash
    dotnet run
    ```
*(Se till att återställa NuGet-paket vid första körningen om det behövs)*

---
*Utvecklat som en del av kursen i C# Systemutveckling (.NET).*
