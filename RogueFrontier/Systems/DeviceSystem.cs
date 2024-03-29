using System;
using System.Collections.Generic;
using System.Linq;
namespace RogueFrontier;
public class Circuit {
    public List<Device> Installed=new();
    public List<Device> Powered=new();

    public List<Armor> Armor = new();
    public List<Engine> Engine = new();
    public List<Enhancer> Enhancer = new();
    public List<Launcher> Launcher = new();
    public List<Reactor> Reactor = new();
    public List<Shield> Shield = new();
    public List<Solar> Solar = new();
    public List<Weapon> Weapon = new();
    public Circuit() {}
    public void Install(IEnumerable<Device> Devices) {
        Installed.AddRange(Devices);
        Powered.AddRange(Devices.Where(d => d.powerUse>=0));

        Armor.AddRange(All<Armor>());
        Engine.AddRange(All<Engine>());
        Enhancer.AddRange(All<Enhancer>());
        Launcher.AddRange(All<Launcher>());
        Reactor.AddRange(All<Reactor>());
        Shield.AddRange(All<Shield>());
        Solar.AddRange(All<Solar>());
        Weapon.AddRange(All<Weapon>());

        IEnumerable<T> All<T>() where T : Device => Devices.OfType<T>();
    }
    public void Install(params Device[] Devices) =>
        Install(Devices.AsEnumerable());
    public void Remove(params Device[] Devices) {
        Installed.RemoveAll(Devices.Contains);
        Powered.RemoveAll(Devices.Where(d=>d.powerUse>=0).Contains);
        Armor.RemoveAll(All);
        Engine.RemoveAll(All);
        Enhancer.RemoveAll(All);
        Launcher.RemoveAll(All);
        Reactor.RemoveAll(All);
        Solar.RemoveAll(All);
        Weapon.RemoveAll(All);
        Shield.RemoveAll(All);
        bool All<T>(T t) where T : Device => Devices.OfType<T>().Contains(t);
    }
    public void Clear() {
        Installed.Clear();
        Powered.Clear();

        Armor.Clear();
        Engine.Clear();
        Enhancer.Clear();
        Launcher.Clear();
        Reactor.Clear();
        Solar.Clear();
        Shield.Clear();
        Weapon.Clear();
    }
    public void UpdateDevices() {
        Powered = Installed.Where(d=>d.powerUse>=0).ToList();
        Armor = All<Armor>();
        Engine = All<Engine>();
        Enhancer = All<Enhancer>();
        Launcher = All<Launcher>();
        Reactor = All<Reactor>();
        Shield = All<Shield>();
        Solar = All<Solar>();
        Weapon = All<Weapon>();

        List<T> All<T>() where T : Device => new(Installed.OfType<T>());
    }
    public void Update(double delta, IShip owner) {
        Installed.ForEach(d => d.Update(delta, owner));
    }
}
