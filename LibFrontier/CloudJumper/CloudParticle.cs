using LibGamer;
using System;
using System.Collections.Generic;
using PI = (int X, int Y);
using static LibGamer.ABGR;
namespace CloudJumper;

public class CloudParticle {
    public PI pos;
    public Tile symbol;

    public CloudParticle(PI pos, Tile symbol) {
        this.pos = pos;
        this.symbol = symbol;
    }
    public void Update(Random random) {
        pos = (pos.X + 1, pos.Y + 0);

        var f = symbol.Foreground;
        var (r, g, b, a) = (R(f), G(f), B(f), A(f));
        Func<byte, byte> transform = i => (byte)Max(0, i - random.Next(0, 2));
        symbol = symbol with { Foreground = RGBA(transform(r), transform(g), transform(b), transform(a)) };
    }

    public static void CreateClouds(int effectMinY, int effectMaxY, List<CloudParticle> clouds, Random random) {
        PI cloudPoint = (0, random.Next(effectMinY, effectMaxY));
        var cloudParticle = new Tile(RGB((byte)(204 + random.Next(0, 51)), 0, (byte)(204 + random.Next(0, 51))), Transparent, GetRandomChar());
        clouds.Add(new CloudParticle(cloudPoint, cloudParticle));
        double i = 1;
        while (random.NextDouble() < 0.9) {
            cloudPoint = (cloudPoint.X + -1, cloudPoint.Y + (random.Next(0, 5) - 2) / 2);
            cloudParticle = new Tile(RGB((byte)(204 + random.Next(0, 51)), 0, (byte)(225 + random.Next(0, 25))), Transparent, GetRandomChar());
            clouds.Add(new CloudParticle(cloudPoint, cloudParticle));
            for (int y = 1; y < random.Next(2, 5); y++) {
                var verticalPoint = (cloudPoint.X - 0, cloudPoint.Y - y);
                cloudParticle = new Tile(RGB((byte)(225 + random.Next(0, 25)), (byte)(153 + random.Next(102)), (byte)(225 + random.Next(0, 25))), Transparent, GetRandomChar());
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
