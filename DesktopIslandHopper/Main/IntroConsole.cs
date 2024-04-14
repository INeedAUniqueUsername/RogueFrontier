using SadRogue.Primitives;
using SadConsole;
using System;
using Console = SadConsole.Console;

namespace IslandHopper;

public class IntroConsole : Console {

    public IntroConsole(int Width, int Height) : base(Width, Height) {
        DefaultBackground = Color.Black;
        DefaultForeground = Color.White;
    }

    public override void Render(TimeSpan delta) {

        this.Clear();

        string[] lines = {
                "In the year 2040, climate change leaves half of the world uninhabitable.",
                "War breaks out between the surviving nations over the control of land.",
                "Some nations resort to nuclear force with nothing to lose. The mainlands are ruined.",
                "Having lost most of their armed forces, the nations deploy untrained civilians",
                "to fight for the remaining remote islands on the oceans",
                "",
                "You are one of those civilians."
            };

        int x = 16;
        int y = 16;
        foreach (var s in lines) {
            this.Print(x, y++, s);
        }

        base.Render(delta);
    }
}
