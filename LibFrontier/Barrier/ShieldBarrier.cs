﻿using Common;
using LibGamer;
using System.Collections.Generic;

namespace RogueFrontier;
//Blocks projectiles from anyone but the player
class ShieldBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public Tile tile => new Tile(color, ABGR.Black, '*');
    public ulong id { get; private set; }
    public ActiveObject owner;
    public XY offset;
    public double lifetime;
    public HashSet<Projectile> blocked;
    public XY position { get; set; }

    public uint color;
    public ShieldBarrier(ActiveObject owner, XY offset, int lifetime, HashSet<Projectile> blocked, uint color = ABGR.DarkCyan) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.blocked = blocked;
        UpdatePosition();
        this.color = color;
    }
    public void Update(double delta) {
        if (owner.active) {
            lifetime -= delta * 60;
            UpdatePosition();
        } else {
            lifetime = 0;
        }
    }
    public void UpdatePosition() => position = owner.position + offset;
    public void Interact(Projectile other) {
        if (other.source == owner) {
            return;
        }
        if (blocked.Contains(other)) {
            return;
        }
        blocked.Add(other);
        other.lifetime = 0;
        other.damageHP = 0;

    }
}
class BubbleBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public Tile tile => new Tile(ABGR.DarkCyan, ABGR.Black, '*');
    public ulong id { get; private set; }
    public ActiveObject owner;
    public XY offset;
    public double lifetime;
    public HashSet<Projectile> blocked;
    public XY position { get; set; }
    public BubbleBarrier(ActiveObject owner, XY offset, int lifetime, HashSet<Projectile> blocked) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.blocked = blocked;
        UpdatePosition();
    }
    public void Update(double delta) {
        if (owner.active) {
            lifetime -= delta * 60;
            UpdatePosition();
        } else {
            lifetime = 0;
        }
    }
    public void UpdatePosition() => position = owner.position + offset;
    public void Interact(Projectile other) {
        if (blocked.Contains(other)) {
            return;
        }
        blocked.Add(other);
        other.lifetime = 0;
        other.damageHP = 0;

    }
}