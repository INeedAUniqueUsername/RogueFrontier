
using Common;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using Console = SadConsole.Console;

namespace RogueFrontier;

class GlyphParticle {
    public Color foregound;
    public char glyph;
    public Point pos;
}
enum Stage {
    Brighten, Darken
}
public class FlashTransition : Console {
    Action next;
    HashSet<GlyphParticle> glyphs = new HashSet<GlyphParticle>();
    Color[,] background;
    double delay = 0;
    Stage stage;
    int tick = 0;
    public FlashTransition(int Width, int Height, Console prev, Action next) : base(Width, Height) {
        this.next = next;
        background = new Color[Width, Height];
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var cg = prev.GetCellAppearance(x, y);
                if (cg.Glyph != ' ') {
                    glyphs.Add(new GlyphParticle() {
                        foregound = cg.Foreground,
                        glyph = cg.GlyphCharacter,
                        pos = new Point(x, y)
                    });
                }
                background[x, y] = cg.Background;
            }
        }
        stage = Stage.Brighten;

        //Draw one frame now so that we don't cut out for one frame
        Render(new TimeSpan());
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            next();
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        if (delay > 0) {
            delay -= delta.TotalSeconds;
        } else {
            tick++;
            switch (stage) {
                case Stage.Brighten: {
                        for (int x = 1; x < Width; x++) {
                            for (int y = 1; y < Height - 1; y++) {
                                var b = background[x, y];
                                int rAdjacent = Math.Min((int)1, (int)Math.Max(background[x - 1, y].R, Math.Max(background[x - 1, y - 1].R, background[x - 1, y + 1].R)));
                                int gAdjacent = Math.Min((int)1, (int)Math.Max(background[x - 1, y].G, Math.Max(background[x - 1, y - 1].G, background[x - 1, y + 1].G)));
                                int bAdjacent = Math.Min((int)1, (int)Math.Max(background[x - 1, y].B, Math.Max(background[x - 1, y - 1].B, background[x - 1, y + 1].B)));
                                background[x, y] = new Color(Math.Min(255, b.R + rAdjacent), Math.Min(255, b.G + gAdjacent), Math.Min(255, b.B + bAdjacent));
                            }
                        }

                        foreach (var glyph in glyphs) {
                            glyph.foregound = glyph.foregound.WithValues(alpha: glyph.foregound.A - 2);
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
                                background[x, y] = new Color(Math.Max(0, b.R - 2), Math.Max(0, b.G - 2), Math.Max(0, b.B - 2));

                                if (background[x, y].GetBrightness() > 0) {
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
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        base.Render(delta);
        foreach (var glyph in glyphs) {
            var (x, y) = glyph.pos;
            this.SetForeground(x, y, glyph.foregound);
            this.SetGlyph(x, y, glyph.glyph);
        }

        //To do: Simplify this so we just draw next with some alpha background
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                //var b = next.GetBackground(x, y);
                var b = background[x, y];
                this.SetBackground(x, y, b);
            }
        }
    }
}
