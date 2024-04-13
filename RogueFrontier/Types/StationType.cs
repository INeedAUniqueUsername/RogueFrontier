using Common;
using System.Collections.Generic;
using System.Xml.Linq;
using Color = SadRogue.Primitives.Color;
using System;
using SadConsole;
using System.Linq;
using ASECII;
using Newtonsoft.Json;
using SFML.Audio;

namespace RogueFrontier;

public class StationType : IDesignType {
    [Req] public string codename;
    [Req] public string name;
    [Opt] public bool crimeOnDestroy;
    [Opt] public double stealth;
    [Opt(parse = false)] public SoundBuffer discoverySound;


    [Opt(separator = ";")] public HashSet<string> attributes = new();

    [Sub(alias = "HP", type = typeof(HitPointDesc))]
    [Sub(alias = "LayeredArmor", type = typeof(LayeredArmorDesc), fallback = true)]
    [Err(msg = "Hull system expected")]
    public HullSystemDesc hull;


    [Opt(alias ="explosionType", parse = false)]
    [Sub(alias = "Explosion", fallback = true)]
    public FragmentDesc ExplosionDesc;

    [Opt] public Station.Behaviors behavior;
    [Req(parse = false)] public Sovereign sovereign;

    public StaticTile tile;
    public List<SegmentDesc> segments;
    public List<XY> dockPoints;


    [Sub(alias = "Cargo", construct = false)]
    [Sub(alias = "Items", construct = false)]
    public Group<Item> Inventory;
    [Sub]
    public WeaponList Weapons;


    [Sub(construct = false)] public ConstructionDesc Construction;
    [Sub(construct = false)] public ShipGroup Ships;
    [Sub(construct = false)] public SystemGroup Satellites;
    [Sub(construct = false)] public Dictionary<(int, int), ColoredGlyph> HeroImage;


    class MultiPointFrom {
        [Par] public StaticTile tile;
        [Req] public int angleInc;
        [Req] public double offsetX, offsetY;
        public MultiPointFrom(XElement e) => e.Initialize(this);
    }
    class LineFrom {
        [Req] public double offsetX, offsetY;
        [Par] public StaticTile tile;


        public bool delta;
        [Opt] public double deltaX, deltaY;

        public bool size;
        [Opt] public double width = 1, height = 1;
        public LineFrom(XElement e) => e.Initialize(this, transform: new() {
            [nameof(deltaX)] = () => delta = true,
            [nameof(deltaY)] = () => delta = true,

            [nameof(width)] = () => size = true,
            [nameof(height)] = () => size = true,
        });
    }
    class ColorsFrom {
        [Opt] public Color f, b;
        public ColorsFrom(XElement e, Color f, Color b) {
            this.f = f;
            this.b = b;
            e.Initialize(this);
        }
    }
    public void Initialize(TypeCollection tc, XElement e) {
        e.Initialize(this, transform: new() {
            [nameof(Construction)] = (XElement x) => new ConstructionDesc(tc, x),
            [nameof(Ships)] = (XElement x) => new ShipGroup(x, SGenerator.ParseFrom(tc, SGenerator.ShipFrom)),
            [nameof(sovereign)] = (string sov) => tc.Lookup<Sovereign>(sov),
            [nameof(Satellites)] = (XElement x) => new SystemGroup(x, (XElement ele) => SSystemElement.Create(tc, ele)),
            [nameof(Inventory)] = (XElement x) => new Group<Item>(x, SGenerator.ParseFrom(tc, SGenerator.ItemFrom)),
            [nameof(ExplosionDesc)] = (object o) => 
                o is string explosionType ?
                    tc.Lookup<ItemType>(explosionType).Weapon.Projectile ?? throw new Exception($"Expected Weapon desc") :
                    (FragmentDesc) o,
            [nameof(HeroImage)] = (XElement x) => {
                if (x.TryAtt("path", out string path)) {
                    return ColorImage.FromFile(path).Sprite;
                } else {
                    var heroImageText = x.Value.Trim('\n').Replace("\r\n", "\n").Split('\n');
                    var heroImageTint = x.TryAttColor("tint", Color.White);
                    return heroImageText.ToImage(heroImageTint);
                }
            },
            [nameof(discoverySound)] = (string s) => new SoundBuffer(s)
        });
        dockPoints = new();

        segments = new();
        if (e.TryAtt("structure", out var structure)) {
            var sprite = ASECIILoader.LoadCG(structure).ToDictionary(
                pair => (pair.Key.Item1, -pair.Key.Item2),
                pair => pair.Value.cg);
            tile = sprite.TryGetValue((0, 0), out var cg) ? new(cg) : null;
            sprite.Remove((0, 0));
            segments.AddRange(sprite.Select((pair) => new SegmentDesc(new(pair.Key.Item1, pair.Key.Item2), pair.Value)));
        } else {
            tile = new(e);
            if (e.HasElement("Segments", out var xmlSegments)) {
                foreach (var xmlSegment in xmlSegments.Elements()) {
                    switch (xmlSegment.Name.LocalName) {
                        case "MultiPoint": {
                                var m = new MultiPointFrom(xmlSegment);
                                XY offset = new(m.offsetX, m.offsetY);
                                for (int angle = 0; angle < 360; angle += m.angleInc) {
                                    segments.Add(new SegmentDesc(offset.Rotate(angle * Math.PI / 180), m.tile));
                                }
                                break;
                            }
                        case "Line": {
                                var l = new LineFrom(xmlSegment);
                                var (x, y) = (l.offsetX, l.offsetY);
                                if (l.delta) {
                                    var (dx, dy) = (l.deltaX, l.deltaY);
                                    segments.AddRange(new XY(x, y)
                                        .LineTo(new(x + dx, y + dy))
                                        .Select(p => new SegmentDesc(p, tile)));
                                } else if (l.size) {
                                    var (w, h) = (l.width, l.height);
                                    segments.AddRange(new XY(x - w / 2, y - h / 2)
                                        .LineTo(new(x + w / 2, y + h / 2))
                                        .Select(p => new SegmentDesc(p, tile)));
                                }
                                break;
                            }
                        case "Ring": {
                                var c = new ColorsFrom(xmlSegment, Color.White, Color.Transparent);
                                segments.AddRange(CreateRing(c.f, c.b));
                                break;
                            }
                        case "Box": {
                                var c = new ColorsFrom(xmlSegment, Color.White, Color.Transparent);
                                segments.AddRange(CreateBox(c.f, c.b));
                                break;
                            }
                        case "Point":
                            segments.Add(new(xmlSegment));
                            break;
                    }
                }
            }
        }
        if (e.HasElement("Dock", out var xmlDock)) {
            foreach (var xmlPart in xmlDock.Elements()) {
                switch (xmlPart.Name.LocalName) {
                    case "MultiPoint": {
                            int angleInc = xmlPart.ExpectAttInt("angleInc");
                            var x = xmlPart.ExpectAttDouble("offsetX");
                            var y = xmlPart.ExpectAttDouble("offsetY");
                            var offset = new XY(x, y);
                            for (int angle = 0; angle < 360; angle += angleInc) {
                                dockPoints.Add(offset.Rotate(angle * Math.PI / 180));
                            }
                            break;
                        }
                    case "Ring": {
                            dockPoints.AddRange(CreateRing());
                            break;
                        }
                    case "Point": {
                            var x = xmlPart.ExpectAttDouble("offsetX");
                            var y = xmlPart.ExpectAttDouble("offsetY");
                            dockPoints.Add(new(x, y));
                            break;
                        }
                }
            }
        }
    }
    public static List<SegmentDesc> CreateRing(Color f, Color b) =>
        new(new (int x, int y, char c)[] {
            (0, 1, '-'),
            (1, 1, '\\'),
            (1, 0, '|'),
            (1, -1, '/'),
            (0, -1, '-'),
            (-1, -1, '\\'),
            (-1, 0, '|'),
            (-1, 1, '/')
        }.Select(p => new SegmentDesc(new(p.x, p.y), new(p.c, f, b))));
    public static List<SegmentDesc> CreateBox(Color f, Color b) =>
        new(new (int x, int y, char c)[] {
            (0, 1,   '-'),
            (1, 1,   '+'),
            (1, 0,   '|'),
            (1, -1,  '+'),
            (0, -1,  '-'),
            (-1, -1, '+'),
            (-1, 0,  '|'),
            (-1, 1,  '+')
        }.Select(p => new SegmentDesc(new(p.x, p.y), new(p.c, f, b))));
    public static List<XY> CreateRing() {
        return new() {
            (0, 1),
            (1, 1),
            (1, 0),
            (1, -1),
            (0, -1),
            (-1, -1),
            (-1, 0),
            (-1, 1)
        };
    }

    public class SegmentDesc {
        public XY offset;
        [Par] public StaticTile tile;

        class Offset {
            [Req] public double offsetX;
            [Req] public double offsetY;
            public Offset(XElement e) => e.Initialize(this);
            public XY Pos => new XY(offsetX, offsetY);
        }
        public SegmentDesc() {

        }
        public SegmentDesc(XY offset, StaticTile tile) {
            this.offset = offset;
            this.tile = tile;
        }
        public SegmentDesc(XElement e) {
            offset = new Offset(e).Pos;
            e.Initialize(this);
        }
    }

    public record ConstructionDesc {
        [Opt]public int max = int.MaxValue;
        public List<ConstructionEntry> catalog;
        public ConstructionDesc() {
        }
        public ConstructionDesc(TypeCollection tc, XElement e) {
            e.Initialize(this);
            catalog = e.Elements("Construct").Select(s => new ConstructionEntry(tc, s)).ToList();
        }
    }
    public record ConstructionEntry {
        [Req] public int time;
        public ShipClass type;
        public ShipEntry.IShipOrderDesc order;
        public ConstructionEntry() { }
        public ConstructionEntry(TypeCollection tc, XElement e) {
            e.Initialize(this);
            type = tc.Lookup<ShipClass>(e.ExpectAtt("codename"));
            order = ShipEntry.IShipOrderDesc.Get(e);
        }
        public static implicit operator ConstructionJob(ConstructionEntry e) => new() { time=e.time, desc=e};
    }

    public class ConstructionJob {
        public int time;
        public ConstructionEntry desc;
    }
}
