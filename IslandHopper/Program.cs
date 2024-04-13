namespace IslandHopper;

class IslandHopper {
    static int Width;
    static int Height;
    static IslandHopper() {
        Height = 60;
        Width = Height * 5 / 3;
    }

    static void Main(string[] args) {
        // Setup the engine and create the main window.
        SadConsole.Game.Create(Width, Height, "IslandHopperContent/IBMCGA.font");

        // Hook the start event so we can add consoles to the system.
        SadConsole.Game.Instance.OnStart = Init;

        // Start the game.
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
    }

    private static void Init() {
        SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height) { IsFocused = true };
    }
}
