
using SadConsole;
using SadConsole.Renderers;
using System;
using System.IO;
using Console = SadConsole.Console;
using SadRogue.Primitives;
namespace Graphics;

class Program {
    public static int Width = 150, Height = 90;

    static void Main(string[] args) {
        // Setup the engine and create the main window.
        SadConsole.Game.Create(Width, Height, "RogueFrontierContent/sprites/IBMCGA.font");
        SadConsole.Game.Instance.DefaultFontSize = IFont.Sizes.Four;
        SadConsole.Game.Instance.OnStart = Init;
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
    }
    private static void Init() {
        RogueFrontier.System w = new RogueFrontier.System();
        w.types.LoadFile("RogueFrontierContent/scripts/Main.xml");

        Directory.CreateDirectory("GraphicsContent");

        var str = "ARCHCANNON";
        var s = new Console(str.Length, 1);
        //var s = new WorldDraw(2000, 2000, w);
        s.Print(0, 0, str, Color.White, Color.Black);

        int x = 0;
        foreach (var c in str) {
            s.Print(x, 0, c.ToString(), Color.FromHSL((1f * x) / str.Length, 1, 0.7f), Color.Black);
            x++;
        }

        s.Render(new TimeSpan());
        var t = ((ScreenSurfaceRenderer)s.Renderer)._backingTexture;
        //t.Save("GraphicsContent/Archcannon.png");

        Environment.Exit(0);

    }
}
