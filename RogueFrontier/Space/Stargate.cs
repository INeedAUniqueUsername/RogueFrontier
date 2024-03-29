using Common;
using Newtonsoft.Json;
using SadConsole;
using System;
using System.Collections.Generic;
using SadRogue.Primitives;
using static RogueFrontier.StationType;
using System.Linq;

namespace RogueFrontier;

public class Stargate : ActiveObject {
    [JsonIgnore]
    public string name => $"Stargate";
    [JsonIgnore]
    public bool active => true;
    [JsonIgnore]
    public ColoredGlyph tile => new ColoredGlyph(Color.Violet, Color.DarkBlue, '*');

    [JsonProperty]
    public ulong id { get; private set; }
    [JsonProperty]
    public System world { get; private set; }
    [JsonProperty]
    public Sovereign sovereign { get; private set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public HashSet<Segment> Segments { get; private set; }

    public string gateId;
    public string destGateId;
    public Stargate destGate;
    [JsonIgnore]
    public System destWorld => destGate?.world;
    public Stargate() { }
    public Stargate(System World, XY Position) {
        this.id = World.nextId++;
        this.world = World;
        this.sovereign = Sovereign.Inanimate;
        this.position = Position;
        this.velocity = new XY();
    }
    public void CreateSegments() {
        Segments = new HashSet<Segment>();

        ColoredGlyph tile = new ColoredGlyph(Color.White, Color.Black, '+');

        int radius = 8;
        double circumference = 2 * Math.PI * radius;
        for (int i = 0; i < circumference; i++) {
            Segments.Add(new Segment(this, new SegmentDesc(
                XY.Polar(2 * Math.PI * i / circumference, radius), tile
                )));
            Segments.Add(new Segment(this, new SegmentDesc(
                XY.Polar(2 * Math.PI * i / circumference, radius - 0.5), tile
                )));
        }

        foreach (var i in Enumerable.Range(1 + radius, 5)) {
            Segments.Add(new Segment(this, new SegmentDesc(XY.Polar(0, i), tile)));
            Segments.Add(new Segment(this, new SegmentDesc(XY.Polar(Math.PI / 2, i), tile)));
            Segments.Add(new Segment(this, new SegmentDesc(XY.Polar(Math.PI, i), tile)));
            Segments.Add(new Segment(this, new SegmentDesc(XY.Polar(Math.PI * 3 / 2, i), tile)));
        }

        Rand r = new Rand();
        radius--;
        for (int i = 0; i < circumference; i++) {
            Segments.Add(new Segment(this, new SegmentDesc(
                XY.Polar(2 * Math.PI * i / circumference, radius),
                new ColoredGlyph(
                    Color.Violet.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                    Color.Blue.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                    '#')
                )));
        }
        for (int x = -radius + 1; x < radius; x++) {
            for (int y = -radius + 1; y < radius; y++) {
                if (x * x + y * y <= radius * radius) {
                    Segments.Add(new Segment(this, new SegmentDesc(
                        new XY(x, y),
                        new ColoredGlyph(
                            Color.BlueViolet.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                            Color.DarkBlue.SetAlpha((byte)(204 + r.NextInteger(-51, 51))), '%')
                    )));
                }
            }
        }

        foreach (var s in Segments) {
            world.AddEffect(s);
        }
    }

    public void Gate(AIShip ai) {
        ai.world.RemoveEntity(ai);
        if (destGate != null) {
            var world = destGate.world;
            ai.ship.world = world;
            ai.ship.position = destGate.position + (ai.ship.position - position);
            world.AddEntity(ai);
            world.AddEffect(new Heading(ai));
        }
    }
    public void Damage(Projectile p) {}

    public void Destroy(ActiveObject source) {}

    public void Update(double delta) {}
}
