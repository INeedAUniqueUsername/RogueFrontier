using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public class Cable : Entity {
    Hook parent;
    public ulong id { get; set; }
    public XY position { get; set; }
    public bool active { get; set; } = true;
    public ColoredGlyph tile => new(Color.LightGray, Color.Transparent, (char)249);
    public Cable(Hook parent, XY position) {
        this.parent = parent;
        id = parent.attached.world.nextId++;
        this.position = position;
    }
    public void Update(double delta) {
        active &= parent.active;
    }
}
public class Hook : Entity {
    public StructureObject attached, source;
    List<Cable> segments=new();
    public Hook(StructureObject attached, StructureObject source) {
        this.attached = attached;
        this.source = source;
        id = attached.world.nextId++;
        var offset = attached.position - source.position;
        var distance = (int)offset.magnitude - 1;
        if (distance < 1) {
            active = false;
            return;
        }
        var direction = offset.normal;
        segments.AddRange(Enumerable.Range(1, distance).Select(
            i => new Cable(this, source.position + direction * i)));
        segments.ForEach(attached.world.AddEntity);
    }
    public ulong id { get; set; }
    public XY position => attached.position - offset.normal;
    public XY offset => attached.position - source.position;
    public bool active { get; set; } = true;
    public ColoredGlyph tile => new(Color.LightGray, Color.Transparent, '?');
    public void Update(double delta) {
        active &= attached.active && source.active;
        var offset = attached.position - source.position;
        segments.ForEach(attached.world.AddEntity);
        var length = segments.Count;
        for (int i = 0; i < length; i++) {
            segments[i].position = source.position + offset * (i+1) / (length+1);
        }
        var nextLength = (int)offset.magnitude;
        if (nextLength >= length) {
            var inc = nextLength - length;
            var direction = offset.normal;
            source.velocity += direction * (inc + 1) * delta;
            attached.velocity -= direction * (inc + 1) * delta;
            if(inc > 8) {
                active = false;
            }
        } else {
            nextLength = Math.Max(nextLength, 5);
            var dec = length - nextLength;
            if (dec > 0) {
                segments.GetRange(length - dec, dec).ForEach(s => s.active = false);
                segments.RemoveRange(length - dec, dec);
            }
        }
    }
}
public class LightningRod : Entity, Ob<Weapon.OnFire>, Ob<Projectile.OnHitActive> {
    public ActiveObject target;
    public Weapon source;
    public int lifetime;
    public int salvoIndex;
    public LightningRod(ActiveObject target, Weapon source, Projectile proj) {
        this.id = target.world.nextId++;

        this.target = target;
        this.source = source;
        this.lifetime = 60;
        this.salvoIndex = proj.salvo.IndexOf(proj);
        source.onFire += this;
    }
    public void Observe(Weapon.OnFire o) {
        var (weapon, projectiles) = o;
        if (!active) {
            weapon.onFire -= this;
            return;
        }
        if (weapon.targeting?.HasTarget(target) != true) {
            return;
        }
        if (weapon.blind) {
            return;
        }
        weapon.delay = 5;
        var p = projectiles[salvoIndex];

        if (!p.active) { return; }
        var source = p.source;
        var direction = Main.CalcFireAngle(target.position - p.position, target.velocity - source.velocity, 300, out var timeToHit);
        p.velocity = source.velocity + XY.Polar(direction, 300);
        p.onHitActive -= weapon;
        p.onHitActive += this;
        //target.Damage(p);
        //p.lifetime = 0;
    }
    public void Observe(Projectile.OnHitActive ev) {
        (var p, var hit) = ev;
        if(hit != target) {
            return;
        }
        if (!active) {
            return;
        }
        if (p.hitHull) {
            lifetime = 90;
        }
    }
    public ulong id { get; set; }
    public XY position => target.position;
    public bool active => target.active && lifetime>0;
    public ColoredGlyph tile => null;

    double timePassed;
    public void Update(double delta) {
        if(target.world.tick%10 == 0) {
            var r = () => target.world.karma.NextDouble();
            target.world.AddEffect(new EffectParticle(
                target.position, target.velocity + XY.Polar(r()*Math.PI*2, 4),
                new ColoredGlyph(Color.Red, Color.Transparent, '%'), 30));
        }
        lifetime--;
    }
}
public class StickyBomb : Entity {
    public StructureObject attached;
    public ActiveObject source;
    public FragmentDesc detonate;
    public StickyBomb(StructureObject attached, ActiveObject source, FragmentDesc detonate) {
        this.attached = attached;
        this.source = source;
        this.detonate = detonate;
        id = attached.world.nextId++;
    }
    public ulong id { get; set; }
    public XY position => attached.position - offset.normal;
    public XY offset => attached.position - source.position;
    public bool active { get; set; } = true;
    public ColoredGlyph tile => new(Color.SpringGreen, Color.Black, 'b');
    public void Update(double delta) {
    }
}