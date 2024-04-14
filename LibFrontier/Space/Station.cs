using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;
using LibGamer;

namespace RogueFrontier;

public class Wreck : MovingObject, IDockable {
    [JsonIgnore]
    public string name => $"Wreck of {creator.name}";
    [JsonIgnore]
    public Tile tile => new Tile(ABGR.RGB(128, 128, 128), ABGR.Black, creator.tile.Glyph);
    [JsonProperty]
    public ulong id { get; private set; }
    [JsonProperty]
    public StructureObject creator { get; set; }
    [JsonProperty]
    public System world { get; private set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public bool active { get; private set; }
    [JsonProperty]
    public HashSet<Item> cargo { get; private set; }
    [JsonProperty]
    public int ticks { get; private set; }
    [JsonProperty]
    public XY gravity { get; private set; }
    public delegate void OnDestroyed(Wreck w);
    public Ev<OnDestroyed> onDestroyed = new();
    public Wreck() { }
    public Wreck(StructureObject creator, IEnumerable<Item> cargo = null) {
        this.id = creator.world.nextId++;
        this.creator = creator;
        this.world = creator.world;
        this.position = creator.position;
        this.velocity = creator.velocity;
        this.active = true;
        this.cargo = new(cargo ?? new List<Item>());

        gravity = new XY(0, 0);
    }
    public IEnumerable<XY> GetDockPoints() {
        yield return XY.Zero;
    }
    public IScene GetDockScene(IScene prev, PlayerShip playerShip) => new WreckScene(prev, playerShip, this);
    public void Damage(Projectile p) {}
    public void Destroy(ActiveObject source) {
        active = false;
        onDestroyed.ForEach(p => p(this));
    }
    public void Update(double delta) {
        position += velocity * delta;
#if false
        if (world.tick % 30 == 0) {
            gravity = new XY(0, 0);
            double stress = 0;
            foreach (var star in world.stars) {
                var towards = (star.position - position);
                var magnitude = Math.Pow(star.radius, 2) / (towards.magnitude2 * Program.TICKS_PER_SECOND);
                var pull = towards.WithMagnitude(magnitude);
                gravity += pull;
                stress += magnitude;
            }
            if (stress > 10f / Program.TICKS_PER_SECOND) {
                Destroy(null);
            }
        }
        velocity += gravity;
#else
        if(velocity.magnitude2 > 1) {
            velocity -= velocity.normal * delta;
        } else {
            velocity = new(0,0);
        }
#endif
    }
}
public class Station : ActiveObject, ITrader, IDockable {
    [JsonIgnore]
    public string name => type.name;
    [JsonProperty]
    public ulong id { get; set; }
    [JsonProperty]
    public System world { get; set; }
    [JsonProperty]
    public StationType type { get; set; }
    [JsonProperty]
    public Sovereign sovereign { get; set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public bool active { get; set; }
    [JsonProperty]
    public double rotation;
    [JsonProperty]
    public StationBehavior behavior;
    [JsonProperty]
    public HullSystem damageSystem;
    [JsonProperty]
    public List<Segment> segments = new();
    [JsonProperty]
    public HashSet<Item> cargo { get; set; } = new();
    [JsonProperty]
    public List<Weapon> weapons = new();
    [JsonProperty]
    public List<AIShip> guards = new();
    public ConstructionJob construction;
    public double stealth;
    public record Destroyed(Station station, ActiveObject destroyer, Wreck wreck);
    public Destroyed destroyed;
    public Vi<Destroyed> onDestroyed = new();
    public record Damaged(Station station, Projectile p);
    public Vi<Damaged> onDamaged = new();
    public record WeaponFired(Station station, Weapon w, List<Projectile> p);
    public Vi<WeaponFired> onWeaponFire = new();
    public Station() { }
    public Station(System World, StationType type, XY Position) {
        this.id = World.nextId++;
        this.world = World;
        this.type = type;
        this.position = Position;
        this.velocity = new XY();
        this.active = true;
        this.sovereign = type.sovereign;
        damageSystem = type.hull.Create(world.types);
        cargo.UnionWith(type.Inventory?.Generate(World.types)??new());
        weapons.AddRange(this.type.Weapons?.Generate(World.types) ?? new());
        weapons.ForEach(w => {
            w.aiming = w.aiming ?? new Omnidirectional();
            w.targeting = w.targeting ?? new Targeting(true);
        });
        InitBehavior(type.behavior);
    }
    public enum Behaviors {
        none,
        raisu,
        pirate,
        reinforceNearby,
        constellationShipyard,
        amethystStore,
        orionWarlords,
        daughtersOutpost
    }
    public void InitBehavior(Behaviors? behavior = null) {
        this.behavior = (behavior ?? type.behavior) switch {
            Behaviors.raisu => null,
            Behaviors.pirate => new IronPirateOutpost(),
            Behaviors.reinforceNearby => new ConstellationAstra(this),
            Behaviors.constellationShipyard => new ConstellationShipyard(this),
            Behaviors.orionWarlords => new OrionWarlordOutpost(this),
            Behaviors.amethystStore=>new AmethystStore(this),
            Behaviors.daughtersOutpost => new DaughtersOutpost(),
            Behaviors.none => null,
            _ => null
        };
    }
    public void CreateSegments() {
        segments.Clear();
        foreach (var segmentDesc in type.segments) {
            var s = new Segment(this, segmentDesc);
            segments.Add(s);
            world.AddEntity(s);
        }
    }
    public void CreateGuards() {
        guards.Clear();
        foreach (var guard in type.Ships?.Generate(world.types, this) ?? guards) {
            guards.Add(guard);
            world.AddEntity(guard);
            world.AddEffect(new Heading(guard));
        }
    }
    public void CreateSatellites(LocationContext lc) {
        type.Satellites?.Generate(lc, world.types);
    }
    public IEnumerable<AIShip> GetDocked() =>
        world.entities.FilterKey(p => (position - p).magnitude < 5)
            .OfType<AIShip>().Where(s => s.dock.Target == this);
    public IEnumerable<XY> GetDockPoints() =>
        type.dockPoints.Except(GetDocked().Select(s => s.dock.Offset)).Append(XY.Zero);
    public List<AIShip> UpdateGuardList() {
        return guards = new(world.universe.GetAllEntities().OfType<AIShip>()
            .Where(s => s.behavior switch {
                GuardAt { home: { }home } => home == this,
                PatrolAt { patrolTarget: { }target } => target == this,
                PatrolAround{patrolTarget:{ }target } => target == this,
                _ => false
            }));
    }
    public void Damage(Projectile p) {
        damageSystem.Damage(world.tick, p, () => Destroy(p.source));
        onDamaged.Observe(new(this, p));
        if (!active) {
            return;
        }
        var source = p.source;
        if (source != null && source.sovereign != sovereign) {
            var guards = world.entities.all.OfType<AIShip>()
                .Select(s => s.behavior.GetOrder())
                .OfType <GuardAt>()
                .Where(g => g.home == this);
            foreach (var order in guards) {
                order.SetAttack(source, 300);
            }
        }
    }
    public void Destroy(ActiveObject source) {
        active = false;
        if (source is PlayerShip ps) {
            ps.stationsDestroyed.Add(this);
            if (type.crimeOnDestroy) {
                ps.crimeRecord.Add(new DestructionCrime(this));
            }
        }
        if(type.ExplosionDesc != null)
            new Weapon() { projectileDesc = type.ExplosionDesc, targeting = new Targeting(false) { target = source } }.Fire(this, rotation);
        var drop = weapons.Where(w => !w.structural).Select(w => w.source)
            .Concat(cargo)
            .Concat((damageSystem as LayeredArmor)?.layers.Select(l => l.source) ?? new List<Item>());
        var wreck = new Wreck(this, drop);
        world.AddEntity(wreck);
        if (segments != null) {
            foreach (var segment in segments) {
                var offset = segment.desc.offset;
                var tile = new Tile(ABGR.RGB(128, 128, 128), ABGR.Black, segment.desc.tile.glyph);
                world.AddEntity(new Segment(wreck, new SegmentDesc(offset, new StaticTile(tile))));
            }
        }
        var guards = world.entities.all.OfType<AIShip>().Where(
            s => s.behavior is GuardAt o && o.home == this);
        var gate = world.entities.all.OfType<Stargate>().FirstOrDefault();
        IShipOrder lastOrder = gate == null ? new AttackTarget(source) : new CompoundOrder(new AttackTarget(source), new GateThrough(gate));
        if (source != null && source.sovereign != sovereign) {
            foreach (var g in guards) {
                g.behavior = lastOrder;
            }
        } else {
            var next = world.entities.all.OfType<Station>().Where(s => s.type == type && s != this).OrderBy(p => (p.position - position).magnitude2).FirstOrDefault();
            if (next != null) {
                foreach (var g in guards) {
                    var o = (GuardAt)g.behavior;
                    o.SetHome(next);
                }
            } else {
                foreach (var g in guards) {
                    g.behavior = new PatrolAt(this, 20);
                }
            }
        }
        onDestroyed.Observe(destroyed = new(this, source, wreck));
    }
    public void Update(double delta) {
        velocity = XY.Zero;
        if(world.tick%15 == 0) {
            stealth = type.stealth;
            if (weapons.Any()) {
                stealth *= Math.Min(1, weapons.Min(w => w.timeSinceLastFire));
            }
            if(construction != null) {
                construction.time -= 15;
                if (construction.time < 1) {
                    var s = new AIShip(new(world, construction.desc.type, position), sovereign, construction.desc.order.Value(this));
                    world.AddEntity(s);
                    guards.Add(s);
                    construction = null;
                }
            } else if(type.Construction != null) {
                if(guards.Count < type.Construction.max) {
                    construction = type.Construction.catalog.GetRandom(world.karma);
                }
            }
        }
        (damageSystem as LayeredArmor)?.layers.ForEach(l => l.Update(delta, this));
        weapons?.ForEach(w => w.Update(delta, this));
        behavior?.Update(delta, this);
    }
    public ScreenSurface GetDockScene(ScreenSurface prev, PlayerShip playerShip) => null;
    [JsonIgnore]
    public Tile tile => type.tile.Original;
}
public interface ISegment : MovingObject {
    MovingObject parent { get; }
}
public class Segment : ISegment {
    //The segment essentially impersonates its parent station but with a different tile
    public System world => parent.world;
    public XY position => parent.position + desc.offset;
    public XY velocity => parent.velocity;
    public ulong id { get; private set; }
    public MovingObject parent { get; private set; }
    public SegmentDesc desc { get; private set; }
    public Segment() { }
    public Segment(MovingObject parent, SegmentDesc desc) {
        this.id = parent.world.nextId++;
        this.parent = parent;
        this.desc = desc;
    }
    public bool active => parent.active;
    public void Update(double delta) {}
    public Tile tile => desc.tile.Original;
}
public class AngledSegment : ISegment {
    //The segment essentially impersonates its parent station but with a different tile
    [JsonIgnore]
    public string name => parent.name;
    [JsonIgnore]
    public System world => parent.world;
    [JsonIgnore]
    public XY position => parent.position + desc.offset.Rotate(parent.rotationRad);
    [JsonIgnore]
    public XY velocity => parent.velocity;
    [JsonIgnore]
    public Sovereign sovereign => parent.sovereign;
    [JsonProperty]
    public ulong id { get; private set; }
    [JsonProperty]
    public IShip parent { get; private set; }
    [JsonIgnore]
    MovingObject ISegment.parent => parent;
    [JsonProperty]
    public SegmentDesc desc { get; private set; }
    public AngledSegment() { }
    public AngledSegment(IShip parent, SegmentDesc desc) {
        this.id = parent.world.nextId++;
        this.parent = parent;
        this.desc = desc;
    }
    [JsonIgnore]
    public bool active => parent.active;
    public void Damage(Projectile p) => parent.Damage(p);
    public void Destroy(ActiveObject source) => parent.Destroy(source);
    public void Update(double delta) { }
    [JsonIgnore]
    public Tile tile => desc.tile.Original;
}