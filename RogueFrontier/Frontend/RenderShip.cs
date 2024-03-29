using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SadConsole;
using SadConsole.Renderers;
using SadRogue.Primitives;
using Console = SadConsole.Console;
namespace RogueFrontier.Screens;

class RenderShip : Console {
    static int Width = 50, Height = 50;

    static void Main2(string[] args) {
        // Setup the engine and create the main window.
        SadConsole.Game.Create(Width, Height, "RogueFrontierContent/sprites/IBMCGA.font", (o, gh) => { });
        SadConsole.Game.Instance.DefaultFontSize = IFont.Sizes.Two;
        SadConsole.Game.Instance.Started += (o, gh) => Init();
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
    }
    private static void Init() {

        TypeCollection tc = new TypeCollection("RogueFrontierContent/scripts/Main.xml");

        Directory.CreateDirectory("RogueFrontierRenders");
        foreach ((var codename, var sc) in tc.GetDict<ShipClass>()) {
            if (sc.playerSettings?.map == null) {
                continue;
            }

            Dictionary<string, Color> c = new Dictionary<string, Color> {
                    {"ship_amethyst_i", Color.Violet },
                    {"ship_beowulf", Color.LightBlue },
                    {"ship_clark", Color.Wheat},
                };

            var s = new RenderShip(Width, Height, sc.playerSettings.map, c[codename]);
            s.Render(new TimeSpan());
            var t = ((ScreenSurfaceRenderer)s.Renderer)._backingTexture;
            //t.Save($"RogueFrontierRenders/{codename}.png");
        }
        Environment.Exit(0);
    }
    string[] map;
    Color c;
    public RenderShip(int width, int height, string[] map, Color c) : base(width, height) {
        this.map = map;
        this.c = c;
    }
    public override void Render(TimeSpan t) {
        this.Fill(Color.Black, Color.Black, 0);
        int y = 0;
        var mapWidth = map.Select(line => line.Length).Max();
        var mapX = Width / 2 - mapWidth / 2;
        //var mapX = 0;
        y++;
        //We print each line twice since the art gets flattened by the square font
        //Ideally the art looks like the original with an added 3D effect
        foreach (var line in map) {
            for (int i = 0; i < line.Length; i++) {
                this.SetCellAppearance(mapX + i, y, new ColoredGlyph(c, Color.Black, line[i]));
            }
            y++;
            for (int i = 0; i < line.Length; i++) {
                this.SetCellAppearance(mapX + i, y, new ColoredGlyph(c, Color.Black, line[i]));
            }
            y++;
        }
        base.Render(t);
    }
}
