using Common;
using LibGamer;
namespace RogueFrontier;
public class Viewport {
    public int Width => sf.Width;
    public int Height => sf.Height;
    public Camera camera;
    public System world;
    public Dictionary<(int, int), Tile> tiles=new();
    public Sf sf;
    public Viewport(int Width, int Height, Monitor m) {
        this.sf = new Sf(Width, Height);
        camera = m.camera;
        world = m.world;
    }
    public void Update(TimeSpan delta) {
        tiles.Clear();
        world.PlaceTiles(tiles);
    }
    public void UpdateVisible(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        tiles.Clear();
        world.PlaceTilesVisible(tiles, getVisibleDistanceLeft);
    }
    public void UpdateBlind(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        world.PlaceTilesVisible(tiles, getVisibleDistanceLeft);
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        int HalfViewWidth = Width / 2;
        int HalfViewHeight = Height / 2;
        for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
            for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
                XY location = camera.position + new XY(x, y).Rotate(camera.rotation);
                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    var xScreen = x + HalfViewWidth;
                    var yScreen = HalfViewHeight - y;
                    sf.Tile[xScreen, yScreen] = tile;
                }
            }
        }
    }
    public Tile GetTile(int x, int y) {
        XY location = camera.position + new XY(x - Width / 2, y - Height / 2).Rotate(camera.rotation);
        return tiles.TryGetValue(location.roundDown, out var tile) ? tile : Tile.empty;
    }
}
