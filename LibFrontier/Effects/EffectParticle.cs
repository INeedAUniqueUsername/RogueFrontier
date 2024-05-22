using Common;
using LibGamer;
using Newtonsoft.Json;
using System;
namespace RogueFrontier;
public class EffectParticle : Effect {
    public double lifetime;
    public EffectParticle() { }
    public EffectParticle(XY Position, Tile Tile, double Lifetime) {
        this.position = Position;
        this.Velocity = new XY();
        this.tile = Tile;
        this.lifetime = Lifetime;
    }
    public EffectParticle(XY Position, XY Velocity, Tile Tile, double Lifetime) {
        this.position = Position;
        this.Velocity = Velocity;
        this.tile = Tile;
        this.lifetime = Lifetime;
    }
    public static void DrawArrow(System world, XY worldPos, XY offset, uint color) {
        //Draw an effect for the cursor
        world.AddEffect(new EffectParticle(worldPos, new Tile(color, ABGR.Transparent, 7), 1));
        //Draw a trail leading back to the player
        var trailNorm = offset.normal;
        var trailLength = Math.Min(3, offset.magnitude / 4) + 1;
        for (int i = 1; i < trailLength; i++) {
            world.AddEffect(new EffectParticle(worldPos - trailNorm * i, new Tile(color, ABGR.Transparent, (char)249), 1));
        }
    }
    public XY position { get; set; }
    public XY Velocity { get; set; }
    [JsonIgnore]
    public bool active => lifetime > 0;
    public Tile tile { get; set; }
    public void Update(double delta) {
        position += Velocity / Constants.TICKS_PER_SECOND;
        lifetime -= delta * 60;
    }
}
public class FadingTile : Effect {
    private int Lifetime;
    public FadingTile() {

    }
    public FadingTile(XY Position, Tile Tile, int Lifetime) {
        this.position = Position;
        this.Velocity = new();
        this._Tile = Tile;
        this.Lifetime = Lifetime;
    }
    public FadingTile(XY Position, XY Velocity, Tile Tile, int Lifetime) {
        this.position = Position;
        this.Velocity = Velocity;
        this._Tile = Tile;
        this.Lifetime = Lifetime;
    }
    public XY position { get; private set; }
    public XY Velocity { get; private set; }

    public bool active => Lifetime > 0;

    private Tile _Tile;
    public Tile tile => new Tile(

        ABGR.SetA(_Tile.Foreground, (byte) Math.Min(255, 255f * Lifetime / 10)),
        ABGR.SetA(_Tile.Background, (byte)Math.Min(255, 255f * Lifetime / 10)),
        _Tile.Glyph);

    public void Update(double delta) {
        position += Velocity / Constants.TICKS_PER_SECOND;
        Lifetime--;
    }
}
