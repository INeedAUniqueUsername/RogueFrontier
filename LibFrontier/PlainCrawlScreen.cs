using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

class PlainCrawlScreen : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Sf sf;
    private Action next;
    private readonly string text;
    bool speedUp;
    int index;
    int tick;

    (int x, int y) pos;
	public PlainCrawlScreen((int x,int y) pos, string text, Action next) {
        this.pos = pos;
        this.sf = new Sf(text.Split('\n').Max(l => l.Length), text.Split('\n').Length, Fonts.FONT_8x8);
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
        var (x, y) = pos;
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
    public void HandleKey(KB info) {
        if (info.IsPress(KC.Enter)) {
            if (speedUp) {
                index = text.Length;
            } else {
                speedUp = true;
            }
        }
    }
}
