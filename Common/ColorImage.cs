using ASECII;
using System.Collections.Generic;
using SadRogue.Primitives;
using System.Linq;
using SadConsole;
using Console = SadConsole.Console;
namespace Common;
public class ColorImage {
    public Dictionary<(int x, int y), ColoredGlyph> Sprite;
    public Point Size;
    public ColorImage(Dictionary<(int x, int y), TileValue> Sprite) {
        int left = Sprite.Keys.Min(p => p.x);
        int top = Sprite.Keys.Min(p => p.y);
        int right = Sprite.Keys.Max(p => p.x);
        int bottom = Sprite.Keys.Max(p => p.y);
        Size = new(right - left, bottom - top);
        var origin = new Point(left, top);
        this.Sprite = new();
        foreach ((var p, var t) in Sprite) {
            this.Sprite[p - origin] = t;
        }
    }
    public void Render(Console onto, Point pos) {
        foreach ((var p, var t) in Sprite) {
            (var x, var y) = pos + p;
            onto.SetCellAppearance(x, y, t);
        }
    }
    public static ColorImage FromFile(string file) => new ColorImage(ASECIILoader.LoadCG(file));
}