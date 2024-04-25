using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public class RepairEffect : Event {
    public bool active { get; set; } = true;
    public int ticks;

    PlayerShip player;
    public Armor armor;
    int interval;
    int costPerHp;
    Action<RepairEffect> done;
    public bool terminated;

    public RepairEffect(PlayerShip player, Armor armor, int interval, int costPerHp, Action<RepairEffect> done) {
        this.player = player;
        this.armor = armor;
        this.interval = interval;
        this.costPerHp = costPerHp;
        this.done = done;
    }
    public void Update(double delta) {
        ticks++;
        if (ticks % interval != 0)
            return;
        if (armor.hp < armor.maxHP) {
            var next = player.person.money - costPerHp;
            if (next < 0) {
                terminated = true;
                Kill();
            } else {
                armor.hp++;
                player.person.money -= costPerHp;
            }
            return;
        }
        Kill();
        void Kill() {
            active = false;
            done?.Invoke(this);
        }
    }
}
public class RefuelEffect : Event {
    public bool active { get; set; } = true;
    public int ticks;

    PlayerShip player;
    public Reactor reactor;
    int interval;
    double costPerEnergy;
    double balance;
    Action<RefuelEffect> done;
    public bool terminated;

    public RefuelEffect(PlayerShip player, Reactor reactor, int interval, double costPerEnergy, Action<RefuelEffect> done)
    {
        this.player = player;
        this.reactor = reactor;
        this.interval = interval;
        this.costPerEnergy = costPerEnergy;
        this.done = done;
    }
    public void Update(double delta) {
        ticks++;
        if (ticks % interval != 0)
            return;
        if (reactor.energy < reactor.desc.capacity) {
            var next = player.person.money - costPerEnergy;
            if (next < 0) {
                terminated = true;
                Kill();
            } else {
                reactor.energy++;

                balance += costPerEnergy;
                var deltaBalance = (int)balance;
                balance -= deltaBalance;
                player.person.money -= deltaBalance;
            }
            return;
        }
        Kill();
        void Kill()
        {
            active = false;
            done?.Invoke(this);
        }
    }
}