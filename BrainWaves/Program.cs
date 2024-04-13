using SadConsole;
namespace BrainWaves;
class Program {
    public static int Width = 150, Height = 90;
    static void Main(string[] args) {
        // Setup the engine and create the main window.
        SadConsole.Game.Create(Width, Height, "BrainWavesContent/IBMCGA.font");
        SadConsole.Game.Instance.DefaultFontSize = IFont.Sizes.One;
        // Hook the start event so we can add consoles to the system.
        SadConsole.Game.Instance.OnStart = Init;

        // Start the game.
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
    }
    private static void Init() {
        GameHost.Instance.Screen = new GameScreen(Width, Height) { IsFocused = true };
    }
}