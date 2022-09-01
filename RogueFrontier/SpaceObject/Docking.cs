﻿using Common;
using System;

namespace RogueFrontier;

public class Docking {
    public StructureObject Target;
    public XY Offset;
    public bool docked;
    public bool justDocked;

    public record OnDocked(IShip owner, Docking d);
    public Vi<OnDocked> onDocked = new();


    public Docking() { }
    public Docking(StructureObject Target) {
        this.Target = Target;
        this.Offset = new XY(0, 0);
    }
    public Docking(StructureObject Target, XY Offset) {
        this.Target = Target;
        this.Offset = Offset;
    }
    public void Update(double delta, IShip owner) {
        if (!docked) {
            if (docked = UpdateDocking(delta, owner)) {
                justDocked = true;

                onDocked.Observe(new(owner, this));
            }
        } else {
            owner.position = Target.position;
            owner.velocity = Target.velocity;
        }
    }
    public bool UpdateDocking(double delta, IShip ship) {
        double decel = ship.shipClass.thrust * Program.TICKS_PER_SECOND / 2;
        double stoppingTime = (ship.velocity - Target.velocity).magnitude / decel;
        double stoppingDistance = ship.velocity.magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
        var stoppingPoint = ship.position;
        if (!ship.velocity.isZero) {
            stoppingPoint += ship.velocity.normal * stoppingDistance;
        }

        var dest = Target.position + Offset;
        var offset = dest + (Target.velocity * stoppingTime) - stoppingPoint;

        if (offset.magnitude > 0.25) {
            ship.velocity += XY.Polar(offset.angleRad, ship.shipClass.thrust * delta * Program.TICKS_PER_SECOND);
        } else if ((ship.position - dest).magnitude2 < 1) {
            ship.velocity = Target.velocity;
            return true;
        }
        return false;
    }
}
