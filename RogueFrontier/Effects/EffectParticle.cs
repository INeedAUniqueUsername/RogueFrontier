using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
namespace RogueFrontier;
public class EffectParticle : Effect {
    public double lifetime;
    public EffectParticle() { }
    public EffectParticle(XY Position, ColoredGlyph Tile, double Lifetime) {
        this.position = Position;
        this.Velocity = new XY();
        this.tile = Tile;
        this.lifetime = Lifetime;
    }
    public EffectParticle(XY Position, XY Velocity, ColoredGlyph Tile, double Lifetime) {
        this.position = Position;
        this.Velocity = Velocity;
        this.tile = Tile;
        this.lifetime = Lifetime;
    }
    public static void DrawArrow(System world, XY worldPos, XY offset, Color color) {
        //Draw an effect for the cursor
        world.AddEffect(new EffectParticle(worldPos, new ColoredGlyph(color, Color.Transparent, 7), 1));
        //Draw a trail leading back to the player
        var trailNorm = offset.normal;
        var trailLength = Math.Min(3, offset.magnitude / 4) + 1;
        for (int i = 1; i < trailLength; i++) {
            world.AddEffect(new EffectParticle(worldPos - trailNorm * i, new ColoredGlyph(color, Color.Transparent, (char)249), 1));
        }
    }
    public XY position { get; set; }
    public XY Velocity { get; set; }
    [JsonIgnore]
    public bool active => lifetime > 0;
    public ColoredGlyph tile { get; private set; }
    public void Update(double delta) {
        position += Velocity / Program.TICKS_PER_SECOND;
        lifetime -= delta * 60;
    }
}
public class FadingTile : Effect {
    private int Lifetime;
    public FadingTile() {

    }
    public FadingTile(XY Position, ColoredGlyph Tile, int Lifetime) {
        this.position = Position;
        this.Velocity = new();
        this._Tile = Tile;
        this.Lifetime = Lifetime;
    }
    public FadingTile(XY Position, XY Velocity, ColoredGlyph Tile, int Lifetime) {
        this.position = Position;
        this.Velocity = Velocity;
        this._Tile = Tile;
        this.Lifetime = Lifetime;
    }
    public XY position { get; private set; }
    public XY Velocity { get; private set; }

    public bool active => Lifetime > 0;

    private ColoredGlyph _Tile;
    public ColoredGlyph tile => new ColoredGlyph(
        _Tile.Foreground.WithValues(alpha: (int)(_Tile.Foreground.A * Math.Min(1, 1f * Lifetime / 10))),
        _Tile.Background.SetAlpha((byte)(_Tile.Background.A * Math.Min(1, 1f * Lifetime / 10))).Premultiply(),
        _Tile.GlyphCharacter);

    public void Update(double delta) {
        position += Velocity / Program.TICKS_PER_SECOND;
        Lifetime--;
    }
}
