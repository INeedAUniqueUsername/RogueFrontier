using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandHopper;

public interface ICharacter : Entity, Damageable {
    HashSet<IItem> Inventory { get; }
    HashSet<EntityAction> Actions { get; }
    void AddMessage(IPlayerMessage we);
}
public class Player : ICharacter {
    public XYZ Velocity { get; set; }
    public XYZ Position { get; set; }
    public Island World { get; set; }
    public HashSet<EntityAction> Actions { get; private set; }
    public Equipment Equipment;
    public HashSet<IItem> Inventory { get; private set; }
    public HashSet<Effect> Watch { get; private set; }
    public List<HistoryEntry> HistoryLog { get; }   //All events that the player has witnessed
    public List<HistoryEntry> HistoryRecent { get; }   //Events that the player is currently witnessing

    public Health health;

    public HashSet<ItemType> found;
    public HashSet<ItemType> known;

    //TO DO: Implement equipment and body

    public int frameCounter = 0;
    public int tick = 0;

    public Player(Island World, XYZ Position) {
        this.World = World;
        this.Position = Position;
        this.Velocity = new XYZ(0, 0, 0);
        Actions = new HashSet<EntityAction>();
        Equipment = new Equipment();
        Inventory = new HashSet<IItem>();
        Watch = new HashSet<Effect>();

        HistoryLog = new List<HistoryEntry>();
        HistoryRecent = new List<HistoryEntry>();
        health = new Health();
    }
    public bool AllowUpdate() => Actions.Count > 0 || frameCounter > 0;
    public bool Active { get; private set; } = true;
    //TO DO: Make sure we remember to call this whenever something is removed for good
    public void OnRemoved() { }
    public void UpdateRealtime(TimeSpan delta) {
        HistoryRecent.RemoveAll(e => (e.ScreenTime -= delta.TotalSeconds) < 0);
        foreach (var i in Inventory) {
            i.UpdateRealtime(delta);
        }
    }
    public void UpdateStep() {
        tick++;
        health.UpdateStep();
        if (health.bleeding > 0 && tick % 5 == 0) {
            World.AddEffect(new Trail(Position, 150, new ColoredGlyph(Color.Red, Color.Black, '+')));
        }
        if (health.bloodHP < 1 || health.bodyHP < 1) {
            Active = false;
            this.AddMessage(new InfoEvent("You have died"));
        }
        /*
        if(AllowUpdate()) {
            World.AddEffect(new RealtimeTrail(Position, 0.25, new ColoredGlyph('@', Color.White, Color.Black)));
        }
        */

        if (frameCounter > 0)
            frameCounter--;

        this.UpdateGravity();
        this.UpdateMotion();

        foreach (var a in Actions) {
            a.Update();
        }
        Actions.RemoveWhere(a => a.Done());

        foreach (var i in Inventory) {
            //Copy so that when the item updates motion, the change does not apply to the player
            i.Position = Position.copy;
            i.Velocity = Velocity.copy;
            i.UpdateStep();
        }

        Inventory.RemoveWhere(i => !i.Active);
        Watch.RemoveWhere(t => !t.Active);
        if (!this.OnGround())
            frameCounter = 20;

        //HistoryRecent.RemoveAll(e => e.ScreenTime < 1);
    }

    public void AddMessage(IPlayerMessage e) {
        var desc = e.Desc;
        if (HistoryLog.Count == 0) {
            var entry = new HistoryEntry(desc);
            HistoryLog.Add(entry);
            HistoryRecent.Add(entry);
        } else {
            var last = HistoryLog.Last();
            if (last._desc.ToString() == desc.ToString()) {
                last.times++;
                last.SetScreenTime();

                if (HistoryRecent.Any()) {
                    if (HistoryRecent.Last() != last) {
                        HistoryRecent.Remove(last);
                        HistoryRecent.Add(last);
                    }
                } else {
                    HistoryRecent.Add(last);
                }
            } else {
                var entry = new HistoryEntry(desc);
                HistoryLog.Add(entry);
                HistoryRecent.Add(entry);
            }
        }
    }

    public void OnDamaged(Damager source) {
        if (source is Bullet b) {
            AddMessage(new InfoEvent(new ColoredString($"You are hit by a bullet!", Color.Red, Color.Black)));
            health.Damage(b.damage);
        } else if (source is ExplosionDamage e) {
            health.Damage(e.damage);
            Velocity += e.knockback;
            AddMessage(new InfoEvent(new ColoredString($"You are caught in an explosion and take {e.damage} damage!", Color.Red, Color.Black)));
        } else if (source is Flame f) {
            health.Damage(f.damage);
            AddMessage(new InfoEvent(new ColoredString($"You are caught in flames!", Color.Orange, Color.Black)));
        } else if (source is Fire fire) {
            AddMessage(new InfoEvent(new ColoredString($"You step on fire!", Color.Orange, Color.Black)));
            health.Damage(1);
            World.AddEffect(new Burn(this, 20));
        }
    }

    public ColoredGlyph SymbolCenter => new ColoredString("@", Color.White, Color.Black)[0];
    public ColoredString Name => new ColoredString("Player", Color.White, Color.Black);
}
public class HistoryEntry {
    public ColoredString Desc => times == 1 ? _desc : (_desc + new ColoredString($" (x{times})", Color.White, Color.Black));
    public ColoredString _desc;
    public int times;
    public double ScreenTime;
    public HistoryEntry(ColoredString Desc, double ScreenTime = 4) {
        this._desc = Desc;
        this.ScreenTime = ScreenTime;
        this.times = 1;
    }
    public void SetScreenTime(double ScreenTime = 4) {
        this.ScreenTime = ScreenTime;
    }
}
