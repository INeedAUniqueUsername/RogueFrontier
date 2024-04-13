using SadConsole;
using System;
using System.Collections.Generic;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using XY = Common.XY;

namespace Graphics;

class WorldDraw : Console {
    RogueFrontier.System World;
    public XY camera;
    public Dictionary<(int, int), ColoredGlyph> tiles;

    public WorldDraw(int width, int height, RogueFrontier.System World) : base(width, height) {
        this.World = World;
        this.camera = new XY(0, 0);
        this.tiles = new Dictionary<(int, int), ColoredGlyph>();
    }
    public override void Update(TimeSpan timeSpan) {


        World.UpdateAdded();
        World.UpdateActive(timeSpan.TotalSeconds);
        World.UpdateRemoved();

        tiles.Clear();
        World.PlaceTiles(tiles);
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var offset = new XY(x, Height - y) - new XY(Width / 2, Height / 2);
                var location = camera + offset;
                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    if (tile.Background == Color.Transparent) {
                        tile.Background = World.backdrop.GetBackground(location, camera);
                    }
                    this.SetCellAppearance(x, y, tile);
                } else {
                    this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                }
            }
        }
        base.Render(drawTime);
    }
}
