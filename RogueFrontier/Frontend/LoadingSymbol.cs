using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;

namespace RogueFrontier;

class LoadingSymbol {
    public class Ring {
        public double radius;
        public double rotation;
        public double arc;
        public double speed;
        public void Update() {
            rotation += (speed) / Program.TICKS_PER_SECOND;
            rotation = Math.IEEERemainder(rotation, 2 * Math.PI);
        }
    }
    int radius;
    List<Ring> rings;
    public LoadingSymbol(int diameter) {
        int radius = diameter / 2;
        this.radius = radius;
        Random random = new Random();
        rings = new List<Ring>();
        for (int r = 3; r < radius; r += 2) {
            rings.Add(new Ring() {
                radius = r,
                rotation = random.NextDouble() * 2 * Math.PI,
                arc = random.NextDouble() * Math.PI + Math.PI / 2,
                speed = random.NextDouble() * Math.PI / 2 + Math.PI / 2
            }); ;
        }
    }
    public void Update() {
        rings.ForEach(r => r.Update());
    }
    public ColoredString[] Draw() {
        ColoredString[] effect = new ColoredString[radius * 2 + 1];
        for (int y = 0; y < effect.Length; y++) {
            effect[y] = new ColoredString(radius * 2 + 1);
        }

        foreach (var ring in rings) {
            double start = (2 * Math.PI) + ring.rotation - ring.arc / 2;
            while (start > 2 * Math.PI) start -= 2 * Math.PI;
            start = start.Round(Math.PI / 16);


            double end = start + ring.arc;
            double diff = end - start;

            for (double angle = start; angle < end; angle += diff / (ring.radius * 3)) {
                int x = radius + (int)(Math.Cos(angle) * ring.radius);
                int y = radius + (int)(Math.Sin(angle) * ring.radius);

                if (effect[y][x].GlyphCharacter == ' ') {
                    const double interval = 2 * Math.PI / 8;
                    const string directions = @"|/-\|/-\|/-\|/-\|";
                    int index = (int)((angle + interval / 2) / interval);
                    char c = directions[index];
                    effect[y][x] = new ColoredGlyph(new Color(255, 204, 153), new Color(204, 153, 102), c).ToEffect();
                }
            }
        }
        return effect;
    }
}
