using Common;
using SadConsole;
using System;

namespace IslandHopper;

class PlaneSegment : Entity, Standable {
    public Entity parent { get; }

    public Island World => parent.World;

    public XYZ Velocity { get => parent.Velocity; set => parent.Velocity = value; }

    public ColoredString Name => parent.Name;

    public XYZ Position { get => parent.Position + offset; set => parent.Position = value - offset; }

    public ColoredGlyph SymbolCenter => parent.SymbolCenter;

    public bool Active => parent.Active;

    private XYZ offset;
    public PlaneSegment(Entity parent, XYZ offset) {
        this.parent = parent;
        this.offset = offset;
    }

    public void OnRemoved() {
    }

    public void UpdateRealtime(TimeSpan delta) {
    }

    public void UpdateStep() {
    }
}
