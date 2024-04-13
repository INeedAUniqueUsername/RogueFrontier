using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;

namespace CloudJumper;

class CloudParticle {
    public Point pos;
    public ColoredGlyph symbol;

    public CloudParticle(Point pos, ColoredGlyph symbol) {
        this.pos = pos;
        this.symbol = symbol;
    }
    public void Update(Random random) {
        pos += new Point(1, 0);

        var f = symbol.Foreground;
        var (r, g, b, a) = (f.R, f.G, f.B, f.A);
        Func<int, int> transform = i => Math.Max(0, i - random.Next(0, 2));
        symbol.Foreground = new Color(transform(r), transform(g), transform(b), transform(a));
    }

    public static void CreateClouds(int effectMinY, int effectMaxY, List<CloudParticle> clouds, Random random) {
        var cloudPoint = new Point(0, random.Next(effectMinY, effectMaxY));
        var cloudParticle = new ColoredGlyph(new Color(204 + random.Next(0, 51), 0, 204 + random.Next(0, 51)), Color.Transparent, GetRandomChar());
        clouds.Add(new CloudParticle(cloudPoint, cloudParticle));
        double i = 1;
        while (random.NextDouble() < 0.9) {
            cloudPoint += new Point(-1, (random.Next(0, 5) - 2) / 2);
            cloudParticle = new ColoredGlyph(new Color(204 + random.Next(0, 51), 0, 225 + random.Next(0, 25)), Color.Transparent, GetRandomChar());
            clouds.Add(new CloudParticle(cloudPoint, cloudParticle));
            for (int y = 1; y < random.Next(2, 5); y++) {
                var verticalPoint = cloudPoint - new Point(0, y);
                cloudParticle = new ColoredGlyph(new Color(225 + random.Next(0, 25), 153 + random.Next(102), 225 + random.Next(0, 25)), Color.Transparent, GetRandomChar());
                clouds.Add(new CloudParticle(verticalPoint, cloudParticle));
            }
            i++;
        }

        char GetRandomChar() {
            const string vwls = "?&%~=+;";
            return vwls[random.Next(vwls.Length)];
        }
    }
}
