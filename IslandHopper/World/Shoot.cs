using Common;
using SadRogue.Primitives;
using SadConsole;
using System;

namespace IslandHopper;

public class ShootAction : EntityAction {
    private ICharacter player;
    public IItem item;
    public TargetMode targeting;
    private XYZ aim;    //Offset from the player. When player pos + aim is close enough to the target pos, we fire
                        //When creating this object, caller must remember to add the Reticles to the world
    public Reticle targetReticle;
    public Reticle aimReticle;
    //We need a way to cancel this action for the player and enemies
    public ShootAction(ICharacter player, IItem item, TargetMode target, XYZ aim = null) {
        this.player = player;
        this.item = item;
        this.targeting = target;
        this.aim = aim ?? new XYZ();
        targetReticle = new Reticle(Active, target.Position, Color.Yellow);
        aimReticle = new Reticle(Active, player.Position + this.aim, Color.Yellow);
        player.World.AddEffect(targetReticle);
        player.World.AddEffect(aimReticle);
        if (player is Player p) {
            p.Watch.Add(aimReticle);
        }
    }
    public void Update() {
        if (!targeting.Active()) {
            active = false;
            return;
        }
        if (!player.Inventory.Contains(item)) {
            active = false;
            return;
        }

        targetReticle.Position = targeting.Position;
        /*
        var aimAngle = aim.xyAngle;
        var targetOffset = (targetPos - player.Position);
        var targetAngle = targetOffset.xyAngle;
        var angleDiff = targetAngle - aimAngle;
        if (angleDiff > 180)
            angleDiff -= 360;
        if (angleDiff < -180)
            angleDiff += 360;

        var radiusDiff = targetOffset.Magnitude - aim.Magnitude;
        if (Math.Abs(angleDiff) > 5) {
            //Bring our aim closer to the target position
            var dir = angleDiff / Math.Abs(angleDiff);
            var delta = Math.Min(angleDiff, Math.Max(1, 1 / aim.Magnitude));
            aim = aim.RotateZ(delta * dir);
            aimReticle.Position = player.Position + aim;
        } else if(Math.Abs(radiusDiff) > 5) {
            var dir = radiusDiff / Math.Abs(radiusDiff);
            aim = aim.Extend(dir);
            aimReticle.Position = player.Position + aim;
        }
        */
        var targetOffset = targeting.Position - player.Position;

        /*
        if(target != null) {
            var bulletSpeed;
            var travelTime = targetOffset.Magnitude / bulletSpeed;
            targetOffset += target.Velocity * travelTime;
        }
        */

        var aimPos = player.Position + aim;
        var diff = targeting.Position.i - aimPos.i;

        bool needAdjust = diff.Magnitude > 0.5;

        /*
        var aimAngle = aim.xyAngle;
        var targetAngle = targetOffset.xyAngle;
        var angleDiff = targetAngle - aimAngle;
        if (angleDiff > 180)
            angleDiff -= 360;
        if (angleDiff < -180)
            angleDiff += 360;

        //If the user is skilled enough to aim this weapon, then we can fire earlier if the angle is right
        if(Math.Abs(angleDiff) < 5 && diff.Magnitude < targetOffset.Magnitude / 2) {

        }
        */
        if (item.Gun.GetState() == Gun.State.NeedsAmmo) {
            //TO DO
            //For now, we should just leave a message saying that the gun is out of ammo
            targeting.shotsLeft = 0;
            player.AddMessage(new InfoEvent(item.Name + new ColoredString(" is out of ammo!")));
        } else if (item.Gun.GetState() == Gun.State.NeedsReload) {
            //For now, we just reload if we need to
            item.Gun.Reload();

            player.AddMessage(new InfoEvent(player.Name + new ColoredString(" reloads ") + item.Name.WithBackground(Color.Black)));
        } else if (item.Gun.GetState() == Gun.State.Reloading) {
            //Don't allow aiming while we're reloading
        } else if (needAdjust) {
            //Bring our aim closer to the target position
            //aim += diff.Normal * Math.Min(diff.Magnitude, 1);
            //If the player is running towards/away from the target, adjust aim faster
            //var speed = Math.Abs(player.Velocity.Dot(targetOffset.Normal));
            //If the player is running, that shouldn't prevent them from aiming

            //Radial jitter to simulate difficulty of aiming at long range
            //var jitter = aim.Magnitude / 20f
            //var speed = player.Velocity.Magnitude - Math.Abs(player.Velocity.Dot(aim.Normal));

            //TO DO: We need to add jitter and inaccuracy based on difficulty
            var speed = player.Velocity.Magnitude;
            if (targeting.Target != null) {
                speed += targeting.Target.Velocity.Magnitude;
            }

            var maxDelta = Math.Max(10 / 30f, 2 * diff.Magnitude / 30 + speed);

            var delta = Math.Min(diff.Magnitude, maxDelta);

            aim += diff.Normal * delta;
            aimReticle.Position = player.Position + aim;
        } else if ((targeting.Position - player.Position).Magnitude > item.Gun.range) {
            player.AddMessage(new InfoEvent((targeting.Target?.Name ?? new ColoredString("Target")) + new ColoredString(" is out of range!")));
        } else if (item.Gun.GetState() == Gun.State.Ready) {
            //Close enough to fire
            item.Gun.Fire(player, item, targeting.Target, targeting.Position);
            targeting.shotsLeft--;
        }
    }
    private bool active = true;
    //Auto remove the reticles when we remove this action from the player
    public bool Active() => player.Active && player.Actions.Contains(this);
    public bool Done() => !active;

    public ColoredString Name => (new ColoredString($"{(item.Gun.ReloadTimeLeft > 0 ? "Reload" : item.Gun.FireTimeLeft > 0 ? "Fire" : "Aim"),-8}", Color.Cyan, Color.Black)
                        + item.Name);

}
public interface TargetMode {
    XYZ Position { get; }
    Entity Target { get; }
    ulong shotsLeft { get; set; }
    bool Active();
}
public class TargetEntity : TargetMode {
    public Entity Target { get; set; }
    public XYZ Position => Target.Position;
    public ulong shotsLeft { get; set; } = ulong.MaxValue;
    public bool Active() => Target.Active && shotsLeft != 0;
    public TargetEntity(Entity Target) {
        this.Target = Target;
    }
}
public class TargetPoint : TargetMode {
    public Entity Target => null;
    public XYZ Position { get; set; }
    public ulong shotsLeft { get; set; } = ulong.MaxValue;
    public bool Active() => shotsLeft != 0;
    public TargetPoint(XYZ Position) {
        this.Position = Position;
    }
}
