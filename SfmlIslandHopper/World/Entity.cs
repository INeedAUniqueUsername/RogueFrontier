using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static IslandHopper.Constants;

namespace IslandHopper;

/*
public interface Visible {
    XYZ Position { get; set; }
    ColoredGlyph SymbolCenter { get; }
    bool Active { get; }
}
*/
class EntityPosition : ILocator<Entity, (int, int, int)> {
    public (int, int, int) Locate(Entity e) => e.Position;
}
class EffectPosition : ILocator<Effect, (int, int, int)> {
    public (int, int, int) Locate(Effect e) => e.Position;
}
public interface Entity : Effect {
    Island World { get; }
    //XYZ Position { get; set; }			//Position in meters
    XYZ Velocity { get; set; }          //Velocity in meters per step
                                        //bool Active { get; }                    //	When this is inactive, we remove it
    void OnRemoved();
    //void UpdateRealtime(TimeSpan delta);				//	For step-independent effects
    //void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND

    //ColoredGlyph SymbolCenter { get; }
    ColoredString Name { get; }
}
public interface Damageable {
    void OnDamaged(Damager source);
}
public interface Damager {
}
public static class EntityHelper {
    public static bool OnGround(this Entity g) => g.World.voxels.InBounds(g.Position)
        && (g.World.voxels[g.Position].Collision == VoxelType.Floor
        || g.World.voxels[g.Position.PlusZ(-0.8)].Collision == VoxelType.Solid
        || g.World.entities[g.Position.PlusZ(-0.8)].OfType<Standable>().Any());
    public static void UpdateGravity(this Entity g) {
        //	Fall or hit the ground
        if (g.OnGround()) {
            UpdateFriction();
            StopFalling();
        } else {
            var down = g.Position.PlusZ(-0.8);
            var standable = g.World.voxels.InBounds(down) ? g.World.entities[down].OfType<Standable>().FirstOrDefault() : null;
            if (standable != null) {
                //Friction
                var velDiff = g.Velocity - standable.Velocity;
                g.Velocity = standable.Velocity + velDiff * 0.9;

                StopFalling();
            } else {
                Debug.Print("fall");
                g.Velocity += new XYZ(0, 0, -9.8 / STEPS_PER_SECOND);
            }
        }
        void UpdateFriction() {
            g.Velocity.x *= 29/30f;
            g.Velocity.y *= 29/30f;
        }
        void StopFalling() {
            if (g.Velocity.z < 0) {
                g.Velocity.z = 0;
            }
        }
    }
    //We attempt to enforce continuous collision detection by incrementing the motion in small steps
    private static XYZ CalcMotionStep(XYZ Velocity) {
        if (Velocity.Magnitude < 1) {
            return Velocity / 2;
        } else {
            return Velocity.Normal / 2;
        }
    }
    public static void UpdateMotion(this Entity g) {
        //TO DO: Implement fall damage
        var Velocity = g.Velocity / 30;
        if (Velocity < 0.1) {
            var p = g.Position + Velocity / 30;
            var v = g.World.voxels.Try(p);
            if (v is Air) {
                g.Position = p;
            }
            return;
        }
        XYZ step = CalcMotionStep(Velocity / 30);
        XYZ final = g.Position;
        for (XYZ p = g.Position + step; (g.Position - p).Magnitude < Velocity.Magnitude / 30; p += step) {
            if (g.World.voxels.Try(p) is Air) {
                final = p;
            } else {
                break;
            }
        }
        /*
        //The velocity is the displacement that we were supposed to travel for this step
        //This is the average velocity for the actual displacement we traveled in this step
        Point3 velocityAverage = (final - g.Position);
        //This is the displacement that we did not get to travel this tick
        Point3 velocityDelta = velocityAverage - g.Velocity;
        */
        g.Position = final;
    }
    public static void UpdateMotionCollision(this Entity g, Func<Entity, bool> collideEntity = null, Func<Voxel, bool> collideTile = null) {
        //If the velocity is too small, then we get an infinite loop from trying to increment
        var Velocity = g.Velocity / 30;
        if (Velocity < 0.1) {
            var p = g.Position + Velocity;
            var v = g.World.voxels.Try(p);
            if (v is Air || collideTile?.Invoke(v) == false) {
                if (collideEntity != null) {
                    var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                    foreach (var entity in entities) {
                        if (collideEntity(entity)) {
                            return;
                        }
                    }
                    g.Position = p;
                } else {
                    g.Position = p;
                }
            }
            return;
        }
        //ignoreEntityCollision = ignoreEntityCollision ?? (e => true);
        //ignoreTileCollision = ignoreTileCollision ?? (v => false);
        XYZ step = CalcMotionStep(Velocity);
        XYZ final = g.Position;
        for (XYZ p = g.Position + step; (g.Position - p).Magnitude < Velocity.Magnitude; p += step) {
            var v = g.World.voxels.Try(p);
            if (v is Air || collideTile?.Invoke(v) == false) {
                if (collideEntity != null) {
                    var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                    foreach (var entity in entities) {
                        if (collideEntity(entity)) {
                            goto Done;
                        }
                    }
                    final = p;
                } else {
                    final = p;
                }
            } else {
                break;
            }
        }
    Done:
        g.Position = final;
    }
    public static void UpdateMotionCollisionTrail(this Entity g, out HashSet<XYZ> trail, Func<Entity, bool> collideEntity = null, Func<Voxel, bool> collideTile = null) {
        trail = new HashSet<XYZ>(new XYZGridComparer());
        var Velocity = g.Velocity / 30;
        if (Velocity < 0.1) {
            var p = g.Position + Velocity;
            var v = g.World.voxels.Try(p);
            if (v is Air || collideTile?.Invoke(v) == false) {
                if (collideEntity != null) {
                    var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                    foreach (var entity in entities) {
                        if (collideEntity(entity)) {
                            return;
                        }
                    }
                    g.Position = p;
                } else {
                    g.Position = p;
                }
            }
            trail.Add(p.i);
            return;
        }

        //ignoreEntityCollision = ignoreEntityCollision ?? (e => true);
        //ignoreTileCollision = ignoreTileCollision ?? (v => false);
        XYZ step = CalcMotionStep(Velocity);
        XYZ final = g.Position;
        trail.Add(final.i);
        for (XYZ p = g.Position + step; (g.Position - p).Magnitude < Velocity.Magnitude; p += step) {
            var v = g.World.voxels.Try(p);
            if (v is Air || collideTile?.Invoke(v) == false) {
                if (collideEntity != null) {
                    var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                    foreach (var entity in entities) {
                        if (collideEntity(entity)) {
                            goto Done;
                        }
                    }
                    final = p;
                    trail.Add(final.i);
                } else {
                    final = p;
                    trail.Add(final.i);
                }
            } else {
                break;
            }
        }
    Done:
        g.Position = final;
    }
    public static void Witness(this Entity e, IPlayerMessage we) {
        if (e is ICharacter w)
            w.AddMessage(we);
    }
}
