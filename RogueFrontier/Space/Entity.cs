using Common;
using SadConsole;

namespace RogueFrontier;

public interface Effect {
    XY position { get; }
    bool active { get; }
    ColoredGlyph tile { get; }
    void Update(double delta);
}
public interface Entity : Effect {
    ulong id { get; }
}
