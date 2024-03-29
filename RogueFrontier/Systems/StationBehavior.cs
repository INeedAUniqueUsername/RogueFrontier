using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Console = SadConsole.Console;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;
using Kodi.Linq.Extensions;
namespace RogueFrontier;
public interface StationBehavior {
    void Update(double delta, Station owner);
    public void RegisterGuard(AIShip guard) { }
}
public class OrionWarlordOutpost : StationBehavior, Ob<Station.Destroyed>, Ob<GuardAt.OnDockedHome> {

    private HashSet<ActiveObject> turretsDeployed = new();
    public void Observe(Station.Destroyed ev) {
        (var station, var destroyer, var wreck) = ev;
        if (destroyer?.active != true) {
            return;
        }
        if (station.world.entities.all
            .OfType<Station>()
            .Where(s => s.active && s.sovereign == station.sovereign && s.UpdateGuardList().Any())
            .OrderBy(s => (s.position - destroyer.position).magnitude2)
            .FirstOrDefault() is Station s) {
            s.guards.ForEach(g => g.behavior = new GuardAt(s, destroyer));
        }
    }
    public void Observe(GuardAt.OnDockedHome ev) {
        (var ship, var order) = ev;
        var home = order.home;
        if (!turretAssignment.TryGetValue(home, out var item)) {
            return;
        }
        turretsDeployed.Remove(home);
        ship.cargo.Remove(item);
        var w = ship.world;
        var turret = new Station(w, turretStationType, home.position);
        w.AddEntity(turret);
        turret.CreateSegments();
    }
    Dictionary<ActiveObject, Item> turretAssignment = new();
    ItemType turretItemType;
    StationType turretStationType;
    public OrionWarlordOutpost(Station owner) {
        owner.onDestroyed += this;
        turretItemType = owner.world.types.Lookup<ItemType>("item_orion_turret");
        turretStationType = owner.world.types.Lookup<StationType>("station_orion_turret");
    }
    public void Update(double delta, Station owner) {
        if (owner.world.tick % 1200 == 0) {
            if (owner.cargo.FirstOrDefault(i => i.type == turretItemType) is Item turretItem
                && owner.guards.FirstOrDefault() is AIShip turretGuard) {
                var k = owner.world.karma;

                var enemies = owner.world.entities.all
                    .OfType<Station>()
                    .Where(a => owner.CanTarget(a))
                    .Shuffle();
                foreach (var enemy in enemies) {
                    XY p = enemy.position + XY.Polar(k.NextDouble() * Math.PI * 2, k.NextInteger(80, 200));
                    if (enemies.All(a => (a.position - p).magnitude > 70) && enemies.Any(a => (a.position - p).magnitude < 200)) {

                        owner.cargo.Remove(turretItem);
                        turretGuard.cargo.Add(turretItem);

                        var turretMarker = new ActiveMarker(owner.world, owner.sovereign, p);
                        turretAssignment[turretMarker] = turretItem;

                        var o = new GuardAt(turretMarker);
                        o.onDockedHome += this;
                        turretGuard.behavior = new CompoundOrder(o, new GuardAt(owner));
                        break;
                    }
                }
            }

        }
    }
}
public class IronPirateOutpost : StationBehavior {
    public IronPirateOutpost() { }
    public void Update(double delta, Station owner) {
        if (owner.world.tick % 300 == 0) {
            //Clear any pirate attacks where the target has too many defenders
            foreach (var g in owner.guards) {
                if (g.behavior.GetOrder() is GuardAt o
                    && o.errand is AttackTarget { Active:true } a
                    && CountDefenders(a.target, g) > 2) {
                    o.ClearErrand();
                }
            }
            var targets = owner.world.entities.all
                        .OfType<IShip>()
                        .Where(s => owner.IsEnemy(s))
                        .Where(s => (s.position - owner.position).magnitude < 500)
                        .ToList();
            //Handle all available guards
            foreach (var g in owner.guards) {
                if (g.behavior.GetOrder() is GuardAt { errandTime: < 1 } o) {
                    var target = targets.FirstOrDefault(
                        s => {
                            int attackers = CountAttackers(s), defenders = CountDefenders(s, g);
                            return attackers < 5 && defenders < 3;
                        });
                    if (target != null) {
                        o.SetAttack(target);
                    }
                }
            }
            //Count the number of objects that could defend this target from the attacker
            int CountDefenders(ActiveObject target, ActiveObject attacker) {
                return target.world.entities.all
                        .OfType<ActiveObject>()
                        .Where(other => (other.position - target.position).magnitude < 150)
                        .Where(other => other.CanTarget(attacker))
                        .Count();
            }
            //Count the number of ships already attacking this target
            int CountAttackers(ActiveObject target) {
                return target.world.entities.all
                        .OfType<AIShip>()
                        .Where(s => s.sovereign == owner.sovereign)
                        .Where(s => s.CanTarget(target))
                        .Count();
            }
        }
    }
}
public class ConstellationAstra : StationBehavior {
    public HashSet<StationType> stationTypes;
    public HashSet<AIShip> reserves;
    public ConstellationAstra(Station owner) {
        stationTypes = new() {
            owner.world.types.Lookup<StationType>("station_constellation_shipyard"),
            owner.world.types.Lookup<StationType>("station_constellation_bunker")
        };
        reserves = new(Enumerable.Range(0, 16).Select(i => new AIShip(
            new(owner.world, owner.world.types.Lookup<ShipClass>("ship_beowulf"), owner.position),
            owner.sovereign,
            null,
            new GuardAt(owner))));
    }
    public void Update(double delta, Station owner) {
        if (owner.world.tick % 150 == 0) {
            owner.UpdateGuardList();
            if (owner.guards.Count < 5) {
                var world = owner.world;
                if (owner.world.universe.GetAllEntities().OfType<Station>().FirstOrDefault(s => stationTypes.Contains(s.type) && s.guards.Count > 5) is Station other) {
                    while (owner.guards.Count < 5 && other.guards.Count > 5) {
                        var g = other.guards.GetRandom(owner.world.karma);
                        g.behavior = new GuardAt(owner);
                        other.guards.Remove(g);
                        owner.guards.Add(g);
                    }
                }
            } else {
                var nearbyFriendly = owner.world.entities.all.OfType<Station>()
                    .Where(s => s.sovereign == owner.sovereign
                    && (s.position - owner.position).magnitude < 250);

                foreach (var nearby in nearbyFriendly) {
                    nearby.UpdateGuardList();
                    if (nearby.guards.Count < 3) {
                        if (owner.guards.Count > 3) {
                            var g = owner.guards.Last();
                            ((GuardAt)g.behavior.GetOrder()).SetHome(nearby);
                            owner.guards.RemoveAt(owner.guards.Count - 1);
                            nearby.guards.Add(g);
                        }
                    }
                }
            }
        }
    }
}
public class ConstellationShipyard : StationBehavior {
    public HashSet<ShipClass> guardTypes;
    public ConstellationShipyard() { }
    public ConstellationShipyard(Station owner) {
        Func<string, ShipClass> f = owner.world.types.Lookup<ShipClass>;
        guardTypes = new() {
            f("ship_ulysses"),
            f("ship_beowulf")
        };
    }
    public void Update(double delta, Station owner) {
        if (owner.world.tick % 900 == 0) {
            owner.UpdateGuardList();
            if(owner.guards.Count < 15) {
                var s = new AIShip(new(owner.world, guardTypes.GetRandom(owner.world.karma), owner.position),
                    owner.sovereign, new GuardAt(owner));
                owner.guards.Add(s);
                owner.world.AddEntity(s);
            }
        }
    }
}
public class DaughtersOutpost : StationBehavior {
    public bool sanctumReady = true;
    public int funds = 1000;
    public void Update(double delta, Station owner) {
    }
}
public class AmethystStore : StationBehavior, Ob<Station.Destroyed>, Ob<Station.Damaged>, Ob<Weapon.OnFire>, Ob<Power.OnInvoked> {

    Dictionary<PlayerShip, int> damaged=new();
    HashSet<PlayerShip> banned = new();
    int damageTaken;
    public void Observe(Weapon.OnFire ff) {
        var(weapon, _) = ff;
        weapon.delay /= 2;
    }
    public void Observe(Station.Destroyed ev) {
        (var station, var destroyer, var wreck) = ev;
        if (destroyer?.active != true) {
            return;
        }
    }
    public void Observe(Station.Damaged ev) {
        (var station, var projectile) = ev;
        var source = projectile.source;
        if (source?.active != true) {
            return;
        }
        if(source is PlayerShip pl) {
            if (banned.Contains(pl)) {
                return;
            }
            if (!damaged.TryGetValue(pl, out var d)) {
                damaged[pl] = 0;
            }
            d = damaged[pl] += projectile.damageHP;
            if (d > 80) {
                //station.weapons.ForEach(w => w.onFire += this);
                station.weapons.ForEach(w => w.SetTarget(pl));

                banned.Add(pl);
                if (pl.cargo.RemoveWhere(i => i.type.codename == "item_amethyst_member_card") > 0) {
                    pl.AddMessage(new Transmission(station, new ColoredString("You have violated the Terms of Service. Your warranty is now void.", Color.Red, Color.Black)));
                }
                if (pl.shipClass.attributes.Contains("Amethyst")) {
                    pl.AddMessage(new Message("Self-Destruct remotely initiated by vendor."));
                    pl.world.AddEvent(new SelfDestruct(pl, 15));
                }
            }
        }
        damageTaken += projectile.damageHP;
        if(damageTaken > 80) {
            if (shine.ready) {
                shine.Invoke(station);
            }
        }
    }
    public void Observe(Power.OnInvoked ev) {
        damageTaken = 0;
    }
    Power shine;
    public AmethystStore(Station owner) {
        owner.onDamaged += this;
        owner.onDestroyed += this;
        shine = new(owner.world.types.Lookup<PowerType>("power_shine"));
        shine.onInvoked += this;
    }
    public void Update(double delta, Station owner) {
        shine.Update(delta, owner);
    }
}