﻿using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.BaseShip;
using Newtonsoft.Json;

namespace RogueFrontier;

public enum Rotating {
    None, CCW, CW
}

public static class SStation {
    public static bool IsEnemy(this Station owner, ActiveObject target) {
        return (owner != target
            && (owner.sovereign.IsEnemy(target)
            || target.sovereign.IsEnemy(owner.sovereign)))
            && !(target is Wreck);
    }
}
public static class SShip {
    public static bool IsEnemy(this IShip owner, ActiveObject target) {
        return owner.CanTarget(target) && (owner.sovereign.IsEnemy(target) || target.sovereign.IsEnemy(owner.sovereign)) && !(target is Wreck);
    }
    public static bool IsFriendly(this IShip owner, ActiveObject target) {
        return owner.CanTarget(target) && owner.sovereign.IsFriend(target) && !(target is Wreck);
    }
    public static bool CanTarget(this IShip owner, ActiveObject target) {
        return owner != target;
    }
}
public static class SStealth {
    public static bool IsVisible(this Entity e, Entity other) => e switch {
        PlayerShip p => p.CanSee(other),
        _ => e.GetVisibleDistanceLeft(other) > 0
    };
    public static double GetVisibleDistanceLeft(this Entity p, Entity e) =>
        GetVisibleRange(GetStealth(p)) - (e.position - p.position).magnitude;
    public static double GetStealth(this Entity e) => e switch {
        PlayerShip pl => pl.ship.stealth,
        AIShip ai => ai.ship.stealth,
        Station st => st.stealth,
        ISegment s => GetStealth(s.parent),
        _ => 0
    };
    public static double GetVisibleRange(double stealth) => stealth switch {
#if true
        > 0 => 250 / stealth,
#else
        > 0 => double.PositiveInfinity,
#endif
        0 => double.PositiveInfinity,
        < 0 => throw new Exception($"Invalid stealth {stealth}")
    };
    public static double GetVisibleRange2(double stealth) {
        var result = GetVisibleRange(stealth);
        return result * result;
    }
}
public interface IShip : ActiveObject {
    XY position { get; set; }
    XY velocity { get; set; }
    HashSet<Item> cargo { get; }
    DeviceSystem devices { get; }
    ShipClass shipClass { get; }
    bool thrusting { get; }
    double rotationDeg { get; }
    double rotationRad => rotationDeg * Math.PI / 180;
    public double stoppingRotation { get; }
    Docking dock { get; set; }
}
public class BaseShip {
    [JsonIgnore]
    public static BaseShip dead => new(System.empty, ShipClass.empty, XY.Zero) { active = false };
    [JsonIgnore]
    public string name => shipClass.name;
    [JsonIgnore]
    public ColoredGlyph tile => shipClass.tile.Original;

    [JsonIgnore]
    public double stoppingRotation {
        get {
            var stoppingTime = Program.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (shipClass.rotationDecel);
            return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((shipClass.rotationDecel / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }
    }
    [JsonIgnore]
    public double stoppingRotationWithCounterTurn {
        get {
            var stoppingRate = shipClass.rotationDecel + shipClass.rotationAccel;
            var stoppingTime = Math.Abs(Program.TICKS_PER_SECOND * rotatingVel / stoppingRate);
            return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((stoppingRate / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }
    }
    public System world;
    public ShipClass shipClass;
    public XY position;
    public ulong id;
    public XY velocity;
    public bool active;
    public HashSet<Item> cargo;
    public DeviceSystem devices;
    public HullSystem damageSystem;
    public double stealth;
    public int blindTicks;
    public Disrupt disruption;
    public Rand destiny;
    public double rotationDeg;
    public bool thrusting;
    public Rotating rotating;
    public double rotatingVel;
    public bool decelerating;

    public Wreck wreck;
    public BaseShip() { }
    public BaseShip(System world, ShipClass shipClass, XY Position) {
        this.world = world;
        this.id = world.nextId++;
        this.shipClass = shipClass;

        this.position = Position;
        this.velocity = new();

        this.active = true;

        this.cargo = new();
        this.cargo.UnionWith(shipClass.cargo?.Generate(world.types) ?? new List<Item>());
        this.devices = new();
        this.devices.Install(shipClass.devices?.Generate(world.types) ?? new List<Device>());
        this.damageSystem = shipClass.damageDesc.Create(world.types);
        this.destiny = new Rand(world.karma.NextInteger());
    }
    public BaseShip(BaseShip source) {
        this.world = source.world;
        this.shipClass = source.shipClass;
        this.position = source.position;
        this.velocity = source.velocity;
        this.active = true;
        this.cargo = source.cargo;
        this.devices = source.devices;
        this.damageSystem = source.damageSystem;
        this.destiny = source.destiny;
    }
    public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
    public void SetRotating(Rotating rotating = Rotating.None) {
        this.rotating = rotating;
    }
    public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;
    public void ReduceDamage(Projectile p) {
        int dmgFull = p.damageHP;
        ref int dmgLeft = ref p.damageHP;
        foreach (var s in devices.Shield) {
            if (dmgLeft == 0) return;
            s.Absorb(p);
        }
        if (dmgLeft == 0) return;
        if (p.fragment.blind is IDice blind) {
            blindTicks += blind.Roll();
            blindTicks = Math.Min(blindTicks, 300);
        }
        int knockback = p.fragment.knockback * dmgLeft / dmgFull;
        velocity += (p.velocity - velocity).WithMagnitude(knockback);
        disruption = p.fragment.disruptor?.GetHijack() ?? disruption;
    }
    public void Destroy(ActiveObject owner) {
        var items = cargo
            .Concat(devices.Installed.Select(d => d.source).Where(i => i != null))
            .Concat((damageSystem as LayeredArmor)?.layers.Select(l => l.source) ?? new List<Item>());
        wreck = new Wreck(owner, items);
        world.AddEntity(wreck);
        foreach(var angle in Enumerable.Range(0, 16).Select(i => i * 2 * Math.PI / 16)) {
            var blast = new EffectParticle(position + XY.Polar(angle, 1),
                velocity + XY.Polar(angle, 4),
                new ColoredGlyph(Color.Orange, Color.Orange.SetAlpha(128), 'x'),
                60);
            world.AddEffect(blast);
        }

        active = false;
    }
    public void UpdatePhysics(double delta) {
        if(world.tick%15 == 0) {
            stealth = shipClass.stealth;
            devices.Shield.ForEach(s => stealth += s.stealth);
            stealth += (damageSystem as LayeredArmor)?.layers.LastOrDefault(a => a.hp > 0)?.stealth ?? 0;

            var weapons = devices.Weapon;
            if (weapons.Any()) {
                stealth *= 1 - weapons.Max(w => ((double)w.delay / w.desc.fireCooldown));
            }

            stealth = Math.Max(stealth, 0);
        }
        UpdateControl(delta);
        UpdateMotion(delta);
        //Devices.Update(this);
    }
    public void ResetControl() {
        thrusting = false;
        rotating = Rotating.None;
        decelerating = false;
    }
    public void UpdateControl(double delta) {
        if(blindTicks > 0) {
            blindTicks--;
            devices.Weapon.ForEach(w => w.blind = true);
        }
        if (disruption != null) {
            thrusting = disruption.thrustMode ?? thrusting;
            rotating = disruption.turnMode switch {
                true => Rotating.CCW,
                false => Rotating.None,
                _ => rotating
            };

            decelerating = disruption.brakeMode ?? decelerating;
            devices.Weapon.ForEach(a => a.firing = disruption.fireMode ?? a.firing);
            disruption.Update();
            if (disruption.active == false) {
                disruption = null;
            }
        }
        UpdateThrust();
        UpdateTurn();
        UpdateRotation();
        UpdateBrake();
        void UpdateThrust() {
            if (thrusting) {
                var rotationRads = rotationDeg * Math.PI / 180;

                var exhaust = new EffectParticle(position + XY.Polar(rotationRads, -1),
                    velocity + XY.Polar(rotationRads, -shipClass.thrust),
                    new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                    4);
                world.AddEffect(exhaust);

                velocity += XY.Polar(rotationRads, shipClass.thrust * delta * Program.TICKS_PER_SECOND);
                if (velocity.magnitude > shipClass.maxSpeed) {
                    velocity = velocity.normal * shipClass.maxSpeed;
                }
            }
        }
        void UpdateTurn() {
            if (rotating != Rotating.None) {
                if (rotating == Rotating.CCW) {
                    /*
                    if (rotatingSpeed < 0) {
                        rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rotatingVel < 0) {
                        Decel();
                    }
                    rotatingVel += shipClass.rotationAccel * delta;
                } else if (rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rotatingVel > 0) {
                        Decel();
                    }
                    rotatingVel -= shipClass.rotationAccel * delta;
                }
                rotatingVel = Math.Min(Math.Abs(rotatingVel), shipClass.rotationMaxSpeed) * Math.Sign(rotatingVel);
            } else {
                Decel();
            }
            void Decel() => rotatingVel -= Math.Min(Math.Abs(rotatingVel), shipClass.rotationDecel * delta) * Math.Sign(rotatingVel); ;
        }
        void UpdateRotation() => rotationDeg += rotatingVel * delta * Program.TICKS_PER_SECOND;
        void UpdateBrake() {
            if (decelerating) {
                if (velocity.magnitude > 0.05) {
                    velocity -= velocity.normal * Math.Min(velocity.magnitude, shipClass.thrust * (delta * Program.TICKS_PER_SECOND) / 2);
                } else {
                    velocity = new XY();
                }
            }
        }
    }
    public void UpdateMotion(double delta) {
        position += velocity * delta;
    }
}
public static class SShipBehavior {
    public static bool CanTarget(this IShipBehavior behavior, ActiveObject other) {
        switch (behavior) {
            case Wingmate w:
                return w.order.CanTarget(other);
            case IShipOrder o:
                return o.CanTarget(other);
        }
        return false;
    }
    public static IShipOrder GetOrder(this IShipBehavior behavior) {
        switch (behavior) {
            case Wingmate w:
                return w.order;
            case IShipOrder o:
                return o;
            case Sulphin s:
                return s.order;
            case Swift sw:
                return sw.order;
            case Merchant t:
                return null;
            default:
                throw new Exception("Unknown behavior type");
        }
    }
    public static string GetOrderName(this IShipBehavior behavior) =>
        behavior.GetOrder()?.GetType().Name ?? "Unknown";
}
public class AIShip : IShip {
    [JsonIgnore] public ulong id => ship.id;
    [JsonIgnore] public string name => ship.name;
    [JsonIgnore] public System world => ship.world;
    [JsonIgnore] public XY position { get => ship.position; set => ship.position = value; }
    [JsonIgnore] public XY velocity { get => ship.velocity; set => ship.velocity = value; }
    [JsonIgnore] public ShipClass shipClass => ship.shipClass;
    [JsonIgnore] public bool thrusting => ship.thrusting;
    [JsonIgnore] public double rotationDeg => ship.rotationDeg;
    [JsonIgnore] public HashSet<Item> cargo => ship.cargo;
    [JsonIgnore] public DeviceSystem devices => ship.devices;
    [JsonIgnore] public HullSystem damageSystem => ship.damageSystem;
    [JsonIgnore] public Rand destiny => ship.destiny;
    [JsonIgnore] public double stoppingRotation => ship.stoppingRotation;
    [JsonIgnore] public HashSet<Entity> avoidHit => new HashSet<Entity> { dock?.Target, (behavior as GuardOrder)?.home };
    
    public Sovereign sovereign { get; set; }
    private IShipBehavior _behavior;
    public IShipBehavior behavior {
        get => _behavior;
        set {
            _behavior = value;
            _behavior.Init(this);
        }
    }
    public BaseShip ship;
    public Docking dock { get; set; }

    public delegate void WeaponFired(AIShip ship, Weapon w, List<Projectile> p);
    public Ev<WeaponFired> onWeaponFire = new();

    public delegate void Damaged(AIShip ship, Projectile hit);
    public Ev<Damaged> onDamaged = new();

    public delegate void Destroyed(AIShip ship, ActiveObject destroyer, Wreck wreck);
    public Ev<Destroyed> onDestroyed = new();
    public AIShip() { }
    public AIShip(BaseShip ship, Sovereign sovereign, IShipBehavior behavior = null, IShipOrder order = null) {
        this.ship = ship;
        this.sovereign = sovereign;
        this.behavior = behavior ?? ship.shipClass.behavior switch {
            EShipBehavior.sulphin => new Sulphin(this, order),
            EShipBehavior.swift => new Swift(this, order),
            EShipBehavior.none => order,
            _ => order
        };
    }
    public bool IsAble() => damageSystem.GetHP() > damageSystem.GetMaxHP() / 2;
    public override string ToString() => $"{id}, {position.roundDown}, {velocity.roundDown}, {shipClass.codename}, {behavior}";
    public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
    public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
    public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
    public void Damage(Projectile p) {
        onDamaged.ForEach(f => f(this, p));
        ship.ReduceDamage(p);
        ship.damageSystem.Damage(world.tick, p, () => Destroy(p.source));
    }
    public void Destroy(ActiveObject source) {
        if (source is PlayerShip ps) {
            ps.shipsDestroyed.Add(this);
            if (shipClass.crimeOnDestroy) {
                ps.crimeRecord.Add(new DestructionCrime(this));
            }
        }
        ship.Destroy(this);

        onDestroyed.RemoveNull();
        onDestroyed.ForEach(f => f(this, source, ship.wreck));
    }
    public void Update(double delta) {
        ship.ResetControl();
        behavior?.Update(delta, this);
        ship.UpdatePhysics(delta);

        dock?.Update(delta, this);

        if(world.tick%30 == 0 && dock?.Target is Station st) {
            ship.stealth = Math.Max(ship.stealth, st.stealth);
        }
        //We update the ship's devices as ourselves because they need to know who the exact owner is
        //In case someone other than us needs to know who we are through our devices
        ship.devices.Update(delta, this);

        if(ship.damageSystem is LayeredArmor la) {
            la.Update(delta, this);
            /*
            if(world.tick%90 == 0) {
                var repairers = new HashSet<(Item, RepairArmor)>(cargo.Select(i => (item: i, repair:i.type.invoke)).OfType<(Item, RepairArmor)>());
                foreach(var l in la.layers) {
                    var delta = l.desc.maxHP - l.hp;
                    if(delta < l.desc.maxHP/2) {
                        continue;
                    }

                    foreach((Item item, RepairArmor ra) pair in repairers) {
                        if(pair.ra.repairHP > delta * 1.2) {
                            continue;
                        }

                        l.Repair(pair.ra);
                        cargo.Remove(pair.item);
                        repairers.Remove(pair);
                        goto RepairCheckDone;
                    }
                }
            RepairCheckDone:
                ;
            }
            */
        }
    }
    [JsonIgnore]
    public bool active => ship.active;
    [JsonIgnore]
    public ColoredGlyph tile => ship.tile;
}

public class PlayerShip : IShip {
    [JsonIgnore]
    public string name => ship.name;
    [JsonIgnore]
    public ulong id => ship.id;
    [JsonIgnore]
    public System world => ship.world;
    [JsonIgnore]
    public XY position { get => ship.position; set => ship.position = value; }
    [JsonIgnore]
    public XY velocity { get => ship.velocity; set => ship.velocity = value; }
    [JsonIgnore]
    public ShipClass shipClass => ship.shipClass;
    [JsonIgnore]
    public bool thrusting => ship.thrusting;
    [JsonIgnore]
    public double rotationDeg => ship.rotationDeg;
    [JsonIgnore]
    public double rotationRad => ship.rotationDeg * Math.PI / 180;
    [JsonIgnore]
    public double stoppingRotation => ship.stoppingRotation;
    [JsonIgnore]
    public HashSet<Item> cargo => ship.cargo;
    [JsonIgnore]
    public DeviceSystem devices => ship.devices;
    [JsonIgnore]
    public HullSystem hull => ship.damageSystem;

    public Player person;
    public BaseShip ship;
    //public PlayerStory story;
    public Sovereign sovereign { get; set; }
    public EnergySystem energy;
    public List<Power> powers = new();

    [JsonIgnore]
    public HashSet<Entity> avoidHit => new() {
        dock?.Target
    };
    public Docking dock { get; set; }

    public bool targetFriends = false;
    public List<ActiveObject> targetList = new();

    public int targetIndex = -1;

    public bool firingPrimary = false;
    public bool firingSecondary = false;

    public ListIndex<Weapon> primary;
    public ListIndex<Weapon> secondary;

    public int mortalChances = 3;
    public double mortalTime = 0;

    public bool autopilot = false;

    public List<IPlayerMessage> logs = new();
    public List<IPlayerMessage> messages = new();
    public HashSet<Entity> visible = new();
    public HashSet<Station> known = new();
    public HashSet<ActiveObject> missionTargets = new();
    private int ticks = 0;
    public HashSet<IShip> shipsDestroyed = new();
    public HashSet<Station> stationsDestroyed = new();
    public List<ICrime> crimeRecord=new();

    public delegate void Destroyed(PlayerShip playerShip, ActiveObject destroyer, Wreck wreck);
    public Ev<Destroyed> onDestroyed = new();
    public delegate void Damaged(PlayerShip playerShip, Projectile p);
    public Ev<Damaged> onDamaged = new();

    public delegate void WeaponFired(PlayerShip playerShip, Weapon w, List<Projectile> p);
    public Ev<WeaponFired> onWeaponFire = new();


    public List<AIShip> wingmates = new();

    public Dictionary<ulong, double> visibleDistanceLeft=new();
    public Dictionary<ActiveObject, int> tracking = new();

    public PlayerShip() { }
    public PlayerShip(Player person, BaseShip ship, Sovereign sovereign) {
        this.person = person;
        this.ship = ship;
        this.sovereign = sovereign;

        //this.story = new(this);

        energy = new(ship.devices);
        primary = new(ship.devices.Weapon);
        secondary = new(ship.devices.Weapon);
    }
    public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
    public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
    public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
    public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
    public void SetFiringSecondary(bool firingSecondary = true) => this.firingSecondary = firingSecondary;
    public void SetRotatingToFace(double targetRads) {
        var facingRads = ship.stoppingRotationWithCounterTurn * Math.PI / 180;
        var dest = XY.Polar(targetRads);
        var ccw = (XY.Polar(facingRads + 3 * Math.PI / 180) - dest).magnitude;
        var cw = (XY.Polar(facingRads - 3 * Math.PI / 180) - dest).magnitude;
        if (ccw < cw) {
            SetRotating(Rotating.CCW);
        } else if (cw < ccw) {
            SetRotating(Rotating.CW);
        } else {
            if (ship.rotatingVel > 0) {
                SetRotating(Rotating.CW);
            } else {
                SetRotating(Rotating.CCW);
            }
        }
    }
    public void DisengageAutopilot() {
        if (autopilot) {
            autopilot = false;
            AddMessage(new Message($"Autopilot disengaged"));
        }
    }
    public bool CheckGate(out Stargate gate) {
        foreach (var s in world.effects[position]) {
            if ((s is ISegment seg ? seg.parent : s) is Stargate g) {
                gate = g;
                return true;
            }
        }
        gate = null;
        return false;
    }
    public void NextPrimary() => primary.index++;
    public void PrevPrimary() => primary.index--;
    public void NextSecondary() => secondary.index++;
    public void PrevSecondary() => secondary.index--;
    /*
    //No, let the player always choose the closest target to cursor. No iteration.
    public void NextTargetSet(SpaceObject next) {
        var index = targetList.IndexOf(next);
        if(index != -1) {
            if(index <= targetIndex) {
                bool canRefresh = true;

            CheckTarget:
                targetIndex++;
                if (targetIndex < targetList.Count) {
                    var target = targetList[targetIndex];
                    if (!target.Active) {
                        goto CheckTarget;
                    } else if ((target.Position - Position).Magnitude > 100) {
                        goto CheckTarget;
                    } else {
                        //Found target
                        UpdateAutoAim();
                    }
                } else {
                    targetIndex = -1;
                    if (canRefresh) {
                        Refresh();
                        goto CheckTarget;
                    } else {
                        //Could not find target
                    }
                }

                void Refresh() {
                    targetList = World.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - Position).Magnitude).ToList();
                    canRefresh = false;
                }
            }
        } else {

        }
    }
    */
    public void NextTargetEnemy() {
        bool canRefresh = true;
        if (targetFriends) {
            Refresh();
            targetFriends = false;
        }

    CheckTarget:
        targetIndex++;
        if (targetIndex < targetList.Count) {
            var t = targetList[targetIndex];
            if(this.IsEnemy(t) && t.active && (t.position - position).magnitude < 100) {
                UpdateWeaponTargets();
            } else {
                goto CheckTarget;
            }
        } else {
            if (canRefresh) {
                Refresh();
                goto CheckTarget;
            } else {
                targetIndex = -1;
            }
        }

        void Refresh() {
            targetList =
                world.entities.all
                .OfType<ActiveObject>()
                .Where(e => this.IsEnemy(e) && e != this)
                .OrderBy(e => (e.position - position).magnitude)
                .Distinct()
                .ToList();
            targetIndex = -1;
            canRefresh = false;
        }
    }
    public void NextTargetFriendly() {
        bool canRefresh = true;

        if (!targetFriends) {
            Refresh();
            targetFriends = true;
        }

    CheckTarget:
        targetIndex++;
        if (targetIndex < targetList.Count) {
            var t = targetList[targetIndex];
            if (!this.IsEnemy(t) && t.active && (t.position - position).magnitude < 100) {
                UpdateWeaponTargets();
            } else {
                goto CheckTarget;
            }
        } else {
            if (canRefresh) {
                Refresh();
                goto CheckTarget;
            } else {
                targetIndex = -1;
            }
        }

        void Refresh() {
            targetList = world.entities.all
                .OfType<ActiveObject>()
                .Where(e => !this.IsEnemy(e) && e != this)
                .OrderBy(e => (e.position - position).magnitude)
                .Distinct()
                .ToList();
            targetIndex = -1;
            canRefresh = false;
        }
    }
    //Remember to call this before we set the targetIndex == -1
    public void ResetWeaponTargets() {
        var primary = GetPrimary();
        if(primary?.target == targetList[targetIndex]) {
            primary.SetTarget(null);
        }
        var secondary = GetSecondary();
        if(secondary?.target == targetList[targetIndex]) {
            secondary.SetTarget(null);
        }
    }
    //Remember to call this after we set the targetIndex > -1
    public void UpdateWeaponTargets() {
        GetPrimary()?.SetTarget(targetList[targetIndex]);
        GetSecondary()?.SetTarget(targetList[targetIndex]);
    }
    //Stop targeting, but remember our remaining targets
    public void ForgetTarget() {
        if (targetIndex == -1) {
            return;
        }
        ResetWeaponTargets();
        targetList = targetList.GetRange(targetIndex, targetList.Count - targetIndex);
        targetIndex = -1;
    }
    //Stop targeting and clear our target list
    public void ClearTarget() {
        if (targetIndex == -1) {
            return;
        }
        ResetWeaponTargets();
        targetList.Clear();
        targetIndex = -1;
    }

    public void SetTargetList(List<ActiveObject> targetList) {
        if (targetIndex > -1) {
            ResetWeaponTargets();
        }
        this.targetList = targetList;
        if (targetList.Count > 0) {
            targetIndex = 0;
            UpdateWeaponTargets();
        } else {
            targetIndex = -1;

        }
    }
    public bool GetTarget(out ActiveObject target) => (target = GetTarget()) != null;
    public ActiveObject GetTarget() {
        if (targetIndex != -1) {
            var target = targetList[targetIndex];
            if (target.active) {
                return target;
            }
            ForgetTarget();
        }
        return null;
    }
    public Weapon GetPrimary() => primary.item;
    public bool GetPrimary(out Weapon result) => (result = GetPrimary()) != null;
    public Weapon GetSecondary() => secondary.item;
    public bool GetSecondary(out Weapon result) => (result = GetSecondary()) != null;
    public void Damage(Projectile p) {
        int originalHP = ship.damageSystem.GetHP();
        
        //We handle our own damage system
        ship.ReduceDamage(p);
        ship.damageSystem.Damage(world.tick, p, DestroyCheck);

        //Check for saving throws
        void DestroyCheck() {
            powers.ForEach(power => power.OnDestroyCheck(this, p));
            ship.ReduceDamage(p);
            ship.damageSystem.Damage(world.tick, p, () => Destroy(p.source));
        }

        if (!active) {
            goto Done;
        }
        int delta = originalHP - ship.damageSystem.GetHP();
        if (delta > ship.damageSystem.GetHP() / 3) {
            if (mortalTime <= 0) {
                if (mortalChances > 0) {
                    AddMessage(new Message("Escape while you can!"));

                    mortalTime = mortalChances * 3.0 + 1;
                    mortalChances--;
                }
            }
        }
        Done:
        foreach (var f in onDamaged.set) f.Value.Invoke(this, p);
    }
    public void Destroy(ActiveObject destroyer) {
        ship.Destroy(this);
        onDestroyed.RemoveNull();
        onDestroyed.ForEach(f => f(this, destroyer, ship.wreck));
    }
    public bool CanSee(Entity e) => GetVisibleDistanceLeft(e) > 0;
    public double GetVisibleDistanceLeft(Entity e) => visibleDistanceLeft.TryGetValue(e.id, out var d) ? d : double.PositiveInfinity;
    public void Update(double delta) {

        messages.ForEach(m => m.Update(delta));
        messages.RemoveAll(m => !m.Active);

        var target = GetTarget();

        powers.ForEach(p => {
            if (p.cooldownLeft > 0) {
                p.cooldownLeft -= delta * 60;

                if (p.cooldownLeft <= 0) {
                    AddMessage(new Message($"[Power] {p.type.name} is ready"));
                }
            }
        });

        if (firingPrimary) {
            if (primary.Has(out var w) && !energy.off.Contains(w))
                w.SetFiring(true, target);
            firingPrimary = false;
        }

        if (firingSecondary) {
            if (secondary.Has(out var w) && !energy.off.Contains(w))
                w.SetFiring(true, target);
            firingSecondary = false;
        }

        ticks++;

        if(ticks%15 == 0) {
            visibleDistanceLeft.Clear();

            foreach (var e in world.entities.all) {
                visibleDistanceLeft[e.id] = SStealth.GetVisibleDistanceLeft(e, this);
            }
            foreach(var e in tracking.Keys) {
                visibleDistanceLeft[e.id] = 16;
                tracking[e] -= 15;
                if(!e.active || tracking[e] < 1) {
                    tracking.Remove(e);
                }
            }
            /*
            world.entities.all.Select(e => e switch {
                Station st => visibleDistanceLeft[st] = SStealth.GetVisibleRange(st.type.stealth) - (e.position - position).magnitude,
                AIShip ai => visibleDistanceLeft[ai] = SStealth.GetVisibleRange(ai.ship.stealth) - (e.position - position).magnitude,
            });
            */
        }
        if (ticks % 60 == 0) {
            visible = new(world.entities.FilterKey(p => (position - p).maxCoord < 50).Where(CanSee));
            foreach (var s in visible.OfType<Station>().Except(known)) {
                AddMessage(new Transmission(s, $"Discovered: {s.type.name}"));
                known.Add(s);
            }
        }

        dock?.Update(delta, this);

        ship.UpdatePhysics(delta);
        ship.ResetControl();

        //We update the ship's devices as ourselves because they need to know who the exact owner is
        //In case someone other than us needs to know who we are through our devices
        foreach (var enabled in ship.devices.Installed.Except(energy.off)) {
            enabled.Update(delta, this);
        }
        energy.Update(this);
        (ship.damageSystem as LayeredArmor)?.layers.ForEach(l => l.Update(delta, this));
    }
    public void AddMessage(IPlayerMessage message) {
        var existing = messages.FirstOrDefault(m => m.Equals(message));
        if (existing != null) {
            existing.Reset();
        } else {
            messages.Add(message);
            logs.Add(message);
        }
    }
    [JsonIgnore]
    public bool active => ship.active;
    [JsonIgnore]
    public ColoredGlyph tile => ship.tile;

    public string GetMemorial(string epitaph) =>
@$"
{person.name} ({person.Genome.subjective}/{person.Genome.objective})
{person.Genome.name}

{epitaph}

Ship: {shipClass.name}

Armor
{string.Join('\n', (hull as LayeredArmor).layers.Select(l => $"    {l.source.type.name}"))}

Devices
{string.Join('\n', devices.Installed.Select(device => $"    {device.source.type.name}"))}

Cargo
{string.Join('\n', cargo.GroupBy(i => i.type.name).Select(group => $"{group.Count(),4}x {group.Key}"))}

Ships Destroyed
{string.Join('\n', shipsDestroyed.GroupBy(sc => sc.shipClass).Select(pair => $"{pair.Count(),4}x {pair.Key.name,-16}"))}
";
}
