using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public class EnergySystem {
    public HashSet<Device> GetEnabled(Circuit c) => c.Installed.Except(off).ToHashSet();
    public HashSet<Device> off = new();
    public int totalOutputMax;
    public int totalOutputUsed;
    public int totalOutputLeft => totalOutputMax - totalOutputUsed;
    public void Update(PlayerShip player) {
        var devices = player.devices;
        var reactors = devices.Reactor;
        if (!reactors.Any()) {
            return;
        }

        var solars = devices.Solar;
        var primary = new List<Reactor>();
        var secondary = new List<Reactor>();
        foreach(var s in solars) {
            s.energyDelta = 0;
        }
        foreach (var r in reactors) {
            r.energyDelta = 0;
            if (r.desc.rechargeable) {
                secondary.Add(r);
            } else {
                primary.Add(r);
            }
        }
        List<PowerSource> sources = new();
        sources.AddRange(solars);
        sources.AddRange(primary);
        sources.AddRange(secondary);

        totalOutputMax = sources.Sum(r => r.maxOutput);
        totalOutputUsed = 0;
        int sourceIndex = 0;
        int sourceOutput = sources[sourceIndex].maxOutput;
        var overloaded = new HashSet<Device>();
        var deactivated = new HashSet<Device>();
        //Devices consume power
        int outputUsed = 0;
        foreach (var powered in devices.Powered.Except(off)) {
            Handle(powered, overloaded);
        }
        foreach (var powered in overloaded) {
            powered.OnOverload(player);
            Handle(powered, deactivated);
        }

        void Handle(Device powered, HashSet<Device> overflow) {
            if(powered is Service s && s.desc.type == ServiceType.grind) {
                int i = 0;
            }
            var powerUse = powered.powerUse;
            if (powerUse <= 0) { return; }
            if (powerUse > totalOutputLeft) {
                overflow.Add(powered);
                return;
            }
            totalOutputUsed += powerUse;
            outputUsed += powerUse;
        CheckReactor:
            var source = sources[sourceIndex];
            //var efficiencyFactor = 1.0;

            //var ApplyOutput = (int delta) => source.energyDelta = delta / efficiencyFactor;
            var ApplyOutput = (int delta) => { source.energyDelta = delta; };
            if (source is Reactor r) {
                if (r.desc.rechargeable) {
                    r.rechargeDelay = 60;
                }
                if(powered is Weapon) {
                    ApplyOutput = (int delta) => {
                        r.energyDelta = delta;
                        r.combatEnergyDelta += delta;
                    };
                    //efficiencyFactor *= r.desc.combatFactor;

                }
            }
            if (outputUsed > sourceOutput) {
                outputUsed -= sourceOutput;
                ApplyOutput(-sourceOutput);
                //Go to the next source
                sourceIndex++;
                sourceOutput = sources[sourceIndex].maxOutput;
                goto CheckReactor;
            } else {
                ApplyOutput(-outputUsed);
            }
        }
        if (deactivated.Any()) {
            foreach(var d in deactivated) {
                d.OnDisable();
                off.Add(d);
            }
            player.AddMessage(new Message("Reactor output overload!"));
            foreach (var d in deactivated) {
                player.AddMessage(new Message($"{d.source.type.name} deactivated!"));
            }
        }
        //bool solarRechargeOnly = true;
        /*
        if(sources[sourceIndex] is Reactor reactor && (reactor.desc.battery || solarRechargeOnly)) {
            return;
        }
        */
        //Batteries recharge from reactor
        int maxGeneratorOutputLeft = totalOutputLeft - secondary.Sum(b => b.maxOutput);
        /*
        if (solarRechargeOnly) {
            maxGeneratorOutputLeft -= burners.Sum(b => b.maxOutput + (int)b.energyDelta);
        }
        */
        if (maxGeneratorOutputLeft <= 0) {
            return;
        }
        foreach (var battery in secondary.Where(b => b.energy < b.desc.capacity)) {
            if (battery.rechargeDelay > 0) {
                battery.rechargeDelay--;
                continue;
            }

            int delta = Math.Min(battery.desc.maxInput, maxGeneratorOutputLeft);
            battery.energyDelta = delta;

            totalOutputUsed += delta;
            outputUsed += delta;
            maxGeneratorOutputLeft -= delta;

        CheckReactor:

            if(sources[sourceIndex] is Reactor r && r.desc.rechargeable) {
                sourceIndex++;
                sourceOutput = sources[sourceIndex].maxOutput;
                goto CheckReactor;
            }

            if (outputUsed > sourceOutput) {
                outputUsed -= sourceOutput;
                sources[sourceIndex].energyDelta = -sourceOutput;

                //Go to the next reactor
                sourceIndex++;
                sourceOutput = sources[sourceIndex].maxOutput;
                goto CheckReactor;
            }

            sources[sourceIndex].energyDelta = -outputUsed;


            if (maxGeneratorOutputLeft == 0) {
                break;
            }
        }
    }
}
