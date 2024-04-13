using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandHopper;

public interface Effect {
    XYZ Position { get; set; }
    ColoredGlyph SymbolCenter { get; }
    bool Active { get; }
    void UpdateRealtime(TimeSpan delta);                //	For step-independent effects
    void UpdateStep();                  //	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND
}
public class Reticle : Effect {
    public XYZ Position { get; set; }
    public Color Color;
    int ticks;
    public ColoredGlyph SymbolCenter => ticks % 20 < 10 ? new ColoredGlyph(Color, Color.Transparent, '+') : new ColoredGlyph(Color.Transparent, Color.Transparent, '+');
    private Func<bool> active;
    public Reticle(Func<bool> active, XYZ Position, Color? Color = null) {
        this.active = active;
        this.Position = Position;
        this.Color = Color ?? new Color(255, 255, 255);
    }
    public bool Active => active();
    public void UpdateRealtime(TimeSpan delta) {
        ticks++;
    }
    public void UpdateStep() { }
}
public class Mirage : Effect {
    public Island World { get; set; }
    public XYZ Position { get; set; }
    public ColoredGlyph SymbolCenter { get; private set; }


    public int lifetime;
    public bool Active => lifetime > 0;
    public Mirage(Island World, XYZ Position, int lifetime) {
        this.World = World;
        this.Position = Position;
        this.lifetime = lifetime;
        var ShiftedPosition = Position + new XYZ(World.karma.NextInteger(-2, 3), World.karma.NextInteger(-2, 3));
        if (World.voxels.InBounds(ShiftedPosition)) {
            HashSet<ColoredGlyph> glyphs = new();
            glyphs.UnionWith(World.effects[ShiftedPosition].Where(e => !(e is Mirage)).Select(e => e.SymbolCenter));
            glyphs.UnionWith(World.entities[ShiftedPosition].Select(e => e.SymbolCenter));
            glyphs.Add(World.voxels[ShiftedPosition.PlusZ(-1)].CharAbove);
            SymbolCenter = glyphs.GetRandom(World.karma);
        } else {
            this.lifetime = 0;
            SymbolCenter = new ColoredGlyph(Color.Transparent, Color.Transparent, ' ');
        }
    }
    public void UpdateRealtime(TimeSpan delta) { }
    public void UpdateStep() {
        lifetime--;
    }
}
public class FlameTrail : Effect {
    public XYZ Position { get; set; }

    public ColoredGlyph SymbolCenter => new(new Color(symbol.Foreground.R, symbol.Foreground.G, symbol.Foreground.B, (byte)255), symbol.Background, symbol.Glyph);
    public int lifetime;
    ColoredGlyph symbol;
    public bool Active => lifetime > 0;
    public FlameTrail(XYZ Position, int lifetime, ColoredGlyph symbol) {
        this.Position = Position;
        this.lifetime = lifetime;
        this.symbol = symbol;
    }
    public void UpdateRealtime(TimeSpan delta) { }
    public void UpdateStep() {
        lifetime--;
    }
}
public class Trail : Effect {
    public XYZ Position { get; set; }

    public ColoredGlyph SymbolCenter => new(new Color(symbol.Foreground.R, symbol.Foreground.G, symbol.Foreground.B, (int)(255 * (lifetime > 5 ? 1 : (lifetime + 5) / 10f))), Color.Black, symbol.Glyph);
    public int lifetime;
    ColoredGlyph symbol;
    public bool Active => lifetime > 0;
    public Trail(XYZ Position, int lifetime, char symbol) {
        this.Position = Position;
        this.lifetime = lifetime;
        this.symbol = new ColoredGlyph(Color.White, Color.Black, symbol);
    }
    public Trail(XYZ Position, int lifetime, ColoredGlyph symbol) {
        this.Position = Position;
        this.lifetime = lifetime;
        this.symbol = symbol;
    }
    public void UpdateRealtime(TimeSpan delta) { }
    public void UpdateStep() {
        lifetime--;
    }
}
public class RealtimeTrail : Effect {
    public XYZ Position { get; set; }

    public ColoredGlyph SymbolCenter => new(new Color(symbol.Foreground.R, symbol.Foreground.G, symbol.Foreground.B, (int)(255 * Math.Min(Math.Max(lifetime * 2, 0.5), 1))), Color.Black, symbol.Glyph);
    public double lifetime;
    ColoredGlyph symbol;
    public bool Active => lifetime > 0;
    public RealtimeTrail(XYZ Position, double lifetime, char symbol) {
        this.Position = Position;
        this.lifetime = lifetime;
        this.symbol = new ColoredGlyph(Color.White, Color.Black, symbol);
    }
    public RealtimeTrail(XYZ Position, double lifetime, ColoredGlyph symbol) {
        this.Position = Position;
        this.lifetime = lifetime;
        this.symbol = symbol;
    }
    public void UpdateRealtime(TimeSpan delta) {
        lifetime -= delta.TotalSeconds;
    }
    public void UpdateStep() {
    }
}
public class Decal : Effect {
    public XYZ Position { get; set; }

    public ColoredGlyph SymbolCenter => symbol;
    public ColoredGlyph symbol;
    public bool Active => true;
    public Decal(XYZ Position, ColoredGlyph symbol) {
        this.Position = Position;
        this.symbol = symbol;
    }
    public void UpdateRealtime(TimeSpan delta) { }
    public void UpdateStep() { }
}
