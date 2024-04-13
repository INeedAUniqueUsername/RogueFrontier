using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Console = SadConsole.Console;
namespace RogueFrontier;

public class Viewport {
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    public Camera camera;
    public System world;
    public Dictionary<(int, int), ColoredGlyph> tiles=new();
    public ScreenSurface Surface;
    public Viewport(Monitor m) {
        Surface = m.NewSurface;
        camera = m.camera;
        world = m.world;
    }
    public void Update(TimeSpan delta) {
        tiles.Clear();
        world.PlaceTiles(tiles);
        Surface.Update(delta);
    }
    public void UpdateVisible(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        tiles.Clear();
        world.PlaceTilesVisible(tiles, getVisibleDistanceLeft);
        Surface.Update(delta);
    }
    public void UpdateBlind(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        world.PlaceTilesVisible(tiles, getVisibleDistanceLeft);
        Surface.Update(delta);
    }
    public void Render(TimeSpan delta) {
        Surface.Clear();
        int HalfViewWidth = Width / 2;
        int HalfViewHeight = Height / 2;
        for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
            for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
                XY location = camera.position + new XY(x, y).Rotate(camera.rotation);
                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    var xScreen = x + HalfViewWidth;
                    var yScreen = HalfViewHeight - y;
                    Surface.SetCellAppearance(xScreen, yScreen, tile);
                }
            }
        }
        Surface.Render(delta);
    }
    public ColoredGlyph GetTile(int x, int y) {
        XY location = camera.position + new XY(x - Width / 2, y - Height / 2).Rotate(camera.rotation);
        return tiles.TryGetValue(location.roundDown, out var tile) ? tile : new ColoredGlyph(Color.Transparent, Color.Transparent);
    }
}
