using System.Collections.Generic;
using System.Linq;


using XYI = (int X, int Y);
using TileTuple = (uint Foreground, uint Background, int Glyph);
using LibGamer;
using static LibGamer.Tile;
namespace Common;
public class TileImage {
    public Dictionary<(int x, int y), Tile> Sprite;
    public XYI Size;
    public TileImage(Dictionary<XYI, TileTuple> Sprite) {
        int left = Sprite.Keys.Min(p => p.X);
        int top = Sprite.Keys.Min(p => p.Y);
        int right = Sprite.Keys.Max(p => p.X);
        int bottom = Sprite.Keys.Max(p => p.Y);
        Size = new(right - left, bottom - top);
        XYI origin = (left, top);
        this.Sprite = new();
        foreach ((var p, var t) in Sprite) {
            this.Sprite[(p.X - origin.X, p.Y - origin.Y)] = t;
        }
    }
    public void Render(Sf onto, XYI pos) {
        foreach ((var p, var t) in Sprite) {
            (var x, var y) = (pos.X + p.x, pos.Y + p.y);
            onto.Tile[x, y] = t;
        }
    }
    public static TileImage FromFile(string file) => new TileImage(ImageLoader.LoadTile(file));
}