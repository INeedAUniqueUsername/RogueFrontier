using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using static IslandHopper.ItemType;
using static IslandHopper.ItemType.GunDesc;

namespace IslandHopper;

public interface IItem : Entity, Damageable {
    void Destroy();
    Head Head { get; set; }
    Grenade Grenade { get; set; }
    Gun Gun { get; set; }

    ItemType Type { get; }
    public ColoredString GetApparentName(Player p);

}
public interface ItemComponent {
    void UpdateRealtime();
    void UpdateStep();
    void Modify(ref ColoredString Name);
}
public interface Usable {
    bool CanUse();
    void Use();
}
public class Grenade : ItemComponent {
    public IItem item;
    public GrenadeType type;
    public bool Armed;
    public int Countdown;
    public Grenade(IItem item) {
        this.item = item;
        this.Armed = false;
    }
    public Grenade(IItem item, GrenadeType type) : this(item) {
        this.type = type;
        this.Countdown = type.fuseTime;
    }
    public void Arm(bool Armed = true) {
        this.Armed = Armed;
    }
    public void Detonate() {
        item.Destroy();
        item.World.AddEffect(new ExplosionSource(item.World, item.Position, 6));
        foreach (var offset in Main.GetWithin(type.explosionRadius)) {
            var pos = item.Position + offset;
            var displacement = pos - item.Position;
            var dist2 = displacement.Magnitude2;
            var radius2 = type.explosionRadius * type.explosionRadius;
            if (dist2 > radius2) {
                continue;
            }
            foreach (var hit in item.World.entities[pos]) {
                if (hit is ICharacter d && hit != item) {
                    var multiplier = (radius2 - displacement.Magnitude2) / radius2;
                    ExplosionDamage damage = new() {
                        damage = (int)(type.explosionDamage * multiplier),
                        knockback = displacement.Normal * type.explosionForce * multiplier
                    };
                    d.OnDamaged(damage);
                }
            }
        }
    }
    public void UpdateRealtime() { }
    public void UpdateStep() {
        if (Armed) {
            if (Countdown > 0) {
                Countdown--;
            } else {
                Detonate();
            }
        }
    }
    public void Modify(ref ColoredString Name) {
        if (Armed) {
            Name = new ColoredString("[Armed] ", Color.Red, Color.Black) + Name;
        }
    }
}
public class ExplosionDamage : Damager {
    public int damage;
    public XYZ knockback;
}
public class Gun : ItemComponent {
    public enum State {
        NeedsAmmo,
        NeedsReload,
        Reloading,
        Firing,
        Ready,
    }

    public int range => desc.projectile.range;
    public IItem item;
    public GunDesc desc;
    public int ReloadTimeLeft;
    public int FireTimeLeft;

    public int TimeSinceLastFire;

    public int ClipLeft;
    public int AmmoLeft;

    public Gun() { }
    public Gun(IItem item, GunDesc desc) {
        this.item = item;
        this.desc = desc;
        AmmoLeft = desc.initialAmmo;
        ClipLeft = desc.initialClip;
        FireTimeLeft = 0;
        ReloadTimeLeft = 0;
    }

    public void OnHit(Damager b, Damageable d) {

    }
    public void Reload() {
        if (desc.reloadTime > 0) {
            ReloadTimeLeft = desc.reloadTime;
        } else {
            OnReload();
        }
    }
    private void OnReload() {
        int reloaded = Math.Min(desc.clipSize - ClipLeft, AmmoLeft);
        ClipLeft += reloaded;
        AmmoLeft -= reloaded;
    }
    public State GetState() {
        if (AmmoLeft == 0 && ClipLeft == 0) {
            return State.NeedsAmmo;
        } else if (ReloadTimeLeft > 0) {
            return State.Reloading;
        } else if (ClipLeft == 0) {
            return State.NeedsReload;
        } else if (FireTimeLeft > 0) {
            return State.Firing;
        } else {
            return State.Ready;
        }
    }
    public void UpdateRealtime() { }
    public void UpdateStep() {
        if (ReloadTimeLeft > 0) {
            if (--ReloadTimeLeft == 0) {
                OnReload();
            }
        }
        if (FireTimeLeft > 0) {
            FireTimeLeft--;
        }
        TimeSinceLastFire++;
        return;
    }
    public void Fire(Entity user, IItem item, Entity target, XYZ targetPos) {

        switch (desc.projectile) {
            case BulletDesc bt: {
                    Bullet b = null;
                    for (int i = 0; i < desc.projectileCount; i++) {
                        var bulletSpeed = bt.speed;
                        var bulletVel = (targetPos - user.Position).Normal * bulletSpeed;
                        var spreadAngle = desc.spread * Math.PI / 180;
                        bulletVel = bulletVel.RotateZ(user.World.karma.NextDouble() * spreadAngle - spreadAngle / 2);
                        int damage = bt.damage;
                        if (ClipLeft == 0 && desc.critOnLastShot) {
                            damage *= 3;
                        }
                        b = new Bullet(user, item, target, bulletVel, damage);
                        user.World.AddEntity(b);
                    }
                    if (user is Player p) {
                        p.Watch.Add(b);
                        p.frameCounter = Math.Max(p.frameCounter, 30);
                    }
                    user.World.AddEffect(new Reticle(() => b.Active, targetPos, Color.Red));
                    user.Witness(new InfoEvent(user.Name + new ColoredString(" fires ") + item.Name.WithBackground(Color.Black) + (target != null ? (new ColoredString(" at ") + target.Name.WithBackground(Color.Black)) : new ColoredString(""))));
                    break;
                }
            case FlameDesc ft: {
                    for (int i = 0; i < desc.projectileCount; i++) {
                        var flameSpeed = ft.speed;
                        var direction = (targetPos - user.Position).Normal;
                        XYZ flameVel =
                            user.Velocity / 30 +
                                direction * (flameSpeed + user.World.karma.NextDouble() * 1) +
                                direction.RotateZ(user.World.karma.NextDouble() * 2 * Math.PI) * flameSpeed / 8;
                        //+ direction.RotateZ(user.World.karma.NextDouble() * Math.PI - Math.PI / 2) * flameSpeed / 4);
                        var lifetime = user.World.karma.NextInteger(ft.lifetime, ft.lifetime * 2);
                        var flame = new Flame(user, item, user.Position + direction * 1.5, flameVel, lifetime);
                        user.World.AddEntity(flame);
                    }

                    if (user is Player p) {
                        p.frameCounter = Math.Max(p.frameCounter, 20);
                    }
                    if (TimeSinceLastFire > desc.fireTime * 2) {
                        user.Witness(new InfoEvent(user.Name + new ColoredString(" fires ") + item.Name.WithBackground(Color.Black) + (target != null ? (new ColoredString(" at ") + target.Name.WithBackground(Color.Black)) : new ColoredString(""))));
                    }
                    break;
                }
            case GrenadeDesc gd: {
                    LaunchedGrenade g = null;
                    for (int i = 0; i < desc.projectileCount; i++) {
                        var grenadeSpeed = gd.speed;
                        var grenadeVel = (targetPos - user.Position).Normal * grenadeSpeed;

                        if (ClipLeft == 0 && desc.critOnLastShot) {
                            //
                        }
                        var direction = (targetPos - user.Position).Normal;
                        g = new LaunchedGrenade(item.World, user, gd.grenadeType) {
                            Position = user.Position + direction * 1.5,
                            Velocity = grenadeVel
                        };
                        user.World.AddEntity(g);
                    }
                    if (user is Player p) {
                        p.Watch.Add(g);
                        p.frameCounter = Math.Max(p.frameCounter, 30);
                    }
                    user.World.AddEffect(new Reticle(() => g.Active, targetPos, Color.Red));
                    user.Witness(new InfoEvent(user.Name + new ColoredString(" fires ") + item.Name.WithBackground(Color.Black) + (target != null ? (new ColoredString(" at ") + target.Name.WithBackground(Color.Black)) : new ColoredString(""))));
                    break;

                }
            case null:
                throw new Exception("Projectile desc does not exist. Check during type generation.");
        }
        TimeSinceLastFire = 0;
        //Decrement ClipLeft last so that it doesn't affect the name display
        ClipLeft--;
        FireTimeLeft = desc.fireTime;
    }
    public void Modify(ref ColoredString Name) {
        Name = new ColoredString($"[{ClipLeft} / {AmmoLeft}] ", Color.Yellow, Color.Black) + Name;
    }
    /*
    public Bullet CreateShot(Entity Source, Entity Target, XYZ Velocity) {
        return new Bullet(Source, Target, Velocity);
    }
    */
}
public class ParticleSystem : ItemComponent {

    public void Modify(ref ColoredString Name) { }

    public void UpdateRealtime() {

    }

    public void UpdateStep() { }

    public class FlickerParticle : Effect {
        public char c => symbol.GlyphCharacter;
        public int r => symbol.Foreground.R;
        public int g => symbol.Foreground.G;
        public int b => symbol.Foreground.B;
        public ColoredGlyph SymbolCenter => new(new Color(r, g, b, (int)(255 * (lifetime > 5 ? 1 : (lifetime + 5) / 10f))), Color.Black, c);
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public double lifetime;
        ColoredGlyph[] symbols;
        public double symbolInterval;
        public ColoredGlyph symbol;
        public bool Active => lifetime > 0;
        public FlickerParticle(XYZ Position, XYZ Velocity, double lifetime, params ColoredGlyph[] symbols) {
            this.Position = Position;
            this.Velocity = Velocity;
            this.lifetime = lifetime;
            this.symbols = symbols;
            this.symbolInterval = 0.5;
            this.symbol = symbols[0];
        }
        public void UpdateRealtime(TimeSpan delta) {
            lifetime -= delta.TotalSeconds;
            symbol = symbols[(int)(lifetime / symbolInterval) % symbols.Length];
        }
        public void UpdateStep() {
        }
    }
}

public class Head : ItemComponent {
    public void Modify(ref ColoredString Name) { }
    public void UpdateRealtime() { }
    public void UpdateStep() { }
    public IItem source;
    public HeadDesc desc;
    public int durability;
    public Head(IItem source, HeadDesc desc) {
        this.source = source;
        this.desc = desc;
        this.durability = desc.durability;
    }

}
public class Item : IItem {
    public Island World { get; set; }
    public XYZ Position { get; set; }
    public XYZ Velocity { get; set; }

    public ColoredGlyph SymbolCenter { get; set; } = new ColoredGlyph(Color.White, Color.Black, 'r');
    public ColoredString ModifierName {
        get {
            ColoredString result = BaseName;
            ItemComponent[] components = {
                    Grenade, Gun
                };
            foreach (var component in components) {
                component?.Modify(ref result);
            }
            return result;
        }
    }
    public ColoredString BaseName => new(Type.name, Color.White, Color.Black);
    public ColoredString Name => ModifierName;


    public ItemType Type { get; set; }
    public Head Head { get; set; }
    public Grenade Grenade { get; set; }
    public Gun Gun { get; set; }

    public bool Active { get; private set; } = true;

    public Item(ItemType type) {
        this.Type = type;
        Velocity = new XYZ();
        Head = type.head == null ? null : new Head(this, type.head);
        Gun = type.gun == null ? null : new Gun(this, type.gun);
        Grenade = type.grenade == null ? null : new Grenade(this, type.grenade);
    }

    public Item(ItemType type, Island world, XYZ position) : this(type) {
        this.World = world;
        this.Position = position;
    }
    public void OnRemoved() { }

    public ColoredString GetApparentName(Player p) {
        if (p.known.Contains(Type)) {
            return Name;
        } else {
            return new ColoredString(Type.unknownType.name);
        }
    }

    public void UpdateRealtime(TimeSpan delta) {
        Grenade?.UpdateRealtime();
        Gun?.UpdateRealtime();
    }
    public void UpdateStep() {
        //Somehow this prevents the player from moving when held
        //It's because the Velocity of this item is a reference to the player's velocity
        this.UpdateGravity();
        this.UpdateMotion();
        Grenade?.UpdateStep();
        Gun?.UpdateStep();
    }
    public void Destroy() {
        Active = false;
    }

    public void OnDamaged(Damager source) {
        if (source is Bullet b) {
            Velocity += b.Velocity.Normal * b.knockback;
        }

        //Flame / explosion should cause grenade to explode
    }
}
public class Parachute : Entity, Damageable {
    public Entity user { get; private set; }
    public bool Active { get; private set; }
    public void OnRemoved() { }
    public Island World => user.World;
    public XYZ Position { get; set; }
    public XYZ Velocity { get; set; }

    public int durability = 50;
    public Parachute(Entity user) {
        this.user = user;
        UpdateFromUser();
        Active = true;
    }

    public void UpdateRealtime(TimeSpan delta) {
    }
    public void UpdateFromUser() {
        Position = user.Position + new XYZ(0, 0, 1);
        Velocity = user.Velocity;
    }
    public void UpdateStep() {
        Debug.Print(nameof(UpdateStep));
        //This actually ends up pulling the player upward when they are moving really fast
        //Also boosts jumps
        /*
        UpdateFromUser();
        XYZ down = user.Position - Position;
        double speed = down * user.Velocity.Magnitude;
        if (speed > 3.8 / 30) {
            double deceleration = speed * 0.4;
            user.Velocity -= down * deceleration;
        }
        */

        UpdateFromUser();
        var vel = user.Velocity;
        var speed = vel.Magnitude;
        var terminal = 9.8 / 30;
        if (speed > terminal) {
            double deceleration = speed / 30;
            user.Velocity -= vel.Normal * deceleration;
        }

    }
    public void OnDamaged(Damager source) {
        if (source is Bullet b) {
            durability -= b.damage;
            user.Witness(new InfoEvent(b.Name + new ColoredString(" damages ") + Name));
        }
        if (durability < 1) {
            Active = false;
            user.Witness(new InfoEvent(Name + new ColoredString(" is destroyed!")));
        }
    }
    public readonly ColoredGlyph symbol = new ColoredString("*", Color.White, Color.Transparent)[0];
    public ColoredGlyph SymbolCenter => symbol;
    public ColoredString Name => new("Parachute", Color.White, Color.Black);
}
