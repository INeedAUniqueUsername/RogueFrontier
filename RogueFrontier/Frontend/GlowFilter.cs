using SadRogue.Primitives;
using SadConsole;
using System.Collections.Generic;
using Common;
using System;
using Console = SadConsole.Console;

namespace RogueFrontier;

class GlowFilter {
    class GlowPoint {
        public XY pos;
        public Color tint;
    }
    Console parent;
    HashSet<GlowPoint> points;
    XY direction;
    public GlowFilter(Console parent) {
        this.parent = parent;
        this.points = new HashSet<GlowPoint>();
        Random r = new Random();
        this.direction = XY.Polar(r.NextDouble() * 2 * Math.PI);

        Color[] colors = new Color[] { Color.Red, Color.Green, Color.Blue };
        for (int i = 0; i < 8; i++) {
            points.Add(new GlowPoint() {
                pos = new XY(r.Next(parent.Width), r.Next(parent.Height)),
                tint = colors[r.Next(colors.Length)]
            });
        }
    }
    public void Update() {
        int Width = parent.Width, Height = parent.Height;
        XY half = new XY(Width / 2, Height / 2);
        XY two = new XY(Width * 2, Height * 2);
        foreach (var p in points) {
            p.pos += direction;
            p.pos = (p.pos + half) % two - half;
        }
    }
    public void Render() {
        for (int x = 0; x < parent.Width; x++) {
            for (int y = 0; y < parent.Height; y++) {
                var foreground = parent.GetForeground(x, y);
                var background = parent.GetBackground(x, y);

                var factorF = foreground.GetLuma() / 128;
                var factorB = background.GetLuma() / 128;
                foreach (var p in points) {
                    var delta = p.tint.WithValues(alpha: Math.Max(2, 36 - (int)(p.pos - new XY(x, y)).manhattan / 4));

                    foreground = foreground.Blend(delta.Multiply(a: factorF));
                    background = background.Blend(delta.Multiply(a: factorB));
                }
                parent.SetForeground(x, y, foreground);
                parent.SetBackground(x, y, background);
            }
        }
    }
}
