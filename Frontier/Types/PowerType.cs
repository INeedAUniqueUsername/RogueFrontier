using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace RogueFrontier;

public enum Voice {
    Orator,
    Dictator,
    Debater,
    Instigator
}
public record PowerType() : IDesignType {
    [Req] public string codename;
    [Req] public string name;
    [Req] public double cooldownTime;
    [Req] public double invokeDelay;
    [Opt] public bool onDestroyCheck = false;
    [Opt] public string message = null;
    //public HashSet<string> attributes;
    [Opt] public bool scareEnemies;

    [Req] public Voice voice;
    [Opt(parse = false)] public SoundBuffer sound;
    public static Dictionary<Voice, Color> glowColors = new() {
        [Voice.Orator] = new Color(204, 153, 255),
        [Voice.Dictator] = Color.OrangeRed,
        [Voice.Debater] = Color.Yellow,
        [Voice.Instigator] = Color.Green
    };
    public Color glowColor;

    public List<PowerEffect> Effect;
    public void Initialize(TypeCollection collection, XElement e) {
        var parent = e.TryAtt("inherit", out var inherit) ? collection.Lookup<PowerType>(inherit) : null;
        e.Initialize(this, parent, transform: new() {
            [nameof(voice)] = (Voice v) => {
                glowColor = glowColors[v];
                return v;
            },
            [nameof(sound)] = (string s) => new SoundBuffer(s)
        });
        Effect = new(e.Elements().Select(e => (PowerEffect)(e.Name.LocalName switch {
            "Projectile" => new PowerProjectile(e),
            "Heal" => new PowerHeal(),
            "Reveal" => new PowerReveal(),
            "ProjectileBarrier" => new PowerBarrier(e),
            "Jump" => new PowerJump(e),
            "Storm" => new PowerStorm(e),
            "Clonewall" => new Clonewall(e),
            "RechargeWeapon" => new PowerRechargeWeapon(collection, e),
            _ => throw new Exception($"Unknown PowerEffect type: {e.Name.LocalName} ### {e} ### {e.Parent}")
        })));
        if(Effect.Count == 0) {
            throw new Exception($"Power must have effect: {codename} ### {e} ### {e.Parent}");
        }
    }
    public void Invoke(PlayerShip player) => Effect.ForEach(e => e.Invoke(player));
}
public interface PowerEffect {
    void Invoke(PlayerShip player);
}
public record PowerRechargeWeapon() : PowerEffect {
    [Req] public int maxCharges;
    WeaponDesc weaponType;
    public PowerRechargeWeapon(TypeCollection tc, XElement e) : this() {
        e.Initialize(this);
        weaponType = tc.Lookup<ItemType>(e.ExpectAtt("codename")).Weapon
                        ?? throw new Exception();
    }
    public void Invoke(PlayerShip player) {
        if(player.devices.Weapon.FirstOrDefault(w => w.desc == weaponType) is Weapon w) {
            ref int c = ref ((ChargeAmmo)w.ammo).charges;
            c = Math.Max(c, maxCharges);
        }
    }
}
public record PowerJump() : PowerEffect {
    [Opt] public int distance = 100;
    public PowerJump(XElement e) : this() => e.Initialize(this);
    public void Invoke(Mainframe main) {
        main.Jump();
        Invoke(main.playerShip);
    }
    public void Invoke(PlayerShip player) =>
        player.position += XY.Polar(player.rotationRad, distance);
}
public record PowerStorm() : PowerEffect {
    public PowerStorm(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerShip player) =>
        player.world.AddEffect(new StormOverlay(player));
    public class StormOverlay : Effect {
        PlayerShip owner;
        public XY position => owner.position;
        public bool active => owner.active;
        public ColoredGlyph tile => null;
        public StormOverlay(PlayerShip owner) =>
            this.owner = owner;
        public void Update(double delta) {
            var w = owner.GetPrimary();
            if(w != null) {
                var f = w.projectileDesc;
                var p = new Projectile(owner, f,
                    owner.position + XY.Polar(0, 50),
                    owner.velocity + XY.Polar(0, -50),
                    maneuver:f.GetManeuver(w.target));
                owner.world.AddEntity(p);
            }
        }
    }
}
public record Clonewall() : PowerEffect {
    [Opt] public int lifetime = 20;
    public Clonewall(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerShip player) {
        var o = new Overlay(player, lifetime);
        player.world.AddEffect(o);
        player.onWeaponFire += o;
    }
    public class Overlay : Effect, Ob<PlayerShip.WeaponFired> {
        int ticks;
        PlayerShip owner;
        public XY position => owner.position;
        public bool active => owner.active && owner.world.effects.Contains(this) && lifetime > 0;
        public ColoredGlyph tile => null;


        private List<XY> offsets;
        private double[] directions;
        private bool busy = false;
        private double lifetime;
        public Overlay(PlayerShip owner, double lifetime) {
            this.owner = owner;
            this.lifetime = lifetime;
            UpdateOffsets();
            directions = new double[offsets.Count];
        }
        private void UpdateOffsets() {
            XY  up = XY.Polar(owner.rotationRad - Math.PI / 2),
                down = XY.Polar(owner.rotationRad + Math.PI / 2);
            offsets = new() {
                up * 2, down * 2,
                up * 4, down * 4,
                up * 6, down * 6
            };
        }
         public void Observe(PlayerShip.WeaponFired ev){
            var (p, w, pr, _) = ev;
            //Deactivate
            if (!active) {
                p.onWeaponFire -= this;
                return;
            }
            //Don't clone any Power attacks
            if(w.desc == null) {
                return;
            }
            //Don't clone the clones
            if(busy) {
                return;
            }
            busy = true;
            var targets = w.targeting?.GetMultiTarget().ToList();

            foreach(var i in Enumerable.Range(0, offsets.Count)) {
                var o = offsets[i];
                var salvo = w.CreateProjectiles(owner, targets, directions[i], false);
                foreach (var pp in salvo) {
                    pp.position += o;
                    owner.world.AddEntity(pp);
                }
                w.onFire.Observe(new(w, salvo));
                w.ammo?.OnFire();
                w.totalTimesFired++;
            }
            busy = false;
        }
        public void Update(double delta) {
            ticks++;
            lifetime -= delta;
            if(owner.GetPrimary() is Weapon w) {
                const int interval = 5;
                if (ticks % interval != 0) {
                    return;
                }
                UpdateOffsets();
                var points = offsets.Select((o, i) => (owner.position + o, i));
                //If we have target, update the clone facings
                if ((w.target ?? owner.GetTarget()) is ActiveObject t) {
                    var desc = w.projectileDesc;
                    var msp = desc.missileSpeed;
                    var dv = t.velocity - owner.velocity;
                    foreach (var (p, i) in points) {
                        directions[i] = Main.CalcFireAngle(t.position - p, dv, msp, out var _);
                    }
                } else {
                    Array.Fill(directions, owner.rotationRad);
                }
                foreach (var (p, i) in points) {
                    owner.world.AddEffect(new EffectParticle(p, owner.tile, interval + 3));
                    Heading.AimLine(owner.world, p, directions[i], interval + 2);
                }
            }
        }
    }
}
public record PowerProjectile() : PowerEffect {
    public FragmentDesc desc;
    public PowerProjectile(XElement e) : this() {
        desc = new FragmentDesc(e);
    }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) =>
        new Weapon() { projectileDesc = desc}.Fire(player, player.rotationRad);
}
public record PowerHeal() : PowerEffect {
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) {
        player.hull.Restore();
        player.devices.Shield.ForEach(s => s.hp = s.desc.maxHP);
        player.devices.Reactor.ForEach(r => r.energy = r.desc.capacity);
    }
}
public record PowerReveal() : PowerEffect {
    public void Invoke(PlayerShip player) {
        var enemies = player.world.entities.all
            .OfType<ActiveObject>()
            .Where(a => (a.position - player.position).magnitude2 < 360 * 360
                        && a.CanTarget(player)
                        && SStealth.GetStealth(a) > 0);
        foreach (var e in enemies) {
            var time = 1800;
            if(player.tracking.TryGetValue(e, out var t)) {
                time = Math.Max(time, t);
            }
            player.tracking[e] = time;
        }
    }
}
public record PowerBarrier() : PowerEffect {
    public enum BarrierType {
        shield, bubble, bounce, multiplyAttack
    }
    public enum Shape {
        circle, diamond
    }
    public BarrierType barrierType;
    public Shape shape;
    [Req] public int radius;
    [Req] public int lifetime;
    [Opt] public Color? color = null;
    public PowerBarrier(XElement e) : this() {
        e.Initialize(this);
        barrierType = e.ExpectAttEnum<BarrierType>(nameof(barrierType));
        shape = e.TryAttEnum(nameof(shape), Shape.circle);
    }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);

    private delegate ProjectileBarrier ConstructBarrier(XY pos, int lifetime);

    public void Invoke(PlayerShip player) => Invoke((ActiveObject)player);
    public void Invoke(ActiveObject owner) {
        var world = owner.world;
        var end = 2 * Math.PI;
        ConstructBarrier construct = null;
        switch (barrierType) {
            case BarrierType.shield: {
                    var blocked = new HashSet<Projectile>();
                    construct = (position, lifetime) => new ShieldBarrier(owner, position, lifetime, blocked, color);
                    break;
                }
            case BarrierType.bubble: {
                    var blocked = new HashSet<Projectile>();
                    construct = (position, lifetime) => new BubbleBarrier(owner, position, lifetime, blocked);
                    break;
                }
            case BarrierType.multiplyAttack: {
                    var cloned = new HashSet<Projectile>();
                    construct = (position, lifetime) => new CloneBarrier(owner, position, lifetime, cloned);
                    break;
                }
            case BarrierType.bounce: {
                    var reflected = new HashSet<Projectile>();
                    construct = (position, lifetime) => new ReflectBarrier(owner, position, lifetime, reflected);
                    break;
                }
        }
        //HashSet<(int, int)> covered = new();
        switch (shape) {
            case Shape.circle:
                for (double r = radius; r < radius + 2; r++) {
                    double step = 1f / (r * 2);
                    for (double angle = 0; angle < end; angle += step) {
                        var p = XY.Polar(angle, r);
                        /*
                        if(covered.Contains(p)) {
                            continue;
                        }
                        covered.Add(p);
                        */
                        var barrier = construct(p, lifetime);
                        world.AddEntity(barrier);
                    }
                }
                break;
            case Shape.diamond:
                List<int> Steps(params int[] points) {
                    int i = 0;
                    int n = points[i];
                    var result = new List<int>();
                    while(i < points.Length) {
                        var next = points[i];
                        while(n != next) {
                            result.Add(n);
                            n += Math.Sign(next - n);
                        }
                        i++;
                    }
                    return result;
                }

                var points = Enumerable.Zip(Steps(0, radius, -radius, 0), Steps(radius, -radius, radius)).Select(p => new XY(p.First, p.Second));
                foreach(var p in points) {
                    var barrier = construct(p, lifetime);
                    world.AddEntity(barrier);
                }
                break;
        }
    }
}
public interface IPower {
    public double cooldownPeriod { get; }
    public double invokeDelay { get; }
    public bool ready => cooldownLeft <= 0;
    public double cooldownLeft { get; set; }
    public int invokeCharge { get; set; }
    public bool charging { get; set; }
    public List<PowerEffect> Effect { get; }
}
public class Power : IPower {
    [JsonProperty]
    public PowerType type;
    [JsonIgnore]
    public double cooldownPeriod => type.cooldownTime;
    [JsonIgnore]
    public double invokeDelay => type.invokeDelay;
    [JsonIgnore]
    public bool fullyCharged => invokeCharge >= invokeDelay;
    public double cooldownLeft { get; set; }
    [JsonIgnore]
    public bool ready => cooldownLeft == 0;
    public int invokeCharge { get; set; }
    public bool charging { get; set; }
    [JsonIgnore]
    public List<PowerEffect> Effect => type.Effect;

    public record OnInvoked(Power power);
    public Vi<OnInvoked> onInvoked = new();
    public Power(PowerType type) {
        this.type = type;
    }
    public void OnDestroyCheck(PlayerShip player, Projectile p) {
        if (type.onDestroyCheck && ready) {
            cooldownLeft = cooldownPeriod;
            p.damageHP = 0;
            player.ship.damageSystem.Restore();
            type.Effect.ForEach(e=>e.Invoke(player));
            if (type.message != null) {
                player.AddMessage(new Message(type.message));
            }
        }
    }
    public void Update(double delta, ActiveObject owner) {
        if(cooldownLeft > 0) {
            cooldownLeft -= delta * 60;
        }
        if (charging) {
            invokeCharge++;
            if (fullyCharged) {
                Invoke(owner);
            }
        } else {
            invokeCharge--;
        }
    }
    public void Invoke(ActiveObject owner) {
        charging = false;
        if(owner is PlayerShip pl) {
            Effect.ForEach(a => a.Invoke(pl));
        } else {
            Effect.ForEach(e => {
                switch (e) {
                    case PowerBarrier pb:
                        pb.Invoke(owner);
                        break;
                }
            });
        }
        onInvoked.Observe(new(this));
        invokeCharge = 0;
        cooldownLeft = cooldownPeriod;
    }
}
