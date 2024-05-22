using LibGamer;
using System;
using System.Linq;
using System.Collections.Generic;
namespace RogueFrontier;
class GlyphParticle {
    public uint foregound;
    public char glyph;
    public (int x, int y) pos;
}
enum Stage {
    Brighten, Darken
}
public class FlashTransition : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Action next;
    HashSet<GlyphParticle> glyphs = new HashSet<GlyphParticle>();
    uint[,] background;
    double delay = 0;
    Stage stage;
    int tick = 0;
    public Sf sf;
    int Width => sf.Width;
    int Height => sf.Height;
    public FlashTransition(int Width, int Height, Sf prev, Action next) {
        this.sf = new Sf(Width, Height);

		this.next = next;
        background = new uint[Width, Height];
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var cg = prev.GetTile(x, y);
                if (cg.Glyph != ' ') {
                    glyphs.Add(new GlyphParticle() {
                        foregound = cg.Foreground,
                        glyph = (char)cg.Glyph,
                        pos = (x, y)
                    });
                }
                background[x, y] = cg.Background;
            }
        }
        stage = Stage.Brighten;

        //Draw one frame now so that we don't cut out for one frame
        Render(new TimeSpan());
    }
    public void HandleKey(KB keyboard) {
        if (keyboard.IsPress(KC.Enter)) {
            next();
        }
    }
    public void Update(TimeSpan delta) {
        if (delay > 0) {
            delay -= delta.TotalSeconds;
        } else {
            tick++;
            switch (stage) {
                case Stage.Brighten: {
                        for (int x = 1; x < Width; x++) {
                            for (int y = 1; y < Height - 1; y++) {
                                var b = background[x, y];
                                var w = background[x - 1, y];
                                var sw = background[x - 1, y - 1];

                                var nw = background[x - 1, y + 1];

								var R = ABGR.R;
                                var G = ABGR.G;
                                var B = ABGR.B;

								int rAdjacent = Math.Min((int)1, (int)Math.Max(R(w), Math.Max(R(sw), R(nw))));
                                int gAdjacent = Math.Min((int)1, (int)Math.Max(G(w), Math.Max(G(sw), G(nw))));
                                int bAdjacent = Math.Min((int)1, (int)Math.Max(B(w), Math.Max(B(sw), B(nw))));
                                background[x, y] = ABGR.RGB((byte)Math.Min(255, R(b) + rAdjacent), (byte)Math.Min(255, G(b) + gAdjacent), (byte)Math.Min(255, B(b) + bAdjacent));
                            }
                        }

                        foreach (var glyph in glyphs) {
                            glyph.foregound = ABGR.SetA(glyph.foregound, (byte)(ABGR.A(glyph.foregound) - 2));
                        }

                        if (tick > 192) {
                            stage = Stage.Darken;
                            delay = 2;
                            tick = 0;
                        }
                        break;
                    }
                case Stage.Darken: {
                        bool done = true;
                        for (int x = 0; x < Width; x++) {
                            for (int y = 0; y < Height; y++) {
                                var b = background[x, y];
                                background[x, y] = ABGR.RGB((byte)Math.Max(0, ABGR.R(b) - 2), (byte)Math.Max(0, ABGR.G(b) - 2), (byte)Math.Max(0, ABGR.B(b) - 2));

                                if (ABGR.GetLightness(background[x, y]) > 0) {
                                    done = false;
                                }
                            }
                        }
                        if (done) {
                            next();
                        }
                        break;
                    }
            }
        }
    }
    public void Render(TimeSpan delta) {
        foreach (var glyph in glyphs) {
            var (x, y) = glyph.pos;
            sf.SetFront(x, y, glyph.foregound);
            sf.SetGlyph(x, y, glyph.glyph);
        }
        //To do: Simplify this so we just draw next with some alpha background
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                //var b = next.GetBackground(x, y);
                var b = background[x, y];
                sf.SetBack(x, y, b);
            }
        }
        Draw(sf);
    }
}
