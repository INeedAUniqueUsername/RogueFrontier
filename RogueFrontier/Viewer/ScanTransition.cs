using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using Console = SadConsole.Console;

namespace RogueFrontier;

public class ScanTransition : Console {
    ScreenSurface next;
    double y;
    public ScanTransition(ScreenSurface next) : base(next.Surface.Width, next.Surface.Height) {
        y = 0;
        this.next = next;
        next.Render(new TimeSpan());
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.KeysPressed.Count > 0) {
            Transition();
            //next.ProcessKeyboard(keyboard);
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        if (y < next.Surface.Height) {
            y += delta.TotalSeconds * Height * 3;
        } else {
            Transition();
        }
        base.Update(delta);
    }
    public void Transition() {

        var p = Parent;
        p.Children.Remove(this);
        p.Children.Add(next);
        next.IsFocused = true;
    }
    public override void Render(TimeSpan delta) {
        this.Clear();

        var last = (int)Math.Min(this.y - 1, Height - 1);

        int y;
        for (y = 0; y < last; y++) {
            for (int x = 0; x < Width; x++) {
                this.SetCellAppearance(x, y, next.Surface.GetCellAppearance(x, y));
            }
        }
        y = last;
        for (int x = 0; x < Width; x++) {
            this.SetCellAppearance(x, y, new ColoredGlyph(Color.Transparent, Color.White.SetAlpha(128)));
        }
        ColoredString empty = new ColoredString(new string(' ', Width), Color.Transparent, Color.Transparent);
        for (y = last + 1; y < Height; y++) {
            this.Print(0, y, empty);
        }
        base.Render(delta);

        //            next.Render(delta);
    }
}
