using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASECII {
    class TileMenu : SadConsole.Console {
        SpriteModel spriteModel;
        TileModel tileModel;
        Action brushChanged;
        bool prevLeft;
        bool prevRight;
        public TileMenu(int width, int height, SpriteModel spriteModel, TileModel tileModel, Action brushChanged) : base(width, height) {
            this.spriteModel = spriteModel;
            this.tileModel = tileModel;
            this.brushChanged = brushChanged;
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            if (state.IsOnScreenObject) {
                int index = (state.SurfaceCellPosition.X) + (state.SurfaceCellPosition.Y * Width);
                if (index > -1 && index < tileModel.tiles.Count) {
                    bool pressLeft = !prevLeft && state.Mouse.LeftButtonDown;
                    if (pressLeft) {
                        if (spriteModel.brush.cell != tileModel.tiles[index]) {
                            spriteModel.brush.cell = tileModel.tiles[index];
                        }
                        brushChanged?.Invoke();
                    }
                }
            }
            prevLeft = state.Mouse.LeftButtonDown;

            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            this.Clear();
            for (int i = 0; i < tileModel.tiles.Count; i++) {
                this.SetCellAppearance(i % Width, i / Width, tileModel.tiles[i]);
            }
            if (tileModel.brushIndex > -1) {
                var i = tileModel.brushIndex.Value;
                int x = i % Width;
                int y = i / Width;

                var t = tileModel.tiles[i];
                this.SetCellAppearance(x, y, new ColoredGlyph(t.Background, t.Foreground, t.Glyph));
            }

            base.Render(timeElapsed);
        }
    }
    public class TileModel {
        public List<TileValue> tiles = new List<TileValue>();
        public HashSet<TileValue> tileset = new HashSet<TileValue>();
        public int? brushIndex;
        public TileIndex brushTile => brushIndex > -1 ? new TileIndex(this, brushIndex.Value) : null;

        public void AddTile(TileValue c) {
            tiles.Add(c);
            tileset.Add(c);
        }
        public void RemoveTile(TileValue c) {
            tiles.Remove(c);
            tileset.Remove(c);
        }
        public void RemoveTile(int index) {
            tileset.Remove(tiles[index]);
            tiles.RemoveAt(index);
        }
        public void UpdateIndexes(SpriteModel spriteModel) {
            var cell = spriteModel.brush.cell;
            brushIndex = tiles.FindIndex(t => t.cg.Matches(cell.cg));
        }
    }
}
