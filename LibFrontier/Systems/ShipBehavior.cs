using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Helper = Common.Main;
using static RogueFrontier.SShipBehavior;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using static RogueFrontier.AttackTarget;

namespace RogueFrontier;
public interface IShipBehavior {
    public void Init(AIShip owner) { }
    void Update(double delta, AIShip owner);
}
public class Sulphin : IShipBehavior, Ob<Station.Destroyed> {
    public void Observe(Station.Destroyed ev) {
        var (s, d, w) = ev;
        s.onDestroyed -= this;
        stationsLost++;
    }
    private int stationsLost;
    public int ticks = 0;
    public HashSet<PlayerShip> playersMet = new();
    public IShipOrder order;
    private HashSet<StationType> stationTypes;
    public Sulphin() { }
    public Sulphin(AIShip ai, IShipOrder order) {
        this.order = order;

        Func<string, StationType> f = ai.world.types.Lookup<StationType>;
        stationTypes = new() { f("station_orion_warlords_camp"), f("station_orion_warlords_stronghold") };
    }
    public void Update(double delta, AIShip owner) {
        order?.Update(delta, owner);
        if(ticks == 0) {
            foreach(var s in owner.world.universe.GetAllEntities().OfType<Station>().Where(s => stationTypes.Contains(s.type))) {
                s.onDestroyed += this;
            }
        }
        if (ticks % 150 == 0) {
            var players = owner.world.entities.all
                .OfType<PlayerShip>()
                .Where(p => (p.position - owner.position).magnitude < 80);
            if (players.Any()) {
                if (stationsLost > 5
                    && !(order is GuardAt g && g.home.world != owner.world)
                    && GetNextStation() is Station next) {
                    var gg = new GuardAt(next);
                    gg.SetAttack(players.GetRandom(owner.world.karma), 600);
                    
                    order = gg;
                    Announce(@"""Ack! Get away from me!""");
                }
                playersMet.UnionWith(players);

                void Announce(string s) {
                    foreach (var p in players) {
                        p.AddMessage(new Transmission(owner, new ColoredString(
                            s, Color.Yellow, Color.Black
                        )));
                    }
                }

                Station GetNextStation() =>
                    owner.world.entities.all
                    .OfType<Stargate>()
                    .Select(gate => gate.destWorld)
                    .Where(w => w != null)
                    .SelectMany(w => w.entities.all.OfType<Station>().Where(s => s.type.codename == "station_orion_warlords_camp"))
                    .GetRandomOrDefault(owner.world.karma);
            }
        }
        ticks++;
    }
}
public class Swift : IShipBehavior, Ob<AIShip.Destroyed> {
    public void Observe(AIShip.Destroyed ev) {
        var (s, d, w) = ev;
        s.onDestroyed -= this;
        if (d is PlayerShip pl) {
            frigatesLost++;
            if (frigatesLost >= 4) {
                order = new AttackTarget(pl);
            }
        }
    }

    private int frigatesLost;

    public int ticks = 0;
    public HashSet<PlayerShip> playersMet = new();
    public IShipOrder order;

    private ShipClass frigateType;
    public Swift() { }
    public Swift(AIShip ai, IShipOrder order) {
        this.order = order;

        Func<string, ShipClass> f = ai.world.types.Lookup<ShipClass>;
        frigateType = f("ship_iron_destroyer");
    }
    public void Update(double delta, AIShip owner) {
        order?.Update(delta, owner);
        if (ticks == 0) {
            foreach (var s in owner.world.universe.GetAllEntities().OfType<AIShip>().Where(s => s.shipClass == frigateType)) {
                s.onDestroyed += this;
            }
        }
        ticks++;
    }
}

public class Merchant : IShipBehavior, Ob<Station.Destroyed> {
    public GateThrough gateOrder;
    public Station target;
    public int timer;
    public void Observe(Station.Destroyed ev) {
        var (s, d, w) = ev;
        s.onDestroyed -= this;
        if (s == target) {
            target = null;
        }
    }
    public Merchant() {}
    public void Update(double delta, AIShip owner) {
        if (gateOrder != null) {
            var current = gateOrder.gate.world;
            if (current == owner.world && owner.world != target.world) {
                gateOrder.Update(delta, owner);
                return;
            } else {
                gateOrder = null;
            }
        }
        if(target == null) {
            target = owner.world.entities.all.OfType<Station>()
                .Where(s => !s.CanTarget(owner)).GetRandomOrDefault(owner.world.karma);
            if (target == null) {
                return;
            }
            target.onDestroyed += this;
        }
        if (owner.world != target.world) {
            gateOrder = new(owner.world.FindGateTo(target.world));
            gateOrder.Update(delta, owner);
            return;
        }
        if (owner.dock.Target != target) {
            if((owner.position - target.position).magnitude2 > 8*8) {
                new Approach(target).Update(delta, owner);
            } else {
                owner.dock.SetTarget(target);
                timer = 60*30 + 60 * owner.world.karma.NextInteger(90);
            }
        } else if(timer > 0) {
            timer--;
        } else {
            target.onDestroyed -= this;
            owner.dock.Clear();

            target = owner.world.entities.all.OfType<Station>()
                .Where(s => s != target && !s.CanTarget(owner)).GetRandomOrDefault(owner.world.karma)
                ?? owner.world.universe.GetAllEntities().OfType<Station>()
                .Where(s => !s.CanTarget(owner)).GetRandomOrDefault(owner.world.karma)
                //?? throw new Exception("Cannot find friendly station");
            ;
        }
    }
}
public interface IShipOrder : IShipBehavior{
    bool Active { get; }
    //void Update(AIShip owner);
    public bool CanTarget(ActiveObject other) => false;
    public delegate IShipOrder Create(ActiveObject target);
}
public interface IDestroyedListener : Ob<Station.Destroyed>, Ob<AIShip.Destroyed>, Ob<PlayerShip.Destroyed> {

    void Ob<Station.Destroyed>.Observe(Station.Destroyed ev) => Observe(new Destroyed(ev.station, ev.destroyer));
    void Ob<AIShip.Destroyed>.Observe(AIShip.Destroyed ev) => Observe(new Destroyed(ev.ship, ev.destroyer));
    void Ob<PlayerShip.Destroyed>.Observe(PlayerShip.Destroyed ev) => Observe(new Destroyed(ev.playerShip, ev.destroyer));
    public record Destroyed(ActiveObject destroyed, ActiveObject destroyer);
    void Observe(Destroyed ev);

    public void Register(ActiveObject target) {
        switch (target) {
            case Station st: st.onDestroyed += this; break;
            case AIShip ai: ai.onDestroyed += this; break;
            case PlayerShip ps: ps.onDestroyed += this; break;
        }
    }
}
public interface IDamagedListener : Ob<Station.Damaged>, Ob<AIShip.Damaged>, Ob<PlayerShip.Damaged> {
    void Ob<Station.Damaged>.Observe(Station.Damaged ev) => Observe(new(ev.station, ev.p));
    void Ob<AIShip.Damaged>.Observe(AIShip.Damaged ev) => Observe(new(ev.ship, ev.hit));
    void Ob<PlayerShip.Damaged>.Observe(PlayerShip.Damaged ev) => Observe(new(ev.playerShip, ev.p));
    public record Damaged(ActiveObject a, Projectile p);
    public void Observe(Damaged ev);

    public void Register(ActiveObject target) {
        switch (target) {
            case Station st: st.onDamaged += this; break;
            case AIShip ai: ai.onDamaged += this; break;
            case PlayerShip ps: ps.onDamaged += this; break;
        }
    }
}
public interface IWeaponListener : Ob<PlayerShip.WeaponFired>, Ob<AIShip.WeaponFired>, Ob<Station.WeaponFired> {

    void Ob<Station.WeaponFired>.Observe(Station.WeaponFired ev) => Observe(new(ev.station, ev.w, ev.p));
    void Ob<AIShip.WeaponFired>.Observe(AIShip.WeaponFired ev) => Observe(new (ev.ship, ev.w, ev.p));
    void Ob<PlayerShip.WeaponFired>.Observe(PlayerShip.WeaponFired ev) => Observe(new (ev.playerShip, ev.w, ev.p, ev.sound));

    public record WeaponFired(ActiveObject source, Weapon w, List<Projectile> proj, bool sound = true);
    public void Observe(WeaponFired ev);

    public void Register(ActiveObject target) {
        switch (target) {
            case Station st: st.onWeaponFire += this; break;
            case AIShip ai: ai.onWeaponFire += this; break;
            case PlayerShip ps: ps.onWeaponFire += this; break;
        }
    }
}
public record OrderOnDestroy(AIShip ship, IShipOrder current, IShipOrder next) : IDestroyedListener {
    public static void Register(AIShip ship, IShipOrder current, IShipOrder next, ActiveObject target) {
        ((IDestroyedListener)new OrderOnDestroy(ship, current, next)).Register(target);
    }
    public bool active => ship.behavior == current;
    public void Observe(IDestroyedListener.Destroyed ev) {
        if (active) {
            ship.behavior = next;
        }
    }
}
public class CompoundOrder : IShipOrder {
    public List<IShipOrder> orders=new();
    public IShipOrder current => orders.FirstOrDefault();

    public delegate void OnOrderCompleted(IShipOrder order);
    public Ev<OnOrderCompleted> onOrderCompleted=new();
    public CompoundOrder() { }
    public CompoundOrder(params IShipOrder[] orders) {
        this.orders.AddRange(orders);
    }
    public void Update(double delta, AIShip owner) {
    Start:
        var first = orders.FirstOrDefault();
        switch (first?.Active) {
            case true:
                first.Update(delta, owner);
                return;
            case false:
                onOrderCompleted.ForEach(f => f(first));
                orders.RemoveAt(0);
                goto Start;
            default:
                return;
        }
    }
    public bool Active => orders.Any();
}
public class EscortShip : IShipOrder {
    [JsonProperty]
    private AttackTarget attack;
    [JsonProperty]
    private FollowShipAtAngle follow;
    int ticks = 0;
    public EscortShip() { }
    public EscortShip(IShip target, XY offset) {
        this.attack = new(null);
        this.follow = new(target, offset);
    }
    public override string ToString() => $"escort {follow.target.name} {(attack?.Active == true ? $"(attack {attack.target.name})" : "")}";
    public bool CanTarget(ActiveObject other) => other == attack?.target;
    public void Update(double delta, AIShip owner) {
        ticks++;
        if(attack.Active == true) {
            attack.Update(delta, owner);
            return;
        }
        if (ticks % 30 == 0) {
            var attacker = owner.world.entities.all
                .OfType<IShip>()
                .FirstOrDefault(s => (s.position - owner.position).magnitude < 100 && s switch {
                    AIShip { behavior: { }be } => be.CanTarget(follow.target) || be.CanTarget(owner),
                    PlayerShip => s.sovereign.IsEnemy(follow.target.sovereign),
                    _ => false
                });
            if (attacker != null) {
                attack.SetTarget(attacker);
                attack.Update(delta, owner);
                return;
            }
        }
        follow.Update(delta, owner);
    }
    public bool Active => follow.Active;
}
public class FollowShipAtAngle : IShipOrder {
    public XY baseOffset;
    public IShip target => approach.target;
    [JsonProperty]
    private FollowShip approach;
    public FollowShipAtAngle(IShip target, XY offset) {
        this.baseOffset = offset;
        this.approach = new(target, offset);
    }
    public void Update(double delta, AIShip owner) {
        approach.offset = baseOffset.Rotate(target.stoppingRotation * Math.PI / 180);
#if DEBUG
        //Heading.Crosshair(owner.world, target.position + offset);
#endif
        approach.Update(delta, owner);
    }
    public bool Active => approach.Active;
}
public class FollowShip : IShipOrder {
    public IShip target;
    public XY offset;
    [JsonProperty]
    private TurnToAngle face;
    public FollowShip(IShip target, XY offset) {
        this.target = target;
        this.offset = offset;
        this.face = new(0);
    }
    public void Update(double delta, AIShip owner) {
        //Remove dock
        owner.dock.Clear();
        var velDiff = owner.velocity - target.velocity;
        double decel = owner.shipClass.thrust * Program.TICKS_PER_SECOND / 2;
        double stoppingTime = velDiff.magnitude / decel;
        double stoppingDistance = owner.velocity.magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
        var stoppingPoint = owner.position;
        if (!owner.velocity.isZero) {
            stoppingPoint += owner.velocity.normal * stoppingDistance;
        }
        var formationPoint = target.position + this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
        var dest = formationPoint + (target.velocity * stoppingTime);
        var offset = dest - owner.position;
#if DEBUG
        //Heading.Crosshair(owner.world, dest);
#endif
        var velProjection = velDiff * velDiff.Dot(offset.normal) / velDiff.Dot(velDiff);
        var velRejection = velDiff - velProjection;
        if (velRejection.magnitude2 > 1) {
            owner.SetDecelerating(true);
        }
        if (offset.magnitude > velDiff.magnitude / 10) {
            //Approach the target
            //Face the target
            face.targetRads = offset.angleRad;
            //var Face = new FaceOrder(Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, owner.ShipClass.thrust * 30, out _));
            face.Update(delta, owner);
            //If we're facing close enough
            if (Math.Abs(Helper.AngleDiffDeg(owner.rotationDeg, offset.angleRad * 180 / Math.PI)) < 10 && (velProjection.magnitude < offset.magnitude / 2 || velDiff.magnitude == 0)) {
                //Go
                owner.SetThrusting(true);
            }
        } else {
            owner.velocity = target.velocity;
            owner.position = formationPoint;
            //Match the target's facing
            face.targetRads = target.rotationDeg * Math.PI / 180;
            face.Update(delta, owner);
        }
    }
    public bool Active => true;
}
public class LootWreck : IShipOrder, Ob<Docking.OnDocked>/*, Lis<Wreck.OnDestroyed>*/ {
    [JsonProperty]
    public Wreck target { get; private set; }
    [JsonProperty]
    private Approach approach;
    public int dockTime;
    public int ticks;

    public Vi<Docking.OnDocked> onDocked = new();
    public LootWreck(Wreck target) {
        this.target = target;
        approach = new(target);
        //target.onDestroyed += this;
        Active = true;
    }
    public override string ToString() => $"loot {target.name}";
    public void Update(double delta, AIShip owner) {
        ticks++;
        if (!target.active) {
            owner.dock.Clear();
            Active = false;
            return;
        }
        if(owner.dock is Docking d && d.Target == target) {
            if (d.docked == true && ++dockTime == 150) {
                owner.dock.Clear();
                Active = false;
                owner.cargo.UnionWith(target.cargo);
                target.cargo.Clear();
                target.Destroy(owner);
            }
            return;
        }
        
        if (ticks % 10 == 0 && approach.currentOffset.magnitude2 < 6 * 6) {
            owner.dock.SetTarget(target, XY.Zero);
            owner.dock.onDocked += this;
        } else {
            approach.Update(delta, owner);
        }
    }
    public bool Active { get; private set; }
    public void Observe(Docking.OnDocked ev) => onDocked.Observe(ev);
    //Wreck.OnDestroyed Lis<Wreck.OnDestroyed>.Value => w => Active = false;
}
public class GuardAt : IShipOrder, Ob<Docking.OnDocked>, Ob<AIShip.Damaged>, Ob<Station.Damaged>, Ob<AttackTarget.TargetInvisible> {
    [JsonProperty]
    public ActiveObject home { get; private set; }
    [JsonProperty]
    public IShipOrder errand { get; private set; } = null;
    [JsonProperty]
    private Approach approach;
    public int errandTime;
    public int ticks;


    public record OnDockedHome(IShip owner, GuardAt order);
    public Vi<OnDockedHome> onDockedHome = new();
    public GuardAt(ActiveObject home) {
        this.home = home;
        approach = new(home);
        errand = null;
        errandTime = -1;
    }
    public GuardAt(ActiveObject home, ActiveObject attackTarget) {
        this.home = home;
        approach = new(home);
        errand = new AttackTarget(attackTarget);
        errandTime = -1;
    }
    public void Init(AIShip owner) {
        owner.onDamaged += this;
    }
    public override string ToString() => $"guard {home.name}{errand switch {
        AttackTarget { target: { name: { }n } }             => $": attack {n}",
        LootWreck { target: { name: { }n } }                => $": loot {n}",
        GateThrough { gate: { destWorld: { name: { }n } } } => $": gate {n}",
        _ => ""
    }}";
    public void SetHome(ActiveObject home) {
        this.home = home;
        approach.target = home;
    }
    public bool CanTarget(ActiveObject other) => errand is AttackTarget a && a.target == other;
    public void SetAttack(ActiveObject target, int attackTime = -1) {
        var a = new AttackTarget(target);
        a.onTargetInvisible += this;
        errand = a;
        errandTime = attackTime;
    }
    public void Observe(AttackTarget.TargetInvisible ev) {
        var a = ev.a;
        if (a == errand) {
            ClearErrand();
        }
    }

    public void SetLoot(Wreck target) {
        errand = new LootWreck(target);
        errandTime = -1;
    }
    public void ClearErrand() {
        errand = null;
        errandTime = -1;
    }
    public bool allowFlee = true;
    public void Update(double delta, AIShip owner) {
        ticks++;
        //If we have an errand, then do it!
        if (errand?.Active == true) {
            errand.Update(delta, owner);
            //If we have finite errand time, then give up on expire
            if (errandTime-- == 0) {
                errand = null;
            }


            if (ticks%30 == 0 && allowFlee && !owner.IsAble()) {
                ClearErrand();
                return;
            }
            return;
        }
        if (ticks % 60 == 0 && owner.world != home.world) {
            errand = new GateThrough(owner.world.FindGateTo(home.world));
            errand.Update(delta, owner);
            return;
        }
        //Otherwise, we're idle
        //If we're docked, then don't check for enemies every tick
        if (ticks % 150 != 0 && owner.dock.docked) {
            return;
        }
        //Look for a nearby attack target periodically
        if (ticks % 30 == 0 && (!allowFlee || owner.IsAble())) {
            if(owner.world.karma.NextDouble() < 1/20f && FindWreck() is Wreck wreck) {
                SetLoot(wreck);
                errand.Update(delta, owner);
                return;
            } else if (FindEnemy() is ActiveObject target) {
                //Start attacking
                SetAttack(target);
                errand.Update(delta, owner);
                return;
            }
        }
        //If we're currently docking, then continue
        if(owner.dock.Target == home) {
            return;
        }
        if (ticks % 10 == 0 && approach.currentOffset.magnitude2 < 6 * 6) {
            var offset = home switch {
                Station s => s.GetDockPoints().MinBy(owner.position.Dist),
                _ => XY.Zero
            };
            owner.dock.SetTarget(home, offset);
            owner.dock.onDocked += this;
        } else {
            approach.Update(delta, owner);
        }
        ActiveObject FindEnemy() =>
            owner.world.entities
                .FilterKey(e => (home.position - e).magnitude2 < 50 * 50)
                .OfType<ActiveObject>()
                .Where(e => !e.IsEqual(owner)
                            && home.CanTarget(e)
                            && SStealth.CanSee(owner, e))
                .GetRandomOrDefault(owner.destiny);

        Wreck FindWreck() =>
            owner.world.entities
                .FilterKey(e => (home.position - e).magnitude2 < 100 * 100)
                .OfType<Wreck>()
                .Where(e => !e.IsEqual(owner))
                .GetRandomOrDefault(owner.destiny);
    }
    public bool Active => home.active;
    public void Observe(Docking.OnDocked ev) => onDockedHome.Observe(new(ev.owner, this));
    public void Observe(AIShip.Damaged ev) {
        var (owner, p) = ev;
        if (!allowFlee) {
            return;
        }
        if (owner.IsAble()) {
            return;
        }
        if (errand is AttackTarget) {
            ClearErrand();
        }
    }
    public void Observe(Station.Damaged ev) {
        var (owner, projectile) = ev;
        if(owner.damageSystem.GetHP() > owner.damageSystem.GetMaxHP()) {
            return;
        }
        allowFlee = false;
    }
}

public class AttackNearby : IShipOrder {
    public int sleepTicks;
    public AttackTarget attack;
    public bool CanTarget(ActiveObject other) => other == attack.target;
    public AttackNearby() {
        attack = new(null);
    }
    public void Update(double delta, AIShip owner) {
        if (sleepTicks > 0) {
            sleepTicks--;
            return;
        }

        if (owner.devices.Weapon.Count == 0) {
            sleepTicks = 150;
            return;
        }
        if (attack.Active == true) {
            attack.Update(delta, owner);
            return;
        }
        //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
        var target = owner.world.entities.all
            .OfType<ActiveObject>()
            .Where(o => owner.IsEnemy(o) && !owner.IsEqual(o))
            .GetRandomOrDefault(owner.destiny);

        //If we can't find a target, then give up for a while
        if (target != null) {
            attack.SetTarget(target);
        } else {
            sleepTicks = 150;
        }
    }
    public bool Active => true;
}
public class FireTrackerAt : IShipOrder, Ob<Weapon.OnHitActive> {
    public int sleepTicks;
    public AttackTarget attack;
    public bool CanTarget(ActiveObject other) => other == attack.target;
    public void Observe(Weapon.OnHitActive ev) {
        var (w, p, h) = ev;
        if (p.hitHull) {
            _active = false;
        }
    }
    public FireTrackerAt(Weapon w, ActiveObject target) {
        attack = new(target);
        w.onHitActive += this;
    }
    public void Update(double delta, AIShip owner) {
        if (sleepTicks > 0) {
            sleepTicks--;
            return;
        }
        if (attack.Active == true) {
            attack.Update(delta, owner);
            return;
        }
        sleepTicks = 150;
        _active = false;
    }
    private bool _active = true;
    public bool Active => _active;
}
public class FireTrackerNearby : IShipOrder, Ob<Weapon.OnHitActive> {
    public int sleepTicks;
    public AttackTarget attack;
    public bool CanTarget(ActiveObject other) => other == attack.target;
    public void Observe(Weapon.OnHitActive ev) {
        var (w, p, h) = ev;
        if (p.hitHull && h == attack.target) {
            attack.ClearTarget();
        }
    }
    public FireTrackerNearby(Weapon w) {
        attack = new(null);
        w.onHitActive += this;
    }
    public void Update(double delta, AIShip owner) {
        if (sleepTicks > 0) {
            sleepTicks--;
            return;
        }
        if (attack.Active == true) {
            attack.Update(delta, owner);
            return;
        }

        var target = owner.world.entities.all
            .OfType<ActiveObject>()
            .Where(o => owner.IsEnemy(o) && !owner.IsEqual(o))
            .GetRandomOrDefault(owner.destiny);

        //If we can't find a target, then give up
        if (target != null) {
            attack.SetTarget(target);
            return;
        }
        _active = false;
    }
    private bool _active = true;
    public bool Active => _active;
}

public class AttackMany : IShipOrder {
    public HashSet<ActiveObject> targets=new();
    public AttackTarget attackOrder=new(null);
    public bool CanTarget(ActiveObject other) => targets.Contains(other);
    public AttackMany(HashSet<ActiveObject> targets) {
        this.targets = targets;
    }
    public void Update(double delta, AIShip owner) {
        if (owner.devices.Weapon.Count == 0) {
            return;
        }
        if (attackOrder.target?.active == true) {
            attackOrder.Update(delta, owner);
            return;
        }
        //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
        targets.RemoveWhere(t => !owner.world.entities.all.Contains(t));
        var target = targets.GetRandomOrDefault(owner.destiny);

        //If we can't find a target, then give up for a while
        if (target != null) {
            attackOrder.SetTarget(target);
            attackOrder.Update(delta, owner);
        } else {
            //attackOrder.SetTarget(null);
        }
    }
    public bool Active => targets.Any();
}
public class AttackTarget : IShipOrder {
    public ActiveObject target { get; private set; }
    public Weapon primary;
    public List<Weapon> secondary=new();
    [JsonProperty]
    private AimAt aim = new(null, 0);
    [JsonProperty]
    private Approach approach = new(null);
    [JsonProperty]
    private GateThrough gate = null;
    [JsonProperty]
    private TurnToAngle face = new(0);

    public int aimWaitingTicks = 0;

    public bool avoid;


    public record TargetInvisible(AttackTarget a);
    public Vi<TargetInvisible> onTargetInvisible = new();

    public AttackTarget(ActiveObject target) {
        SetTarget(target);
    }
    public override string ToString() => $"attack {target.name}";
    public void ClearTarget() => this.target = null;
    public void SetTarget(ActiveObject target) {
        this.target = target;
        this.aim = new(target, 0);
        this.approach = new(target);
    }
    public bool CanTarget(ActiveObject other) => other == target;
    private void Set(Weapon w) => w.SetFiring(true, target);

    double stealthCheckTime;
    public void Update(double delta, AIShip owner) {
        if (target == null) {
            return;
        }
        if (gate != null) {
            var gateWorld = gate.gate.world;
            if (gateWorld == owner.world && owner.world != target.world) {
                gate.Update(delta, owner);
                return; 
            } else {
                gate = null;
            }
        }
        if(!owner.world.entities.Contains(target) && target.active && owner.world != target.world) {
            gate = new(owner.world.FindGateTo(target.world));
            gate.Update(delta, owner);
            return;
        }

        if (onTargetInvisible.any) {
            stealthCheckTime += delta;
            if (stealthCheckTime > 0.5) {
                stealthCheckTime = 0;
                if(SStealth.CanSee(owner, target)) {
                    onTargetInvisible.Observe(new(this));
                }
            }
        }

        var weapons = owner.devices.Weapon;
        if (primary?.AllowFire != true) {
            var available = weapons.Where(w => w.AllowFire);
            primary = available.FirstOrDefault(w => w.aiming == null) ?? available.FirstOrDefault();
            if (primary == null) {
                //omni = null;
                return;
            }
            secondary.Clear();
            secondary.AddRange(available.Where(w => w.aiming != null && w != primary));
        } else if (!primary.ReadyToFire && weapons.Count > 1) {
            primary = weapons.FirstOrDefault(w => w.aiming == null && w.ReadyToFire) ?? primary;
        }

        //Remove dock
        owner.dock.Clear();

        var offset = (target.position - owner.position);
        var dist = offset.magnitude;
        foreach(var w in secondary) {
            if (w.targeting?.target != null) {
                w.SetFiring(true);
            }
        }
        void SetFiringPrimary() {
            Set(primary);
            aimWaitingTicks = 0;
        }
        if(owner.world.tick%30 == 0) {
            avoid = target is PlayerShip pl && pl.powers.Any(p => p.charging && p.Effect.Any(e => e is PowerProjectile));
        }
        var minDist = avoid ? 36 : 12;
        if (dist < minDist) {
            //If we are too close, then move away
            //Face away from the target
            face.targetRads = offset.angleRad + Math.PI;
            face.Update(delta, owner);
            //Get moving!
            owner.SetThrusting(true);
        } else {
            var range = primary.projectileDesc.range;
            if (dist < range) {
                //If we are in range, then aim and fire
                //Aim at the target
                aim.missileSpeed = primary.projectileDesc.missileSpeed;
                aim.Update(delta, owner);
                if (Math.Abs(aim.GetAngleDiff(owner)) < 10
                    && (owner.velocity - target.velocity).magnitude2 < 5 * 5) {
                    owner.SetThrusting(true);
                }
                if (aimWaitingTicks%150 > 90) {
                    owner.SetThrusting(true);
                }
                //Fire if we are close enough
                if (primary.aiming != null || Math.Abs(aim.GetAngleDiff(owner)) * dist < 6) {
                    SetFiringPrimary();
                } else {
                    aimWaitingTicks++;
                }
            } else {
                //Otherwise, get closer
                approach.Update(delta, owner);
                /*
                //Fire if our angle is good enough
                if (primary.aiming != null
                    || Math.Abs(aim.GetAngleDiff(owner)) * dist < 6) {
                    SetFiringPrimary();
                } else {
                    aimWaitingTicks++;
                }
                */
                aimWaitingTicks++;
            }
        }
    }
    public bool Active => target?.active == true;
}
public class GateThrough : IShipOrder {
    public Stargate gate;
    public System destWorld => gate.destWorld;
    public GateThrough(Stargate gate) {
        this.gate = gate;
        Active = true;
    }
    public bool CanTarget(ActiveObject other) => false;
    public void Update(double delta, AIShip owner) {
        if ((owner.position - gate.position).magnitude2 > 5 * 5) {
            new Approach(gate).Update(delta, owner);
        } else {
            gate.Gate(owner);
            Active = false;
        }
    }
    public bool Active { get; private set; }
}

public class PatrolAt : IShipOrder {
    public ActiveObject patrolTarget;
    public double patrolRadius;
    public double attackLimit;
    public AttackTarget attackOrder;
    public int tick;
    public PatrolAt(ActiveObject patrolTarget, double patrolRadius) {
        this.patrolTarget = patrolTarget;
        this.patrolRadius = patrolRadius;
        this.attackLimit = 2 * patrolRadius;
        this.attackOrder = new(null);
    }
    public void Update(double delta, AIShip owner) {
        tick++;
        //Carry out our current attack order
        if (attackOrder.Active == true) {
            attackOrder.Update(delta, owner);
            return;
        }
        //Look for an attack target periodically
        if (tick % 15 == 0) {
            List<ActiveObject> except = new List<ActiveObject> { owner, patrolTarget };
            var attackLimit2 = attackLimit * attackLimit;
            var attackRange2 = 50 * 50;
            var target = owner.world.entities.all
                .OfType<ActiveObject>()
                .Where(p => (patrolTarget.position - p.position).magnitude2 < attackLimit2)
                .Where(p => (owner.position - p.position).magnitude2 < attackRange2)
                .Where(o => owner.IsEnemy(o))
                .Where(o => !SSpaceObject.IsEqual(o, owner) && !SSpaceObject.IsEqual(o, patrolTarget))
                .GetRandomOrDefault(owner.destiny);
            if (target != null) {
                attackOrder.SetTarget(target);
                attackOrder.Update(delta, owner);
                return;
            }
        }
        var offsetFromTarget = (owner.position - patrolTarget.position);
        var dist = offsetFromTarget.magnitude;
        var deltaDist = patrolRadius - dist;
        var nextDist = Math.Abs(deltaDist) > 10 ?
            dist + Math.Sign(deltaDist) * 10 :
            patrolRadius;
        var nextOffset = offsetFromTarget
            .Rotate(2 * Math.PI / 16)
            .WithMagnitude(nextDist);
        var deltaOffset = nextOffset - offsetFromTarget;
        var Face = new TurnToAngle(deltaOffset.angleRad);
        Face.Update(delta, owner);
        owner.SetThrusting(true);
    }
    public bool Active => patrolTarget.active;
}
public class PatrolAround : IShipOrder {
    public ActiveObject patrolTarget;
    public List<ActiveObject> nearbyFriends;
    public ActiveObject nearestFriend;
    public double patrolRadius;
    public double attackLimit;
    public AttackTarget attackOrder;
    public TurnToAngle face;
    public int tick;
    public PatrolAround(ActiveObject patrolTarget, double patrolRadius) {
        this.patrolTarget = patrolTarget;
        this.patrolRadius = patrolRadius;
        this.attackLimit = 2 * patrolRadius;
        this.nearbyFriends = new();
        this.attackOrder = new(null);
        this.face = new(0);
    }
    public void Update(double delta, AIShip owner) {
        tick++;
        //If we have an active attack order, then attack!
        if (attackOrder.Active == true) {
            attackOrder.Update(delta, owner);
            return;
        }
        //Look for an attack target periodically
        if (tick % 15 == 0) {
            List<ActiveObject> except = new List<ActiveObject> { owner, patrolTarget };
            var attackLimit2 = attackLimit * attackLimit;
            var attackRange2 = 50 * 50;
            var target = owner.world.entities.all
                .OfType<ActiveObject>()
                .Where(p => (patrolTarget.position - p.position).magnitude2 < attackLimit2)
                .Where(p => (owner.position - p.position).magnitude2 < attackRange2)
                .Where(o => owner.IsEnemy(o))
                .Where(o => !SSpaceObject.IsEqual(o, owner) && !SSpaceObject.IsEqual(o, patrolTarget))
                .GetRandomOrDefault(owner.destiny);

            if (target != null) {
                attackOrder.SetTarget(target);
                attackOrder.Update(delta, owner);
                return;
            }
        }
        //Update our awareness of friendly stations periodically
        if (tick % 300 == 0) {
            var friendlyStations = owner.world.entities.all.OfType<Station>()
                .Where(s => s.sovereign == patrolTarget.sovereign)
                .OrderBy(s => (s.position - patrolTarget.position).magnitude2);
            nearbyFriends = new();
            nearbyFriends.Add(patrolTarget);
            var threshold = 100 * 100;
            foreach (var s in friendlyStations) {
                if (nearbyFriends.Any(f => (f.position - s.position).magnitude2 < threshold)) {
                    nearbyFriends.Add(s);
                }
            }
        }
        var offsetFromTarget = (owner.position - patrolTarget.position);
        var dist = offsetFromTarget.magnitude;
        var patrolRadius = this.patrolRadius;
        //Update our nearest friend periodically
        if (tick % 15 == 0) {
            nearestFriend = nearbyFriends?.OrderBy(s => (s.position - owner.position).magnitude2).FirstOrDefault();
        }
        if (nearestFriend != null) {
            patrolRadius += (nearestFriend.position - patrolTarget.position).magnitude;
        }
        var deltaDist = patrolRadius - dist;
        var nextDist = Math.Abs(deltaDist) > 25 ?
            dist + Math.Sign(deltaDist) * 25 :
            patrolRadius;
        var nextOffset = offsetFromTarget
            .Rotate(2 * Math.PI / 16)
            .WithMagnitude(nextDist);
        var deltaOffset = nextOffset - offsetFromTarget;
        face.targetRads = deltaOffset.angleRad;
        face.Update(delta, owner);
        owner.SetThrusting(true);
    }
    public bool Active => patrolTarget.active;
}
public class SnipeAt : IShipOrder {
    public ActiveObject target;
    public Weapon weapon;
    [JsonProperty]
    private AimAt aim;
    public SnipeAt(ActiveObject target) {
        this.target = target;
        this.aim = new(target, 0);
    }
    public bool CanTarget(ActiveObject other) => other == target;
    public void Update(double delta, AIShip owner) {
        var weapons = owner.devices.Weapon;
        if (weapon?.AllowFire != true) {
            weapon = weapons.FirstOrDefault(w => w.AllowFire);
            if (weapon == null) {
                return;
            }
            aim.missileSpeed = weapon.projectileDesc.missileSpeed;
        } else if (!weapon.ReadyToFire && weapons.Count > 1 && weapons.FirstOrDefault(w => w.ReadyToFire) is Weapon next) {
            weapon = next;
            aim.missileSpeed = weapon.projectileDesc.missileSpeed;
        }
        //Aim at the target
        aim.Update(delta, owner);
        //Fire if we are close enough
        if (weapon.projectileDesc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
            weapon.SetFiring(true, target);
        }
    }
    public bool Active => target?.active == true && weapon?.AllowFire == true;
}
public class Approach : IShipOrder {
    public MovingObject target;
    [JsonProperty]
    private TurnToAngle face;
    public XY currentOffset;
    public Approach(MovingObject target) {
        this.target = target;
        this.face = new(0);
        currentOffset = XY.Zero;
    }
    public void Update(double delta, AIShip owner) {
        //Remove dock
        owner.dock.Clear();
        //Find the direction we need to go
        currentOffset = (target.position - owner.position);
        var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * currentOffset.x, (2 * owner.destiny.NextDouble() - 1) * currentOffset.y) / 5;
        currentOffset += randomOffset;
        var speedTowards = (owner.velocity - target.velocity).Dot(currentOffset.normal);
        if (speedTowards < 0) {
            //Decelerate
            face.targetRads = Math.PI + owner.velocity.angleRad;
            face.Update(delta, owner);
            owner.SetThrusting(true);
        } else {
            //Approach
            //Face the target
            face.targetRads = currentOffset.angleRad;
            face.Update(delta, owner);
            //If we're facing close enough
            if (Math.Abs(Helper.AngleDiffDeg(owner.rotationDeg, currentOffset.angleRad * 180 / Math.PI)) < 10) {
                //Go
                owner.SetThrusting(true);
            }
        }
    }
    public bool Active => true;
}
// to do: replace this with bool flag
public class TurnToFace : IShipOrder {
    public AimAt order;
    public TurnToFace(MovingObject target, double missileSpeed) {
        this.order = new AimAt(target, missileSpeed);
        Active = true;
    }
    public void Update(double delta, AIShip owner) {
        order.Update(delta, owner);
        Active = Math.Abs(order.GetAngleDiff(owner)) > 1;
    }

    public bool Active { get; private set; }
}
public class AimAt : IShipOrder {
    public MovingObject target;
    public double missileSpeed;
    public double GetTargetRads(AIShip owner) => Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
    public double GetAngleDiff(AIShip owner) => Helper.AngleDiffDeg(owner.rotationDeg, GetTargetRads(owner) * 180 / Math.PI);
    public AimAt(MovingObject target, double missileSpeed) {
        this.target = target;
        this.missileSpeed = missileSpeed;
    }
    public bool Active => true;
    public void Update(double delta, AIShip owner) {
        var targetRads = GetTargetRads(owner);
        var facingRads = owner.stoppingRotation * Math.PI / 180;

        var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        if (ccw < cw) {
            owner.SetRotating(Rotating.CCW);
        } else if (cw < ccw) {
            owner.SetRotating(Rotating.CW);
        }
    }
}
public class TurnToAngle : IShipOrder {
    public double targetRads;
    public TurnToAngle(double targetRads) {
        this.targetRads = targetRads;
    }
    public void Update(double delta, AIShip owner) {
        var facingRads = owner.ship.stoppingRotationWithCounterTurn * Math.PI / 180;

        var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        if (ccw < cw) {
            owner.SetRotating(Rotating.CCW);
        } else if (cw < ccw) {
            owner.SetRotating(Rotating.CW);
        } else {
            if (owner.ship.rotatingVel > 0) {
                owner.SetRotating(Rotating.CW);
            } else {
                owner.SetRotating(Rotating.CCW);
            }
        }
    }
    public bool Active => true;
}
