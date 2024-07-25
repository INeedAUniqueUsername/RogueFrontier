using System.Collections.Generic;
using System.Linq;


using PI = (int X, int Y);
using TileTuple = (uint Foreground, uint Background, int Glyph);
using LibGamer;
using static LibGamer.Tile;
namespace Common;
public class TileImage {
    public Dictionary<(int x, int y), Tile> Sprite = new();
    public XYI Size;
    bool transparent;

    public int Width, Height;
    public TileImage(Dictionary<PI, TileTuple> Sprite) {
        int left = Sprite.Keys.Min(p => p.X);
        int top = Sprite.Keys.Min(p => p.Y);
        int right = Sprite.Keys.Max(p => p.X);
        int bottom = Sprite.Keys.Max(p => p.Y);

        (this.Width, this.Height) = (right - left, top - bottom);
        Size = new(right - left, bottom - top);
        PI origin = (left, top);
        this.Sprite = new();
        foreach ((var p, var t) in Sprite) {
            this.Sprite[(p.X - origin.X, p.Y - origin.Y)] = t;
        }
    }
    public void Render(Sf onto, PI pos) {
        foreach ((var p, var t) in Sprite) {
            onto.Tile[pos.X + p.x, pos.Y + p.y] = t;
        }
    }
    public void Render(Sf onto, PI pos, Func<Tile,Tile> f) {
		foreach((var p, var t) in Sprite) {
			onto.Tile[pos.X + p.x, pos.Y + p.y] = f(t);
		}
	}
    public static TileImage FromFile(string file) => new TileImage(ImageLoader.LoadTile(file));
}