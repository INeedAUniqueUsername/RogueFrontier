using Common;
using SadRogue.Primitives;
using LibGamer;

namespace RogueFrontier;

public class ImageDisplay {
    public TileImage image;
    public Point adjust;
    Sf sf;
    public ImageDisplay(int width, int height, TileImage image, Point adjust) {
        this.image = image;
        this.adjust = adjust;
        sf = new Sf(width, height);
        Draw();
    }
    public void Draw() {
        foreach (((int x, int y) p, Tile t) in image.Sprite) {
            var pos = (x:p.x + adjust.X, y:p.y + adjust.Y);
            sf.SetTile(pos.x, pos.y, t);
        }
    }
}
