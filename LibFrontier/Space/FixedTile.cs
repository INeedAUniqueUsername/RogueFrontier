using Common;
using LibGamer;
namespace RogueFrontier;
class FixedTile : Effect {
    public Tile tile { get; private set; }
    public XY position { get; private set; }
    public bool active { get; private set; }
    public FixedTile(Tile tile, XY Position) {
        this.tile = tile;
        this.position = Position;
        this.active = true;
    }
    public void Update(double delta) {
    }
}
