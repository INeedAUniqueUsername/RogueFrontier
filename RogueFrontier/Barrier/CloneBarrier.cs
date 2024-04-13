using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
namespace RogueFrontier;
//Surrounds the playership, any projectile that hits this barrier accelerates to extreme speed
class CloneBarrier : ProjectileBarrier {
    [JsonIgnore]
    public bool active => lifetime > 0;
    [JsonIgnore]
    public ColoredGlyph tile => new ColoredGlyph(Color.OrangeRed, Color.Black, '*');
    public ulong id { get; private set; }
    public ActiveObject owner;
    public XY offset;
    public double lifetime;
    public HashSet<Projectile> cloned;
    public XY position { get; set; }
    public CloneBarrier() { }
    public CloneBarrier(ActiveObject owner, XY offset, int lifetime, HashSet<Projectile> cloned) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.cloned = cloned;
        UpdatePosition();
    }
    public void Update(double delta) {
        if (owner.active) {
            lifetime -= delta * 60;
            UpdatePosition();
        } else {
            lifetime = 0;
        }
    }
    public void UpdatePosition() {
        this.position = owner.position + offset;
    }
    public void Interact(Projectile other) {
        if (other.source != owner) {
            return;
        }
        if (cloned.Contains(other)) {
            return;
        }
        cloned.Add(other);

        //other.velocity = other.velocity.WithMagnitude(400);
        var world = owner.world;

        Clone(offset.angleRad + Math.PI / 8);
        Clone(offset.angleRad - Math.PI / 8);
        Clone(offset.angleRad + Math.PI * 2 / 8);
        Clone(offset.angleRad - Math.PI * 2 / 8);
        Clone(offset.angleRad + Math.PI * 3 / 8);
        Clone(offset.angleRad - Math.PI * 3 / 8);

        /*
        for(double angle = offset.angleRad - Math.PI / 2; angle = offset.angleRad + Math.PI / 2; angle++) {

        }
        */


        void Clone(double angle) {
            var velocity = other.velocity + XY.Polar(angle, other.velocity.magnitude / 2);
            var p = new Projectile(other.source, other.desc, other.position, velocity, angle, other.maneuver);
            cloned.Add(p);
            world.AddEntity(p);
        }
        return;
    }
}
