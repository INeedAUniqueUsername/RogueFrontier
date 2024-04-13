using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using static IslandHopper.Constants;
namespace IslandHopper;

public interface EntityAction {
    void Update();
    bool Done();
    ColoredString Name { get; }
}
/*
public class DelayedAction: EntityAction {
    public EntityAction action;
    public int delay;

    public DelayedAction(EntityAction e, int predelay) {
        this.action = e;
        this.delay = predelay;
    }
    public DelayedAction(Action a, int delay) {
        this.action = new CustomAction(a);
        this.delay = delay;
    }
    public void Update() {
        if(delay > 0) {
            delay--;
        } else {
            action.Update();
        }
    }
    public bool Done() => delay > 0 && action.Done();
}
public class CustomAction : EntityAction {
    public Action action;
    public bool done;

    public CustomAction(Action a) {
        this.done = false;
    }
    public void Update() {
        if(done) {

        } else {
            done = true;
            action();
        }
    }
    public bool Done() => done;
}
*/

public class WalkAction : EntityAction {
    private Entity player;
    private XYZ displacement;
    private XYZ delta;
    private int ticks;
    public WalkAction(Entity actor, XYZ displacement) {
        this.player = actor;
        this.displacement = displacement;
        this.delta = (displacement / STEPS_PER_SECOND);
        ticks = STEPS_PER_SECOND;
    }
    public void Update() {
        player.Position += delta;
        ticks--;
    }
    public bool Done() => ticks == 0;

    public ColoredString Name => new("Walk", Color.Cyan, Color.Black);
}
//Impulse that lasts for a while so that the player cannot jump multiple times at once
public class Jump : EntityAction {
    private Entity player;
    private XYZ velocity;
    private int ticks;
    private int lifetime;
    private int deltaTime;
    public double z => velocity.z;
    public Jump(Entity actor, XYZ velocity, int lifetime = 20) {
        this.player = actor;
        this.velocity = velocity;
        ticks = 0;
        this.lifetime = lifetime;
    }
    public void Update() {
        if (ticks < lifetime) {
            player.Velocity += velocity;
            Debug.Print("JUMP");
        }
        ticks++;
    }
    public bool Done() => ticks >= lifetime;

    public ColoredString Name => z > 0 ? new ColoredString("Jump", Color.Cyan, Color.Black) : new ColoredString("Run", Color.Cyan, Color.Black);
}
public class Impulse : EntityAction {
    private Entity player;
    private XYZ velocity;
    private bool done;
    public Impulse(Entity actor, XYZ velocity) {
        this.player = actor;
        this.velocity = velocity;
        done = false;
    }
    public void Update() {
        player.Velocity += velocity;
        Debug.Print("JUMP");
        done = true;
    }
    public bool Done() => done;
    public ColoredString Name => new("Jump", Color.Cyan, Color.Black);
}
public class WaitAction : EntityAction {
    private int ticks;
    public WaitAction(int ticks) {
        this.ticks = ticks;
    }
    public void Update() => ticks--;
    public bool Done() => ticks == 0;
    public ColoredString Name => new("Wait", Color.Cyan, Color.Black);
}
public interface ICompoundAction : EntityAction { }
public class CompoundAction : ICompoundAction {
    public EntityAction[] actions;
    int index = 0;
    public CompoundAction(params EntityAction[] actions) {
        this.actions = actions;
    }
    public void Update() {
        if (Done()) {
            return;
        }
        actions[index].Update();
        if (actions[index].Done()) {
            index++;
        }
    }
    public bool Done() => index == actions.Length;
    public ColoredString Name => new("Compound Action", Color.Cyan, Color.Black);
}
public class TakeItem : EntityAction {
    public ICharacter actor;
    public IItem item;
    public bool done;
    public TakeItem(ICharacter actor, IItem item) {
        this.actor = actor;
        this.item = item;
    }
    public void Update() {
        if (actor.World.entities[actor.Position].Contains(item)) {
            actor.World.RemoveEntity(item);
            /*
            if(actor is Player p) {
                if(p.found.Add(item.Type)) {
                    if(item.Type.knownChance == 100) {
                        p.known.Add(item.Type);
                    } else if(p.World.karma.Next(0, 100) < item.Type.knownChance) {
                        p.Witness(new InfoEvent(item.GetApparentName(p) + new ColoredString(" is identified as ") + item.Name));
                        p.known.Add(item.Type);
                    }
                }
            }
            */
            actor.Inventory.Add(item);
        }
        done = true;
    }
    public bool Done() => done;
    public ColoredString Name => new ColoredString("Take ", Color.Cyan, Color.Black) + item.Name;
}
public class FollowPath : ICompoundAction {
    public ICharacter actor;
    public LinkedList<XYZ> points;
    private WalkAction action;
    public FollowPath(ICharacter actor, LinkedList<XYZ> points) {
        this.actor = actor;
        this.points = points;
    }
    public void Update() {
        //Note: If the actor is pushed during this compound action, it will automatically warp back to the path. We should handle interruptions where the actor is attacked
        if (action?.Done() != false) {
            if (points.Count == 0) {
                return;
            }
            //Make a new action

            if (points.Count > 0) {
                //Truncate to integer coordinates so that we don't get confused by floats
                action = new WalkAction(actor, points.First.Value.i - actor.Position.i);
                points.RemoveFirst();
            }
        } else {
            action.Update();
        }
    }
    public bool Done() => points.Count == 0 && (action == null || action.Done());
    public ColoredString Name => new("Follow Path", Color.Cyan, Color.Black);
}
public class AttackAction : EntityAction, Damager {
    Entity attacker, target;
    IItem weapon;
    int ticks;
    public AttackAction(Entity attacker, Entity target, IItem weapon) {
        this.attacker = attacker;
        this.target = target;
        this.weapon = weapon;
        ticks = 10;
    }
    public void Update() {
        ticks--;
        if (ticks == 0) {
            var ev = new InfoEvent(attacker.Name + new ColoredString(" strikes ", Color.White, Color.Black) + target.Name + new ColoredString(" with ", Color.White, Color.Black) + weapon.Name);
            attacker.Witness(ev);
            target.Witness(ev);

            //To do: Damage calculation
            if (target is Damageable d) {
                d.OnDamaged(this);
            }
        }
    }
    public bool Done() => ticks == 0;

    public ColoredString Name => new("Walk", Color.Cyan, Color.Black);
}

public class ReloadAction : EntityAction, Damager {
    public Gun gun;
    public int amount;
    public int ticks;
    public ReloadAction(Gun gun, int amount) {
        this.gun = gun;
        this.amount = amount;
        ticks = 90;
    }
    public void Update() {
        ticks--;
        if (Done()) {
            gun.AmmoLeft = Math.Min(gun.AmmoLeft + amount, gun.desc.maxAmmo);
        }
    }
    public bool Done() => ticks == 0;
    public ColoredString Name => new ColoredString("Reload", Color.Cyan, Color.Black) + " " + gun.item.Name;
}
