using Common;
using Newtonsoft.Json;

namespace RogueFrontier;

public enum WingOrder {
    Escort,
    Wait,
    BreakAndAttack,
    Scout,
}
public class Wingmate : IShipBehavior, Ob<PlayerShip.Destroyed> {
    public PlayerShip player;
    public IShipOrder order;
    public void Observe(PlayerShip.Destroyed ev) {
        var (s, d, w) = ev;
        order = new AttackTarget(d);
    }
    //This class handles orders and communications
    public Wingmate(PlayerShip player) {
        this.player = player;
    }
    public void Update(double delta, AIShip owner) {
        if(order?.Active != true) {
            order = new EscortShip(player, new());
        }
        order?.Update(delta, owner);
    }
}
