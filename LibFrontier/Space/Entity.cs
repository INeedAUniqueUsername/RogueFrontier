using Common;
using LibGamer;

namespace RogueFrontier;

public interface Effect {
    XY position { get; }
    bool active { get; }
    Tile tile { get; }
    void Update(double delta);
}
public interface Entity : Effect {
    ulong id { get; }
}
