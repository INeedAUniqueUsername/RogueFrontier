using Common;
using Newtonsoft.Json;
using SadConsole;

namespace RogueFrontier;

class Marker : Entity {
    public ulong id => 0xFFFFFFFFFFFFFFFF;
    public string Name { get; private set; }
    public XY position { get; set; }
    public bool active { get; set; }
    public ColoredGlyph tile => null;
    public XY Velocity { get; set; }
    public System world;
    public Marker(System world, string Name, XY Position) {
        this.world = world;
        this.Name = Name;
        this.position = Position;
        this.Velocity = new XY();
        this.active = true;
    }
    public void Update(double delta) { }
}

class ActiveMarker : ActiveObject {
    [JsonIgnore]
    public ColoredGlyph tile => null;
    [JsonIgnore]
    public System world { get; set; }
    [JsonIgnore]
    public Sovereign sovereign { get; set; }
    [JsonIgnore]
    public ulong id => 0xFFFFFFFFFFFFFFFF;
    //public List<SpaceObject> Nearby;
    public string name => "marker";
    public XY position { get; set; }
    public XY velocity { get; set; }
    public bool active { get; set; }

    public ActiveMarker(System world, Sovereign sovereign, XY Position) {
        this.world = world;
        this.sovereign = sovereign;
        this.position = Position;
        this.velocity = new XY();
        this.active = true;
    }
    public void Update(double delta) {
        //Nearby = Owner.world.entities.all.OfType<SpaceObject>().Except(new SpaceObject[] { Owner }).OrderBy(e => (e.position - position).magnitude).ToList();
    }
    public void Damage(Projectile p) { }
    public void Destroy(ActiveObject source = null) { active = false; }
}

class TargetingMarker : ActiveObject {
    [JsonIgnore]
    public ColoredGlyph tile => null;
    [JsonIgnore]
    public System world => Owner.world;
    [JsonIgnore]
    public Sovereign sovereign => Owner.sovereign;
    [JsonIgnore]
    public ulong id => 0xFFFFFFFFFFFFFFFF;

    public PlayerShip Owner;
    //public List<SpaceObject> Nearby;
    public string name { get; set; }
    public XY position { get; set; }
    public XY velocity { get; set; }
    public bool active { get; set; }

    public TargetingMarker(PlayerShip Owner, string Name, XY Position) {
        this.Owner = Owner;
        //this.Nearby = new List<SpaceObject>();
        this.name = Name;
        this.position = Position;
        this.velocity = new XY();
        this.active = true;
    }
    public void Update(double delta) {
        //Nearby = Owner.world.entities.all.OfType<SpaceObject>().Except(new SpaceObject[] { Owner }).OrderBy(e => (e.position - position).magnitude).ToList();
    }
    public void Damage(Projectile p) { }
    public void Destroy(ActiveObject source = null) { }
}
