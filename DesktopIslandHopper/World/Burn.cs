using Common;
using SadConsole;
using System;
using SadRogue.Primitives;

namespace IslandHopper;

class Burn : Effect {
    public ICharacter burning;
    public double duration = 150;
    public XYZ Position { get; set; }
    public bool Active => duration > 0;
    public ColoredGlyph SymbolCenter => new((((int)ticks % 13) % 5) > 2 ? Color.Red : Color.Orange, Color.Black, 'v');
    private int ticks;
    public Burn(ICharacter burning, int duration) {
        this.Position = burning.Position;
        this.burning = burning;
        this.duration = duration;
    }
    public void UpdateStep() {
        var delta = (Position - burning.Position).Magnitude;
        duration -= (int)delta;

        Position = burning.Position;


        duration--;
    }
    public void UpdateRealtime(TimeSpan delta) {
        ticks++;
    }
}
