using SadConsole;
using SadConsole.Input;
using System;
using System.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace RogueFrontier;

class MinimalCrawlScreen : Console {
    private Action next;
    private readonly string text;
    bool speedUp;
    int index;
    int tick;

    public MinimalCrawlScreen(string text, Action next) : base(text.Split('\n').Max(l => l.Length), text.Split('\n').Length) {
        this.next = next;
        this.text = text;
    }
    public override void Update(TimeSpan time) {
        if (index < text.Length) {
            tick++;
            if (speedUp) {
                index++;
            } else {
                if (tick % 4 == 0) {
                    index++;
                }
            }
        } else {
            next();
        }
    }
    public override void Render(TimeSpan drawTime) {
        base.Render(drawTime);
        this.Clear();

        int x = 0;
        int y = 0;
        for (int i = 0; i < index; i++) {
            if (text[i] == '\n') {
                x = 0;
                y++;
            } else {
                this.SetCellAppearance(x, y, new ColoredGlyph(Color.White, Color.Black, text[i]));
                x++;
            }
        }
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(SadConsole.Input.Keys.Enter)) {
            if (speedUp) {
                index = text.Length;
            } else {
                speedUp = true;
            }
        }

        return base.ProcessKeyboard(info);
    }
}
