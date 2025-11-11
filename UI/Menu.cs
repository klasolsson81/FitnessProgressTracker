using System;

namespace YourAppName.Client
{
    public enum ClientMenuOption
    {
        VisaTräningsschema = 1,
        VisaKostschema = 2,
        UppdateraMål = 3,
        LoggaTräning = 4,
        SeFramstegOchStatistik = 5,
        SkickaMeddelandeTillPT = 6,
        LoggaUt = 7
    }

    public class MenuTexts
    {
        public string Loading { get; set; }
        public string Result { get; set; }
    }

    public static class ClientMenuTexts
    {
        public static MenuTexts GetTexts(ClientMenuOption choice)
        {
            return choice switch
            {
                ClientMenuOption.VisaTräningsschema => new MenuTexts
                {
                    Loading = "Hämtar träningsschema...",
                    Result = "[green]Ditt träningsschema visas här![/]"
                },
                ClientMenuOption.VisaKostschema => new MenuTexts
                {
                    Loading = "Hämtar kostschema...",
                    Result = "[green]Din kostplan visas här![/]"
                },
                ClientMenuOption.UppdateraMål => new MenuTexts
                {
                    Loading = "Uppdaterar mål...",
                    Result = "[green]Dina mål har uppdaterats![/]"
                },
                ClientMenuOption.LoggaTräning => new MenuTexts
                {
                    Loading = "Loggar dagens träning...",
                    Result = "[green]Träning registrerad![/]"
                },
                ClientMenuOption.SeFramstegOchStatistik => new MenuTexts
                {
                    Loading = "Hämtar statistik...",
                    Result = "[blue]Här är dina framsteg och statistik![/]"
                },
                ClientMenuOption.SkickaMeddelandeTillPT => new MenuTexts
                {
                    Loading = "Skickar meddelande...",
                    Result = "[green]Meddelande skickat till din PT![/]"
                },
                ClientMenuOption.LoggaUt => new MenuTexts
                {
                    Loading = "Loggar ut...",
                    Result = "[yellow]Du har loggats ut![/]"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(choice), $"Oväntat menyval: {choice}")
            };
        }
    }
}
