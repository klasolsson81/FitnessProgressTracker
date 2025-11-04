# 🏋️‍♂️ Fitness Progress Tracker

Ett OOP-baserat terminalprogram i C# som hjälper PT:s och klienter att hantera träning och kost.

## 👥 Team
- Klas Olsson
- Mohammed Yusur
- Sacad Elmi
- Sajad Azizi
- Yonis Bashir
- Ali Dehi

## 🎯 Projektmål
- Visa förståelse för OOP, generiska klasser, JSON och Spectre.Console
- Arbeta effektivt i team via GitHub (brancher, pull requests, kodgranskning)

## Kom igång

För att komma igång med projektet på din lokala maskin, följ dessa steg:

1.  **Acceptera inbjudan som Collaborator på repot.**
    Detta är nödvändigt för att få åtkomst till repositoryt.

2.  **Klona repositoryt.**
    Öppna din terminal eller kommandotolk och kör följande kommando:
    ```bash
    git clone https://github.com/your-org/your-repo.git
    ```
    (Ersätt `https://github.com/your-org/your-repo.git` med den faktiska URL:en för detta repository.)

3.  **Öppna projektet i din kod-editor.**
    Navigera till den klonade mappens rot och öppna projektet i din kod-editor (t.ex. VS Code).

4.  **Verifiera att allt bygger.**
    Öppna terminalen *inom* projektets rotkatalog och kör:
    ```bash
    dotnet build
    ```
    Detta kommer att kompilera projektet och säkerställa att alla beroenden är lösta och att det inte finns några kompileringsfel.

## 🧍‍♀️ Användartyper
### PT
- Skapa träningsschema med AI-hjälp
- Skapa kostschema
- Sätta mål för klienter

### Klient
- Registrera sig och skapa profil
- Se träningsschema
- Logga framsteg
- Avboka/ändra pass

## 🧩 Funktioner (Features)
- Inloggning (PT/Klient)
- CRUD för scheman och loggar
- JSON-lagring
- Spectre.Console UI

## ⚙️ Struktur
- Models: klasser och datamodeller
- Services: logik och datalagring
- UI: Spectre.Console-meny och visning

## 📊 Datahantering
All data sparas i JSON-filer vid avslut och laddas vid start.

## 🌈 Exempel på flöde
Klient → Loggar in → Ser träningsplan → Markerar pass som klart → Framsteg sparas till JSON.

---

## 🧠 Kommande funktioner
- AI-stöd för att skapa träningsplaner
- Progress bars för viktförändring
- Loggning av prestationer