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
    public static void DrawArrow(World world, XY worldPos, XY offset, uint color) {
        //Draw an effect for the cursor
        world.AddEffect(new EffectParticle(worldPos, new Tile(color, ABGR.Transparent, 7), 1));
        //Draw a trail leading back to the player
        var trailNorm = offset.normal;
        var trailLength = Min(3, offset.magnitude / 4) + 1;
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

    private double lifespan;
    private double timeLeft;
    public FadingTile() {

    }
    public FadingTile(XY Position, Tile Tile, int Lifetime) {
        this.position = Position;
        this.velocity = new();
        this.original = Tile;
        this.timeLeft = Lifetime / 30d;
        this.lifespan = timeLeft;
    }
    public FadingTile(XY Position, XY Velocity, Tile Tile, int Lifetime) {
        this.position = Position;
        this.velocity = Velocity;
        this.original = Tile;
        this.timeLeft = Lifetime / 30d;
		this.lifespan = timeLeft;
	}
    public XY position { get; private set; }
    public XY velocity { get; private set; }
    public bool active => timeLeft > 0;
    private Tile original;
    public Tile tile => new(
        ABGR.SetA(original.Foreground, (byte)Main.Lerp(timeLeft, 0, lifespan, 0, ABGR.A(original.Foreground), 0.5)),
        ABGR.SetA(original.Background, (byte)Main.Lerp(timeLeft, 0, lifespan, 0, ABGR.A(original.Background), 0.5)),

        original.Glyph);

    public void Update(double delta) {
        position += velocity / Constants.TICKS_PER_SECOND;
        timeLeft -= delta;
    }
}
