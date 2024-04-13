using Common;
using SadRogue.Primitives;
using SadConsole;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using ASECII;

namespace RogueFrontier;
public interface ITrail {
    Effect GetParticle(XY Position, XY Velocity = null);
}
public record SimpleTrail(StaticTile Tile) : ITrail {
    public Effect GetParticle(XY Position, XY Velocity = null) => new EffectParticle(Position, Velocity ?? new XY(0, 0), Tile, 3);
}
public class Projectile : MovingObject {
    public ulong id { get; set; }
    public System world { get; set; }
    public ActiveObject source;
    public XY position { get; set; }
    public XY velocity { get; set; }
    public double direction;

    public double fragmentRotation;
    public double lifetime { get; set; }
    public double age;
    public ColoredGlyph tile { get; private set; }
    public ITrail trail;
    public FragmentDesc desc;
    public Maneuver maneuver;
    public int damageHP;
    public int armorSkip;
    public int ricochet = 0;
    class IntervalFragment {
        public IntervalFragment(FragmentDesc desc) {
            this.desc = desc;
        }
        public FragmentDesc desc;
        public double elapsed = 0;
    }

    List<IntervalFragment> intervalFragments;

    //Hit results
    public bool hitReflected;
    public bool hitBlocked;
    public bool hitHull;
    public bool hitKill;
    public bool hitHandled => damageHP == 0 || hitReflected || hitBlocked;
    public double detonateRadius;
    public HashSet<Entity> exclude = new();
    //List of projectiles that were created from the same fragment
    public List<Projectile> salvo = new();
    public record OnHitActive(Projectile p, ActiveObject other);
    public Vi<OnHitActive> onHitActive=new();
    public bool active { get; set; } = true;
    public Projectile() { }
    public Projectile(ActiveObject source, FragmentDesc desc, XY position, XY velocity, double? direction = null, Maneuver maneuver = null, HashSet<Entity> exclude = null) {
        this.id = source.world.nextId++;
        this.source = source;
        this.world = source.world;
        this.tile = desc.effect.Original;
        this.position = position;
        this.velocity = velocity;
        this.direction = direction ?? velocity.angleRad;
        this.lifetime = desc.lifetime;
        this.desc = desc;
        this.intervalFragments = desc.Fragment.Where(f => f.fragmentInterval > 0).Select(f => new IntervalFragment(f)).ToList();
        this.trail = (ITrail)desc.Trail ?? new SimpleTrail(desc.effect.Original);
        this.maneuver = maneuver;
        this.damageHP = desc.damageHP.Roll();
        this.armorSkip = desc.armorSkip;
        this.ricochet = desc.ricochet;

        this.detonateRadius = desc.detonateRadius;

        this.exclude = exclude;
        if(this.exclude == null) {
            this.exclude = desc.hitSource ?
                new() { null, this } :
                new() { null, source, this };
            this.exclude.UnionWith(source switch {
                PlayerShip { avoidHit: { }ah } => ah,
                AIShip { avoidHit: { }ah } => ah,
                Station {guards:{ }g } => g,
                _ => new HashSet<Entity>()
            });
        }
        //exclude.UnionWith(source.world.entities.all.OfType<ActiveObject>().Where(a => a.sovereign == source.sovereign));
    }

    public void Update(double delta) {
        if (lifetime > 0) {

            var deltaTicks = delta * 60;
            lifetime -= deltaTicks;
            age += deltaTicks;
            fragmentRotation += delta * desc.fragmentSpin;
            UpdateMove();
            foreach (var f in intervalFragments) {
                if(age > f.desc.fragmentInitialDelay) {
                    ref var elapsed = ref f.elapsed;
                    elapsed += deltaTicks;
                    var interval = f.desc.fragmentInterval;
                    while(elapsed > interval) {
                        elapsed -= interval;
                        Fragment(f.desc);
                    }
                }
            }
        } else if(active) {
            UpdateMove();
            if (world.karma.NextDouble() < desc.detonateFailChance) {
                goto NoDetonate;
            }
            Detonate();

            NoDetonate:
            active = false;

        }
        void UpdateMove() {
            maneuver?.Update(delta, this);

            var dest = position + velocity * delta;
            var inc = velocity.normal * 0.5;
            var steps = velocity.magnitude * 2 * delta;

            bool InDetonateRange() {
                var r = detonateRadius * detonateRadius;
                return r > 0 && world.entities.FilterKey(p => (position - p).magnitude2 < r).Select(e => e is ISegment { parent: { } p } ? p : e)
                    .Distinct().Except(exclude).Any(e => e switch {
                        ActiveObject {active:true } => true,
                        Projectile p => !exclude.Contains(p.source) && desc.hitProjectile,
                        //TargetingMarker marker => marker.Owner == source,
                        _ => false
                    });
            }
            if (InDetonateRange()) {
                if (world.karma.NextDouble() < desc.detonateFailChance) {
                    detonateRadius = 0;
                    goto NoDetonate;
                }
                lifetime = 0;
                Detonate();
                return;
            }
            NoDetonate:
            for (int i = 0; i < steps; i++) {
                position += inc;

                bool destroyed = false;
                bool stop = false;

                var entities = world.entities[position].Select(e => e is ISegment { parent: { }p } ? p : e).Distinct().Except(exclude);
                foreach (var other in entities) {
                    switch (other) {
                        case Asteroid a:
                            lifetime = 0;
                            destroyed = true;
                            break;
                        case ActiveObject { active: true } hit when !destroyed && (desc.hitNonTarget || new[] { null, hit }.Contains(maneuver?.target)):
                            hitReflected |= world.karma.NextDouble() < desc.detonateFailChance;
                            var angle = (position - hit.position).angleRad;

                            hit.Damage(this);
                            onHitActive.Observe(new(this, hit));

                            var cg = new ColoredGlyph(hitHull ? Color.Yellow : Color.LimeGreen, Color.Transparent, 'x');
                            world.AddEffect(new EffectParticle(hit.position + XY.Polar(angle), hit.velocity, cg, 10));


                            if (hitReflected) {
                                hitReflected = false;
                                velocity = -velocity;

                                exclude = new();
                            } else if (ricochet > 0) {
                                ricochet--;
                                velocity = -velocity;
                                //velocity += (hit.velocity - velocity) / 2;
                                //stop = true;

                                exclude = new();
                            } else {
                                Detonate();

                                lifetime = 0;
                                destroyed = true;
                            }
                            break;
                        case Projectile p when !exclude.Contains(p.source) && desc.hitProjectile && !destroyed:
                            /*
                            var delta = Math.Min(p.damageHP, damageHP);
                            if((p.damageHP -= delta) <= 0) {
                                p.lifetime = 0;
                            }
                            if((damageHP -= delta) == 0) {
                                lifetime = 0;
                                destroyed = true;
                            }
                            */
                            p.lifetime = 0;
                            lifetime = 0;
                            destroyed = true;
                            break;
                        case ProjectileBarrier barrier when desc.hitBarrier:
                            barrier.Interact(this);
                            stop = true;
                            //Keep interacting with all the barriers
                            break;
                    }
                }
                if (stop || destroyed) {
                    return;
                }
            CollisionDone:
                world.AddEffect(trail.GetParticle(position));
            }
            position = dest;
        }
    }
    public record Detonated(Projectile source) { };
    public Vi<Detonated> onDetonated = new();
    public void Detonate() {
        var d = new Detonated(this);
        onDetonated.Observe(d);

        
        foreach (var f in desc.Fragment) {
            Fragment(f);
        }
        if (desc.Flash is { } fl) {
            fl.Create(world, position);
        }
    }
    public void Fragment(FragmentDesc fragment) {
        if (fragment.targetLocked != null
            && fragment.targetLocked != (maneuver?.target != null)) {
            return;
        }
        double angleInterval = fragment.spreadAngle / fragment.count;
        double fragmentAngle;
        if (fragment.omnidirectional
            && maneuver?.target is { active:true } target
            && (target.position - position) is { } offset
            && offset.magnitude < fragment.range) {
            fragmentAngle = Main.CalcFireAngle(offset, (target.velocity - velocity), fragment.missileSpeed, out var _);
        } else {
            fragmentAngle = direction;
        }
        fragmentAngle += fragmentRotation;
        HashSet<Entity> exclude = new() { null, this };
        if (fragment.precise) {
            exclude.UnionWith(this.exclude);
        }
        if(fragment.hitSource) {
            exclude.Remove(source);
        } else {
            exclude.Add(source);
        }
        List<Projectile> salvo = new();
        for (int i = 0; i < fragment.count; i++) {
            double angle = fragmentAngle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            var p = new Projectile(source,
                fragment,
                position + XY.Polar(angle, 0.5),
                velocity + XY.Polar(angle, fragment.missileSpeed),
                angle,
                fragment.GetManeuver(maneuver?.target),
                exclude
                ) { salvo = salvo };
            salvo.Add(p);
            world.AddEntity(p);
        }
    }
}
public class Maneuver {
    public ActiveObject target;
    public double maneuver;
    public double maneuverDistance;
    public bool smart = true;
    private double prevDistance = double.NegativeInfinity;
    private bool startApproach = false;
    public Maneuver(ActiveObject target, double maneuver, double maneuverDistance) {
        this.target = target;
        this.maneuver = maneuver;
        this.maneuverDistance = maneuverDistance;
    }
    public void Update(double delta, Projectile p) {
        if (target == null || maneuver == 0) {
            return;
        }
        //var uncertainty = XY.Polar(p.world.karma.NextDouble() * 2 * Math.PI, 0);
        var vel = p.velocity;
        var offset = target.position - p.position;
        var turn = maneuver * delta * Program.TICKS_PER_SECOND;
        var velLeft = vel.Rotate(turn);
        var velRight = vel.Rotate(-turn);
        var distLeft = (offset - velLeft.normal).magnitude;
        var distRight = (offset - velRight.normal).magnitude;
        (var closer, var farther) = distLeft < distRight ? (velLeft, velRight) : (velRight, velLeft);
        if (maneuverDistance == 0) {
            if(smart) {
                var dist = offset.magnitude;
                if (dist < prevDistance) {
                    p.velocity = closer;
                    startApproach = false;
                } else if (startApproach) {
                    p.velocity = closer;
                } else {
                    p.velocity = farther;

                    var deltaVel = target.velocity - p.velocity;
                    var deltaAngle = Math.Abs((offset.normal - deltaVel.normal).angleRad);
                    var deltaAngleDeg = deltaAngle * 180 / Math.PI;


                    var timeToHit = offset.magnitude / deltaVel.magnitude;
                    var timeToTurn = Math.Min(Math.PI/2, deltaAngle) / (maneuver * Program.TICKS_PER_SECOND);

                    if (timeToTurn < timeToHit) {
                        startApproach = true;

                    }
                }
                prevDistance = dist;
            } else {
                p.velocity = closer;
            }
            /*
            var deltaVel = target.velocity - p.velocity;
            //var deltaAngle = Math.Atan2(deltaVel.y, deltaVel.x);

            var deltaAngle = Math.Abs((offset - deltaVel).angleRad);
            
            var circ = Math.Abs(deltaVel.magnitude) * (2 * Math.PI) / (maneuver * Program.TICKS_PER_SECOND);
            //var circ = (deltaVel.magnitude) * Math.Abs(deltaAngle) / (maneuver * delta * Program.TICKS_PER_SECOND);
            var diameter = circ / Math.PI;
            var threshold = Math.Abs(diameter * Math.Cos(deltaAngle));

            var timeToHit = offset.magnitude / deltaVel.magnitude;
            var timeToTurn = deltaAngle / (maneuver * Program.TICKS_PER_SECOND);
            //if (offset.magnitude > diameter) {
            if (timeToTurn < timeToHit * 3 || deltaAngle < Math.PI/4) {
                if (distLeft < distRight) {
                    p.velocity = velLeft;
                } else if (distRight < distLeft) {
                    p.velocity = velRight;
                }
            } else {
                //Turn away so we can turn towards later
                if (distLeft < distRight) {
                    p.velocity = velRight;
                } else if (distRight < distLeft) {
                    p.velocity = velLeft;
                }
            }
            */
        } else {
            if (offset.magnitude > maneuverDistance) {
                p.velocity = closer;
            } else {
                p.velocity = farther;
            }
        }
    }
}
