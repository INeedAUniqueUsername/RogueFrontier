using SadConsole;
using System;
namespace Common;

public class SparkleFilter {
    float[,] time;
    int cycle = 720;
    public SparkleFilter(int Width, int Height) {
        time = new float[Width, Height];
        Random r = new();
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                time[x, y] = r.Next(cycle);
            }
        }
    }
    public void Update() {
        for (int x = 0; x < time.GetLength(0); x++) {
            for (int y = 0; y < time.GetLength(1); y++) {
                time[x, y]++;
            }
        }
    }
    public void Filter(int x, int y, ref ColoredGlyph cg) {
        var value = Math.Sin(time[x, y] * 2 * Math.PI / cycle);
        float brightness = cg.Foreground.GetBrightness();
        brightness = (float)Math.Clamp(brightness + value * brightness / 4, 0, 1);
        cg.Foreground = cg.Foreground.SetBrightness(brightness);
    }
}
