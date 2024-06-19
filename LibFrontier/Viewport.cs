using Common;
using LibGamer;
using System;
using System.Collections.Generic;
namespace RogueFrontier;
public class Viewport {
    public Action<Sf> Draw { get; set; }
    public int Width => sf.Width;
    public int Height => sf.Height;
    public Camera camera;
    public System world;
    public Dictionary<(int, int), Tile> tiles = [];
    public Sf sf;

	public Indexer<(int x, int y), Tile> _Tile;
	public Viewport(int Width, int Height, Monitor m) {

		_Tile = new(p => GetTile(p.x, p.y));
		this.sf = new Sf(Width, Height, Fonts.FONT_8x8);
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
                if (tiles.GetValueOrDefault(location.roundDown, null) is { }tile) {
                    var xScreen = x + HalfViewWidth;
                    var yScreen = HalfViewHeight - y - 1;
                    sf.Tile[xScreen, yScreen] = tile;
                }
            }
        }
        Draw?.Invoke(sf);
    }
    public Tile GetTile(int x, int y) {
        XY location = camera.position + new XY(x - Width / 2, y - Height / 2).Rotate(camera.rotation);
        return tiles.GetValueOrDefault(location.roundDown, Tile.empty);
    }
}
public record Indexer<T, U> (Func<T, U> f) {
    public U this[T t] => f(t);
}