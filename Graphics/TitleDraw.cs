
using SadConsole;
using System;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using System.IO;
using System.Linq;

namespace Graphics;

class TitleDraw : Console {
    public static string[] title = File.ReadAllText("RogueFrontierContent/sprites/Title.txt").Replace("\r\n", "\n").Split('\n');
    public static int Width = title.Max(line => line.Length);
    public static int Height = title.Length;
    public TitleDraw() : base(Width, Height) {
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        var titleY = 0;
        foreach (var line in title) {
            this.Print(0, titleY, line, Color.White, Color.Black);
            titleY++;
        }
        base.Render(delta);
    }
}
