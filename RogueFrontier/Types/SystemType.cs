using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace RogueFrontier;
public record SystemType : IDesignType {
    [Req] public string codename;
    public SystemGroup systemGroup;
    public SystemType() {

    }
    public SystemType(SystemGroup system) {
        this.systemGroup = system;
    }
    public void Initialize(TypeCollection collection, XElement e) {
        e.Initialize(this);
        if (e.HasElement("SystemGroup", out var xmlSystem)) {
            systemGroup = new SystemGroup(xmlSystem, SGenerator.ParseFrom(collection, SSystemElement.Create));
        }
    }
    public void Generate(System world) {
        systemGroup.Generate(new LocationContext() {
            pos = new XY(0, 0),
            focus = new XY(0, 0),
            world = world,
            angle = 0,
            radius = 0
        }, world.types);
    }
}
public record LocationContext {
    public System world;
    public XY pos;
    public double angle;
    public double angleRad => angle * Math.PI / 180;
    public double radius;
    public XY focus;
    public int index;
}
public record LocationMod {
    [Opt] public IDice radius = null;
    [Opt] public IDice radiusInc = new Constant(0);

    [Opt] public IDice angle = null;
    [Opt] public IDice angleInc = new Constant(0);

    [Opt] public IDice arcInc = new Constant(0);
    public LocationMod() { }
    public LocationMod(XElement e) {
        e.Initialize(this);
    }
    public void Get(LocationContext lc, out double r, out double a) {
        r = radius?.Roll() ?? (lc.radius + radiusInc.Roll());
        a = angle?.Roll() ?? (lc.angle + angleInc.Roll() + (r == 0 ? 0 : arcInc.Roll() / r));
    }
    public LocationContext Adjust(LocationContext lc) {
        Get(lc, out var r, out var a);
        var p = lc.focus + XY.Polar(a * Math.PI / 180, r);
        return lc with { radius = r, angle = a, pos = p };
    }
}

public interface SystemElement {
    void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null);
}
public static class SSystemElement {
    public static SystemElement Create(TypeCollection tc, XElement e) {
        var f = SGenerator.ParseFrom(tc, Create);

        return e.Name.LocalName switch {
            "System"    => new SystemGroup(e, f),
            "Group"     => new SystemGroup(e, f),
            "Table"     => new SystemTable(e, f),
            "Orbital"   => new SystemOrbital(e, f),
            "Planet"    => new SystemPlanet(e),
            "Asteroids" => new SystemAsteroids(e),
            "Nebula"    => new SystemNebula(e),
            "Sibling"   => new SystemSibling(e, f),
            "Star"      => new SystemStar(e),
            "Stargate"  => new SystemStargate(tc, e),
            "Station"   => new SystemStation(tc, e),
            "Ship"      => new SystemShips(tc, e),
            "Ships"     => new SystemShips(tc, e),
            "At"        => new SystemAt(e, f),
            "Marker"    => new SystemMarker(e),
            _           => throw new Exception($"Unknown system element <{e.Name}>")
        };
    }
}
public record SystemGroup() : SystemElement {
    [Par] public LocationMod loc;
    public List<SystemElement> subelements;
    public SystemGroup(XElement e, Parse<SystemElement> parse):this() {
        e.Initialize(this);
        subelements = e.Elements().Select(e => parse(e)).ToList();
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var sub_lc = loc.Adjust(lc);
        try {
            subelements.ForEach(g => g.Generate(sub_lc, tc, result));
        } catch (Exception e) {
            subelements.Reverse<SystemElement>().ToList().ForEach(g => g.Generate(sub_lc, tc, result));
        }
        
    }
}

public record SystemTable() : SystemElement {
    [Opt] public IDice count = new Constant(1);
    [Opt] public bool replacement = true;
    public List<(double chance, SystemElement element)> generators;
    private double totalChance;
    public SystemTable(XElement e, Parse<SystemElement> parse) : this() {
        e.Initialize(this);
        generators = new();
        foreach (var element in e.Elements()) {
            var chance = element.ExpectAttDouble("chance");
            generators.Add((chance, parse(element)));
            totalChance += chance;
        }
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        if (replacement) {
            Enumerable.Range(0, count.Roll()).ToList().ForEach(i => {
                var c = lc.world.karma.NextDouble() * totalChance;
                foreach ((var chance, var g) in generators) {
                    if (c < chance) {
                        g.Generate(lc, tc, result);
                        return;
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            });
        } else {
            List<(double chance, SystemElement element)> choicesLeft;
            double totalChanceLeft;
            ResetTable();
            Enumerable.Range(0, count.Roll()).ToList().ForEach(i => {
                if (totalChanceLeft > 0) {
                    ResetTable();
                }
                var c = lc.world.karma.NextDouble() * totalChanceLeft;
                for (int j = 0; j < choicesLeft.Count; j++) {
                    (var chance, var g) = generators[j];
                    if (c < chance) {
                        generators.RemoveAt(j);
                        totalChanceLeft -= chance;
                        g.Generate(lc, tc, result);
                        return;
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            });

            void ResetTable() {
                choicesLeft = new(generators);
                totalChanceLeft = totalChance;
            }

        }
    }
}


public record SystemAt() : SystemElement {
    public List<SystemElement> subelements;
    public HashSet<int> index;
    public SystemAt(XElement e, Parse<SystemElement> parse) : this() {
        subelements = e.Elements().Select(e => parse(e)).ToList();
        index = e.ExpectAtt("index").Split(",").Select(s => s.Trim()).Select(int.Parse).ToHashSet();
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        if (index.Contains(lc.index)) {
            subelements.ForEach(g => g.Generate(lc, tc, result));
        }
    }
}

public record SystemOrbital() : SystemElement {
    public List<SystemElement> subelements;

    [Opt] public IDice count = new Constant(1);

    public IDice angle;
    public IDice angleInc;

    public IDice increment;
    public bool equidistant;

    [Opt] public int radius;
    public SystemOrbital(XElement e, Parse<SystemElement> parse) : this() {
        subelements = e.Elements().Select(e => parse(e)).ToList();

        e.Initialize(this);
        if (e.TryAtt(nameof(angle), out var a)) {
            switch (a) {
                case "random":
                    angle = new IntRange(0, 360);
                    break;
                case var i when IDice.TryParse(i, out var d):
                    angle = d;
                    break;
                case var unknown:
                    throw new Exception($"Invalid angle {unknown}");
            }
        } else if (e.TryAtt(nameof(angleInc), out var ai)) {
            angleInc = IDice.Parse(ai);
        }
        switch (e.TryAttNullable(nameof(increment))) {
            case null:
                break;
            case "random":
                increment = new IntRange(0, 360);
                break;
            case "equidistant":
                equidistant = true;
                break;
            case var i when IDice.TryParse(i, out var d):
                increment = d;
                break;
            case var unknown:
                throw new Exception($"Invalid increment {unknown}");
        }
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {

        if (subelements.Count == 0) return;

        var count = this.count.Roll();
        var angle = this.angle?.Roll() ?? (lc.angle + (angleInc?.Roll()??0));

        int equidistantInterval = 360 / (subelements.Count * count);
        for (int i = 0; i < count; i++) {
            foreach (var sub in subelements) {
                var loc = lc with {
                    angle = angle,
                    radius = radius,
                    focus = lc.pos,
                    pos = lc.pos + XY.Polar(angle * Math.PI / 180, radius),
                    index = i
                };
                sub.Generate(loc, tc, result);

                if (increment is IDice d) {
                    angle += d.Roll();
                } else if (equidistant) {
                    angle += equidistantInterval;
                }
            }
        }
    }
}
public record SystemMarker() : SystemElement {
    [Req] public string name;
    public SystemMarker(XElement e) : this() {
        e.Initialize(this);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var m = new Marker(lc.world, name, lc.pos);
        lc.world.AddEntity(m);
        result?.Add(m);
    }
}
public record SystemPlanet() : SystemElement {
    [Req] public int radius;
    [Opt] public bool showOrbit = true;
    public SystemPlanet(XElement e) : this() {
        e.Initialize(this);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var diameter = radius * 2;
        var radius2 = radius * radius;
        var center = new XY(radius, radius);

        var r = lc.world.karma;
        ColoredGlyph[,] tiles = new ColoredGlyph[diameter, diameter];
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
            var yEnd = radius + yRange;
            for (int y = yStart; y < yEnd; y++) {
                var pos = lc.pos + (new XY(x, y) - center);

                var f = Color.LightBlue;
                f = f.Blend(Color.DarkBlue.SetAlpha((byte)r.NextInteger(0, 153)));
                f = f.Blend(Color.Gray.SetAlpha(102));

                var tile = new ColoredGlyph(f, Color.Black, '%');
                lc.world.backdrop.planets.tiles[pos] = tile;
                tiles[x, y] = tile;
                //lc.world.AddEffect(new FixedTile(tile, pos));
            }
        }
        var circ = radius * 2 * Math.PI;
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
            var yEnd = radius + yRange;
            for (int y = yStart; y < yEnd; y++) {
                var loc = r.NextDouble() * circ * (radius - 2);
                var from = center + XY.Polar(loc % 2 * Math.PI, loc / circ);
                var t = tiles[x, y];
                t.Foreground = t.Foreground.Blend(tiles[from.xi, from.yi].Foreground.SetAlpha((byte)r.NextInteger(0, 51)));
            }
        }
#if false
        if (showOrbit) {
            var orbitFocus = lc.focus;
            var orbitRadius = lc.radius;
            var orbitCirc = orbitRadius * 2 * Math.PI;
            for (int i = 0; i < orbitCirc; i++) {
                var angle = i / orbitRadius;
                lc.world.backdrop.orbits.tiles[orbitFocus + XY.Polar(angle, orbitRadius)] = new ColoredGlyph(Color.LightGray, Color.Transparent, '.');

                for(double j = 0.7; j < orbitRadius / 60; j += 0.7) {

                    lc.world.backdrop.orbits.tiles[orbitFocus + XY.Polar(angle, orbitRadius - j)] = new ColoredGlyph(Color.LightGray, Color.Transparent, '.');
                    lc.world.backdrop.orbits.tiles[orbitFocus + XY.Polar(angle, orbitRadius + j)] = new ColoredGlyph(Color.LightGray, Color.Transparent, '.');
                }
            }
        }
#endif
    }
}
public record SystemAsteroids() : SystemElement {
    [Req] public double angle;
    [Req] public int size;
    public SystemAsteroids(XElement e) : this() {
        e.Initialize(this);
        angle *= Math.PI / 180;
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        double arc = lc.radius * angle;
        double halfArc = arc / 2;
        for (double i = -halfArc; i < halfArc; i++) {
            int localSize = (int)(size * Math.Abs(Math.Abs(i) - halfArc) / halfArc);
            for (int j = -localSize / 2; j < localSize / 2; j++) {
                if (lc.world.karma.NextDouble() > 0.02) {
                    continue;
                }
                var p = XY.Polar(lc.angleRad + i / lc.radius, lc.radius + j);
                lc.world.AddEntity(new Asteroid(lc.world, p));
            }
        }
    }
}
public record SystemNebula() : SystemElement {
    [Req] public double angle;
    [Req] public int size;
    public SystemNebula(XElement e) : this() {
        e.Initialize(this);
        angle *= Math.PI / 180;
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        double arc = lc.radius * angle;
        double halfArc = arc / 2;
        for (double i = -halfArc; i < halfArc; i += 0.1) {
            int localSize = (int)(lc.world.karma.NextDouble() * 2 * size * Math.Abs(Math.Abs(i) - halfArc) / halfArc);

            for (int j = -localSize / 2; j < localSize / 2; j++) {
                var p = XY.Polar(lc.angleRad + i / lc.radius, lc.radius + j);

                var tile = new ColoredGlyph(Color.Violet.SetAlpha((byte)(64 + 128 * lc.world.karma.NextDouble())), Color.Transparent, '%');
                lc.world.backdrop.nebulae.tiles[p] = tile;
            }
        }
    }
}
public record SystemSibling() : SystemElement {
    [Opt] public IDice count = new Constant(1);
    [Opt] public IDice increment = new IntRange(0, 360);
    [Par] public LocationMod mod;
    public List<SystemElement> subelements;
    public SystemSibling(XElement e, Parse<SystemElement> parse) : this() {
        e.Initialize(this);
        subelements = e.Elements().Select(e => parse(e)).ToList();
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var count = this.count.Roll();

        mod.Get(lc, out var r, out var a);
        var sub_lc = lc with {
            angle = a,
            radius = r,
            pos = lc.focus + XY.Polar(a * Math.PI / 180, r)
        };

        Generate:
        subelements.ForEach(s => s.Generate(sub_lc, tc));
        if (count > 1) {
            count--;

            a += increment.Roll();
            sub_lc = lc with {
                angle = a,
                radius = r,
                pos = lc.focus + XY.Polar(a * Math.PI / 180, r)
            };
            goto Generate;
        }
    }
}
public record LightGenerator() : IGridGenerator<Color> {
    public LocationContext lc;
    public int radius;
    public LightGenerator(LocationContext lc, int radius) : this() {
        this.lc = lc;
        this.radius = radius;
    }
    public Color Generate((long, long) p) {
        //var xy = new XY(p);
        return new Color(255, 255, 204, Math.Min(255, (int)(radius * 255 / ((lc.pos - p).magnitude + 1))));
    }
}
public record SystemStar() : SystemElement {
    public int radius;
    public SystemStar(XElement e) : this() {
        this.radius = e.ExpectAttInt("radius");
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        /*
        var diameter = radius * 2;
        var radius2 = radius * radius;
        var center = new XY(radius, radius);
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = Math.Round(yRange, MidpointRounding.AwayFromZero);
            for (int y = -(int)yStart; y < yRange; y++) {
                var pos = new XY(x, y);
                var offset = (pos - center);
                var tile = new ColoredGlyph(Color.Gray, Color.Black, '%');
                lc.world.AddEffect(new FixedTile(tile, lc.pos + offset));
            }
        }
        */
        var rectSize = radius * 16;
        lc.world.backdrop.starlight.AddLayer(0, new GeneratedGrid<Color>(new LightGenerator(lc, radius)), new(lc.pos, rectSize, rectSize));
        lc.world.stars.Add(new Star(lc.pos, radius));
    }
}
public record SystemShips : SystemElement {
    private Sovereign sovereign;
    public ShipGenerator ships;
    public SystemShips() { }
    public SystemShips(TypeCollection tc, XElement e) {
        sovereign = tc.Lookup<Sovereign>(e.ExpectAtt(nameof(sovereign)));
        ships = SGenerator.ShipFrom(tc, e);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var m = new ActiveMarker(lc.world, sovereign, lc.pos);
        ships.GenerateAndPlace(tc, m);
    }
}
public record SystemStation() : SystemElement {
    [Opt]public string id = "";
    [Req] public string codename;
    
    public ShipGroup ships;
    public StationType stationtype;

    public bool cargoReplace;
    public IGenerator<Item> cargo;
    public SystemStation(TypeCollection tc, XElement e) : this() {
        e.Initialize(this);
        stationtype = tc.Lookup<StationType>(codename);
        ships = e.HasElement("Ships", out var xmlShips) ? new(xmlShips, SGenerator.ParseFrom(tc, SGenerator.ShipFrom)) : null;

        if (e.HasElement("Cargo", out var xmlCargo)) {
            cargo = SGenerator.ItemFrom(tc, xmlCargo);
            cargoReplace = xmlCargo.TryAttBool("replace");
        }
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var w = lc.world;
        var s = new Station(w, stationtype, lc.pos);

        if (cargo != null) {
            if (cargoReplace) {
                s.cargo.Clear();
            }
            s.cargo.UnionWith(cargo.Generate(tc));
        }

        if (id.Any() == true) {
            w.universe.identifiedObjects[id] = s;
        }
        w.AddEntity(s);
        s.CreateSegments();
        s.CreateGuards();
        s.CreateSatellites(lc);
        ((ShipGenerator)ships)?.GenerateAndPlace(tc, s);
    }
}
public record SystemStargate() : SystemElement {
    [Req] public string gateId;
    [Opt] public string destGateId = "";
    public ShipGenerator ships;
    public SystemStargate(TypeCollection tc, XElement e) : this() {
        e.Initialize(this);
        if (e.HasElement("Ships", out XElement xmlShips)) {
            ships = new ShipGroup(xmlShips, SGenerator.ParseFrom(tc, SGenerator.ShipFrom));
        }
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var s = new Stargate(lc.world, lc.pos) { gateId = gateId, destGateId = destGateId };
        lc.world.AddEntity(s);
        s.CreateSegments();

        ships?.GenerateAndPlace(tc, s);
    }
}
