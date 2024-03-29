using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public class SelfDestruct : Event, Ob<PlayerShip.Destroyed> {
    public bool active { get; set; } = true;
    public PlayerShip target;
    public Message message;
    public double time;

    public SelfDestruct(PlayerShip target, double time) {
        this.target = target;
        this.time = time;
        message = new("");
        target.AddMessage(message);
        target.onDestroyed += this;
    }
    public void Update(double delta) {
        if (time > delta) {
            time -= delta;
            message.message.String = $"Self destructing in {(int)time} seconds.";
            target.AddMessage(message);
        } else {
            active = false;
            message.message.String = $"Self destructed completed.";
            target.AddMessage(message);

            target.Destroy(null);
            //target.Damage(new(target, new() { armorDrill = 1, shieldDrill = 1, damageHP = new Constant(int.MaxValue), effect=new() }, target.position, new()));
        }
    }
    public void Observe(PlayerShip.Destroyed d) {
        if(d.playerShip == target) {
            message.message.String = $"Self destruction complete.";
            target.AddMessage(message);
            active = false;
        }
    }
}
