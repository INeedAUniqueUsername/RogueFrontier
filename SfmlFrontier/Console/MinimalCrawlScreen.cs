using SadConsole;
using SadConsole.Input;
using System;
using System.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using LibGamer;

namespace RogueFrontier;

class MinimalCrawlScreen : IScene {
    public Sf sf;
    private Action next;
    private readonly string text;
    bool speedUp;
    int index;
    int tick;
    public Action<IScene> Go { get; set; } = _ => { };
    public Action<Sf> Draw { get; set; } = _ => { };
	public MinimalCrawlScreen(Sf prev, string text, Action next) {
        this.sf = new Sf(text.Split('\n').Max(l => l.Length), text.Split('\n').Length);
        this.next = next;
        this.text = text;
    }
    public void Update(TimeSpan time) {
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
    public void Render(TimeSpan drawTime) {
        sf.Clear();
        int x = 0;
        int y = 0;
        for (int i = 0; i < index; i++) {
            if (text[i] == '\n') {
                x = 0;
                y++;
            } else {
                sf.SetTile(x, y, new Tile(ABGR.White, ABGR.Black, text[i]));
                x++;
            }
        }
        Draw(sf);
    }
    public void HandleKey(Keyboard info) {
        if (info.IsKeyPressed(SadConsole.Input.Keys.Enter)) {
            if (speedUp) {
                index = text.Length;
            } else {
                speedUp = true;
            }
        }
    }
}
