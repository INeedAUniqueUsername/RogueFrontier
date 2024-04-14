using Common;
using SadConsole;
using SadRogue.Primitives;
using System.Collections.Generic;

namespace RogueFrontier;

//Surrounds the playership, reflects their projectiles back so that they bounce around
class ReflectBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public ColoredGlyph tile => new ColoredGlyph(Color.Goldenrod, Color.Black, '*');
    public ulong id { get; private set; }
    public ActiveObject owner;
    public XY offset;
    public double lifetime;
    public HashSet<Projectile> reflected;
    public XY position { get; set; }
    public ReflectBarrier(ActiveObject owner, XY offset, int lifetime, HashSet<Projectile> reflected) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.reflected = reflected;
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
        if (other.source == owner) {
            return;
        }
        if (reflected.Contains(other)) {
            return;
        }
        reflected.Add(other);
        if (other.maneuver?.target != null) {
            other.maneuver.target = other.source;
        }
        other.source = null;
        other.velocity = new XY() - other.velocity;

    }
}
