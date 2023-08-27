﻿using SadRogue.Primitives;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;
using Common;
using ArchConsole;
using SFML.Audio;
using System.ComponentModel.DataAnnotations;
using CloudJumper;

namespace RogueFrontier;
public static partial class SMenu {
    public static char indexToLetter(int index) {
        if (index < 26) {
            return (char)('a' + index);
        } else {
            return '\0';
        }
    }
    public static int letterToIndex(char ch) {
        ch = char.ToLower(ch);
        if (ch >= 'a' && ch <= 'z') {
            return (ch - 'a');
        } else {
            return -1;
        }
    }
    public static char indexToKey(int index) {
        //0 is the last key; 1 is the first
        if (index < 10) {
            return (char)('0' + (index + 1) % 10);
        } else {
            index -= 10;
            if (index < 26) {
                return (char)('a' + index);
            } else {
                return '\0';
            }
        }
    }
    public static int keyToIndex(char ch) {
        //0 is the last key; 1 is the first
        if (ch >= '0' && ch <= '9') {
            return (ch - '0' + 9) % 10;
        } else {
            ch = char.ToLower(ch);
            if (ch >= 'a' && ch <= 'z') {
                return (ch - 'a') + 10;
            } else {
                return -1;
            }
        }
    }
    public static ListMenu<IPlayerMessage> Logs(ScreenSurface prev, PlayerShip player) {
        ListMenu<IPlayerMessage> screen = null;
        List<IPlayerMessage> logs = player.logs;
        return screen = new(prev,
        player,
            $"{player.name}: Logs",
            logs,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(IPlayerMessage i) => i switch {
            Message m => m.text,
            Transmission t => $"{t.text}",
            _ => throw new NotImplementedException()
        };
        List<ColoredString> GetDesc(IPlayerMessage i) {
            return i switch {
                Message m => new(),
                Transmission t => new() {
                    new ColoredString("Source: ") + (t.source as ActiveObject)?.name ?? new("N/A"),
                },
                _ => throw new NotImplementedException()
            };
        }
        void Invoke(IPlayerMessage item) {
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<IPlayerInteraction> Missions(ScreenSurface prev, PlayerShip player, PlayerStory story) {
        ListMenu<IPlayerInteraction> screen = null;
        List<IPlayerInteraction> missions = new();
        void UpdateList() {
            missions.Clear();
            missions.AddRange(story.mainInteractions);
            missions.AddRange(story.secondaryInteractions);
            missions.AddRange(story.completedInteractions);
        }
        UpdateList();
        return screen = new(prev,
            player,
            $"{player.name}: Missions",
            missions,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );
        string GetName(IPlayerInteraction i) => i switch {
            DestroyTarget dt => "Destroy Target",
            _ => "Mission"
        };
        List<ColoredString> GetDesc(IPlayerInteraction i) {
            List<ColoredString> result = new();
            switch (i) {
                case DestroyTarget dt:
                    if (dt.complete) {
                        result.Add(new($"Mission complete"));
                        result.Add(new($"Return to {dt.source.name}"));
                    } else {
                        result.Add(new("Destroy the following targets:"));
                        foreach (var t in dt.targets) {
                            result.Add(new($"- {t.name}"));
                        }
                        result.Add(new(""));
                        result.Add(new($"Source: {dt.source.name}"));
                    }
                    result.Add(new(""));
                    result.Add(new($"[Enter] Update targets", Color.Yellow, Color.Black));
                    break;
            }
            return result;
        }
        void Invoke(IPlayerInteraction item) {
            screen.list.UpdateIndex();
            switch (item) {
                case DestroyTarget dt:
                    var a = dt.targets.Where(t => t.active);
                    player.SetTargetList(a.Any() ? a.ToList() : new() { dt.source });
                    player.AddMessage(new Message("Targeting updated"));
                    break;
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static List<ColoredString> GenerateDesc(ItemType t) {
        var result = new List<ColoredString>();
        var desc = t.desc.SplitLine(64);
        if (desc.Any()) {
            result.AddRange(desc.Select(Main.ToColoredString));
            result.Add(new(""));
        }
        return result;
    }
    public static List<ColoredString> GenerateDesc(Item i) => GenerateDesc(i.type);
    public static List<ColoredString> GenerateDesc(Device d) {
        var r = GenerateDesc(d.source.type);
        ((Action)(d switch {
            Weapon w => () => {

                r.AddRange(new ColoredString[] {
                    new($"Damage range: {w.desc.Projectile.damageHP.str}"),
                    new($"Fire cooldown:{w.desc.fireCooldown/60.0:0.00} SEC"),
                    new($"Power rating: {w.desc.powerUse}"),
                    w.desc.recoil != 0 ?
                        new($"Recoil force: {w.desc.recoil}") : null,
                    w.ammo switch {
                        ItemAmmo ia =>      new($"Ammo type:    {ia.itemType.name}"),
                        ChargeAmmo ca =>    new($"Charges left: {ca.charges}"),
                        _ => null
                    },
                    w.aiming switch {
                        Omnidirectional =>  new($"Turret:       Omnidirectional"),
                        Swivel s =>         new($"Turret:       {(int)((s.leftRange + s.rightRange) * 180 / Math.PI)}-degree swivel"),
                        _ => null
                    }
                }.Except(new ColoredString[] { null }));
            },
            Shield s => () => {

                r.AddRange(new ColoredString[] {
                    new(        $"Max HP:  {s.desc.maxHP} HP"),
                    new(        $"Regen:   {s.desc.regen:0.00} HP/s"),
                    new(        $"Stealth: {s.desc.stealth}"),
                    new(        $"Idle power use:  {s.desc.idlePowerUse}"),
                    new(        $"Regen power use: {s.desc.powerUse}"),
                    s.desc.reflectFactor is > 0 and var reflectFactor ?
                        new(  $"Reflect factor:  {reflectFactor}") : null,
                }.Except(new ColoredString[] {null}));
            }
            ,
            Solar solar => () => {
                r.AddRange(new ColoredString[] {
                    new($"Peak output:    {solar.maxOutput} EL"),
                    new($"Current output: {solar.energyDelta} EL")
                });
            }
            ,
            Reactor reactor => () => {
                r.AddRange(new ColoredString[] {
                    new($"Peak output:     {reactor.maxOutput, -4} EL"),
                    new($"Current output:  {-reactor.energyDelta, -4} EL"),


                    new($"Energy capacity: {reactor.desc.capacity, -4} EN"),
                    new($"Energy content:  {(int)reactor.energy, -4} EN"),
                    new($"Efficiency:      {reactor.efficiency, -4} EL/EN"),

                });
            },
            Armor armor => () => {
                r.AddRange(new ColoredString[] {
                    new($"Max HP: {armor.maxHP}"),

                });
            },

            _ => () => { }
        })).Invoke();
        r.Add(new(""));
        return r;
    }
    public static ListMenu<Item> Usable(ScreenSurface prev, PlayerShip player) {
        ListMenu<Item> screen = null;
        IEnumerable<Item> cargoInvokable;
        IEnumerable<Item> installedInvokable;
        List<Item> usable = new();
        void UpdateList() {
            cargoInvokable = player.cargo.Where(i => i.type.Invoke != null);
            installedInvokable = player.devices.Installed.Select(d => d.source).Where(i => i.type.Invoke != null);
            usable.Clear();
            usable.AddRange(installedInvokable.Concat(cargoInvokable));
        }
        UpdateList();
        return screen = new(prev,
            player,
            $"{player.name}: Useful Items",
            usable,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installedInvokable.Contains(i) ? "[*] " : "[c] ")}{i.type.name}";
        List<ColoredString> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = GenerateDesc(i);
            if (invoke != null) {
                string action = $"[Enter] {invoke.GetDesc(player, i)}";
                result.Add(new(action, Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item item) {
            item.type.Invoke?.Invoke(screen, player, item, Update);
            screen.list.UpdateIndex();
        }
        void Update() {
            UpdateList();
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            if (prev != null && prev != p) {
                p.Children.Add(prev);
            }
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> Installed(ScreenSurface prev, PlayerShip player) {
        ListMenu<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(prev,
            player,
            $"{player.name}: Device System",
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );
        string GetName(Device d) => d.source.type.name;
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(d);

            if (invoke != null) {
                result.Add(new($"[Enter] {invoke.GetDesc(player, item)}", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeDevice(Device d) {
            var item = d.source;
            var invoke = item.type.Invoke;
            invoke?.Invoke(screen, player, item);
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Item> Cargo(ScreenSurface prev, PlayerShip player) {
        ListMenu<Item> screen = null;
        var items = player.cargo;
        return screen = new ListMenu<Item>(prev,
            player,
            $"{player.name}: Cargo",
            items,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => i.type.name;
        List<ColoredString> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = GenerateDesc(i);
            if (invoke != null) {
                result.Add(new($"[Enter] {invoke.GetDesc(player, i)}", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item item) {
            var invoke = item.type.Invoke;
            invoke?.Invoke(screen, player, item);
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> DeviceManager(ScreenSurface prev, PlayerShip player) {
        ListMenu<Device> screen = null;
        var disabled = player.energy.off;
        var powered = player.devices.Powered;
        return screen = new(prev,
            player,
            $"{player.name}: Device Power",
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );
        string GetName(Device d) => $"{(disabled.Contains(d) ? "[ ]" : "[*]")} {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var result = GenerateDesc(d);
            result.Add(new($"Status: {(disabled.Contains(d) ? "OFF" : "ON")}"));
            result.Add(new(""));
            var off = disabled.Contains(d);
            var word = (off ? "Enable" : "Disable");
            result.Add(new($"[Enter] {word} this device", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Device p) {
            if (disabled.Contains(p)) {
                disabled.Remove(p);
                player.AddMessage(new Message($"Enabled {p.source.type.name}"));
            } else {
                disabled.Add(p);
                p.OnDisable();
                player.AddMessage(new Message($"Disabled {p.source.type.name}"));
            }
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> RemoveDevice(ScreenSurface prev, PlayerShip player) {
        ListMenu<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(prev,
            player,
            $"{player.name}: Device Removal",
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );
        string GetName(Device d) => d.source.type.name;
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(d);
            if (invoke != null) {
                result.Add(new($"[Enter] Remove this device", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeDevice(Device device) {
            var item = device.source;
            player.devices.Remove(device);
            player.cargo.Add(item);
            screen.list.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Armor> RepairArmorFromItem(ScreenSurface prev, PlayerShip player, Item source, RepairArmor repair, Action callback) {
        ListMenu<Armor> screen = null;
        Sound s = new();
        var devices = (player.hull as LayeredArmor).layers;

        return screen = new(prev,
            player,
            $"{player.name}: Armor Repair",
            devices,
            GetName,
            GetDesc,
            Repair,
            Escape
            ) { IsFocused = true };

        string GetName(Armor a) => $"{$"[{a.hp} / {a.maxHP}]",-12}{a.source.type.name}";
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(a);
            if (a.desc.RestrictRepair?.Matches(source) == false) {
                result.Add(new("This armor is not compatible", Color.Yellow, Color.Black));
            } else if (a.hp < a.maxHP) {
                result.Add(new("[Enter] Repair this armor", Color.Yellow, Color.Black));
            } else if(a.maxHP < a.desc.maxHP) {
                result.Add(new("This armor cannot be repaired any further", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This armor is at full HP", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Repair(Armor a) {
            if (a.desc.RestrictRepair?.Matches(source) == false) {
                return;
            }
            var before = a.hp;
            var repairHP = Math.Min(repair.repairHP, a.maxHP - a.hp);
            if (repairHP > 0) {
                a.hp += repairHP;
                player.cargo.Remove(source);
                player.AddMessage(new Message($"Used {source.type.name} to restore {repairHP} hp on {a.source.type.name}"));

                callback?.Invoke();
                Escape();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Reactor> RefuelFromItem(ScreenSurface prev, PlayerShip player, Item source, Refuel refuel, Action callback) {
        ListMenu<Reactor> screen = null;
        var devices = player.devices.Reactor;
        return screen = new(prev,
            player,
            $"{player.name}: Refuel Reactor",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            result.Add(new($"Refuel amount: {refuel.energy}"));
            result.Add(new($"Fuel needed:   {r.desc.capacity - (int)r.energy}"));
            result.Add(new(""));


            if(!r.desc.allowRefuel) {
                result.Add(new("This reactor does not accept fuel", Color.Yellow, Color.Black));
            } else if (r.energy < r.desc.capacity) {
                result.Add(new("[Enter] Refuel", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This reactor is full", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            var before = r.energy;
            var refuelEnergy = Math.Min(refuel.energy, r.desc.capacity - r.energy);

            if (refuelEnergy > 0) {
                r.energy += refuelEnergy;
                player.cargo.Remove(source);
                player.AddMessage(new Message($"Used {source.type.name} to refuel {refuelEnergy:0} energy on {r.source.type.name}"));

                callback?.Invoke();
                Escape();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> ReplaceDeviceFromItem(ScreenSurface prev, PlayerShip player, Item source, ReplaceDevice replace, Action callback) {
        ListMenu<Device> screen = null;
        var devices = player.devices.Installed.Where(i => i.source.type == replace.from);
        return screen = new(prev,
            player,
            $"{player.name}: Device Replacement",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Device d) => $"{d.source.type.name}";
        List<ColoredString> GetDesc(Device r) {
            var item = r.source;
            var result = GenerateDesc(r);
            result.Add(new("Replace this device", Color.Yellow, Color.Black));
            return result;
        }
        void Invoke(Device d) {
            d.source.type = replace.to;
            switch (d) {
                case Weapon w: w.SetWeaponDesc(replace.to.Weapon); break;
                default:
                    throw new Exception("Unsupported ReplaceDevice type");
            }
            player.AddMessage(new Message($"Used {source.type.name} to replace {d.source.type.name} with {replace.to.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Weapon> RechargeWeaponFromItem(ScreenSurface prev, PlayerShip player, Item source, RechargeWeapon recharge, Action callback) {
        ListMenu<Weapon> screen = null;
        var devices = player.devices.Weapon.Where(i => i.desc == recharge.weaponType);
        return screen = new(prev,
            player,
            $"{player.name}: Recharge Weapon",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Weapon w) => $"{w.source.type.name}";
        List<ColoredString> GetDesc(Weapon w) {
            var item = w.source;
            var result = GenerateDesc(w);
            result.Add(new("Recharge this weapon", Color.Yellow, Color.Black));
            return result;
        }
        void Invoke(Weapon d) {
            var c = (d.ammo as ChargeAmmo);
            c.charges += recharge.charges;
            player.AddMessage(new Message($"Recharged {d.source.type.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<ItemType> Workshop(ScreenSurface prev, PlayerShip player, Dictionary<ItemType, Dictionary<ItemType, int>> recipes, Action callback) {
        ListMenu<ItemType> screen = null;
        var listing = new Dictionary<ItemType, Dictionary<ItemType, HashSet<Item>>>();
        var available = new Dictionary<ItemType, bool>();
        Calculate();
        void Calculate() {
            foreach ((var result, var rec) in recipes) {
                var components = new Dictionary<ItemType, HashSet<Item>>();
                foreach (var compType in rec.Keys) {
                    components[compType] = new();
                }
                foreach (var item in player.cargo) {
                    var type = item.type;
                    if (components.TryGetValue(type, out var set)) {
                        set.Add(item);
                    }
                }
                available[result] = rec.All(pair => components[pair.Key].Count >= pair.Value);
                listing[result] = components;
            }
        }
        return screen = new(prev,
            player,
            $"Workshop",
            recipes.Keys,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };
        string GetName(ItemType type) => $"{type.name}";
        List<ColoredString> GetDesc(ItemType type) {
            var result = GenerateDesc(type);
            var rec = recipes[type];
            foreach ((var compType, var minCount) in rec) {
                var count = listing[type][compType].Count;
                result.Add(new($"{compType.name}: {count} / {minCount}", count >= minCount ? Color.Yellow : Color.Gray, Color.Black));
            }
            result.Add(new(""));

            if (available[type]) {
                result.Add(new("[Enter] Fabricate this item", Color.Yellow, Color.Black));
            } else {
                result.Add(new("Additional materials required", Color.Yellow, Color.Black));
            }

        Done:
            return result;
        }
        void Invoke(ItemType type) {
            if (available[type]) {
                foreach ((var compType, var minCount) in recipes[type]) {
                    player.cargo.ExceptWith(listing[type][compType].Take(minCount));
                }
                player.cargo.Add(new(type));

                Calculate();
                callback?.Invoke();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Reactor> DockReactorRefuel(ScreenSurface prev, PlayerShip player, Func<Reactor, int> GetPrice, Action callback) {
        ListMenu<Reactor> screen = null;
        var reactors = player.devices.Reactor;
        RefuelEffect job = null;
        return screen = new(prev,
            player,
            $"Refuel Service",
            reactors,
            GetName,
            GetDesc, Invoke, Escape) { IsFocused = true };
        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            int unitPrice = GetPrice(r);
            if (unitPrice < 0) {
                result.Add(new("Refuel services not available for this reactor", Color.Yellow, Color.Black));
                return result;
            }
            var delta = r.desc.capacity - (int)r.energy;
            result.Add(new($"Fuel needed: {delta}"));
            result.Add(new($"Total cost:  {unitPrice * delta}"));
            result.Add(new($"Your money:  {player.person.money}"));
            result.Add(new(""));
            if (delta <= 0) {
                result.Add(new("This reactor is full", Color.Yellow, Color.Black));
            } else if (job?.active == true) {
                if (job.reactor == r) {
                    result.Add(new("This reactor is currently refueling.", Color.Yellow, Color.Black));
                } else {
                    result.Add(new("Please wait for current refuel job to finish.", Color.Yellow, Color.Black));
                }
            } else if (unitPrice > player.person.money) {
                result.Add(new($"You cannot afford refueling", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"[Enter] Order refueling", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            if (job?.active == true) {
                return;
            }
            int delta = r.desc.capacity - (int)r.energy;
            if (delta == 0) {
                return;
            }
            int unitPrice = GetPrice(r);
            if (unitPrice < 0) {
                return;
            }
            int price = delta * unitPrice;
            if (unitPrice > player.person.money) {
                return;
            }
            job = new RefuelEffect(player, r, 6, unitPrice, Done);
            player.world.AddEvent(job);
            player.AddMessage(new Message($"Refuel job initiated..."));
            callback?.Invoke();
        }
        void Done(RefuelEffect r) {
            player.world.RemoveEvent(r);
            player.AddMessage(new Message($"Refuel job {(r.terminated ? "terminated" : "completed")}"));
        }
        void Escape() {
            if (job?.active == true) {
                job.active = false;
                player.world.RemoveEvent(job);
                job = null;
                player.AddMessage(new Message($"Refuel job canceled"));
                return;
            }
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Armor> DockArmorRepair(ScreenSurface prev, PlayerShip player, Func<Armor, int> GetPrice, Action callback) {
        ListMenu<Armor> screen = null;
        var layers = (player.hull as LayeredArmor)?.layers ?? new();
        RepairEffect job = null;

        Sound s = new();
        SoundBuffer
            start = new("RogueFrontierContent/sounds/repair_start.wav"),
            stop = new("RogueFrontierContent/sounds/repair_stop.wav");


        return screen = new(prev,
            player,
            $"Armor Repair",
            layers,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Armor a) {
            var BAR = 8;
            int available = BAR * Math.Min(a.maxHP, a.desc.maxHP) / Math.Max(1, a.desc.maxHP);

            int active = available * Math.Min(a.hp, a.maxHP) / Math.Max(1, a.maxHP);

            return $"[{new string('=', active)}{new string('.', available - active)}{new string(' ', BAR - available)}] [{a.hp}/{a.maxHP}] {a.source.type.name}";
        }
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;
            var result = GenerateDesc(a);
            int unitPrice = GetPrice(a);
            if (unitPrice < 0) {
                result.Add(new("Repair services not available for this armor", Color.Yellow, Color.Black));
                return result;
            }
            int delta = a.maxHP - a.hp;
            result.Add(new($"Price per HP: {unitPrice}"));
            result.Add(new($"HP to repair: {delta}"));
            result.Add(new($"Your money:   {player.person.money}"));
            result.Add(new($"Full price:   {unitPrice * delta}"));
            result.Add(new(""));
            if (delta <= 0) {
                if(a.maxHP == 0) {
                    result.Add(new("This armor cannot be repaired.", Color.Yellow, Color.Black));
                } else if (a.maxHP < a.desc.maxHP) {
                    result.Add(new("This armor cannot be repaired any further.", Color.Yellow, Color.Black));
                } else {
                    result.Add(new("This armor is at full HP.", Color.Yellow, Color.Black));
                }
                goto Done;
            }
            if (job?.active == true) {
                if (job.armor == a) {
                    result.Add(new("This armor is currently under repair.", Color.Yellow, Color.Black));
                } else {
                    result.Add(new("Another armor is currently under repair.", Color.Yellow, Color.Black));
                }
                goto Done;
            }
            if (unitPrice > player.person.money) {
                result.Add(new($"You cannot afford repairs", Color.Yellow, Color.Black));
                goto Done;
            }
            result.Add(new($"[Enter] Order repairs", Color.Yellow, Color.Black));

        Done:
            return result;
        }
        void Invoke(Armor a) {
            if (job?.active == true) {
                if (job.armor == a) {
                    job.active = false;
                    player.AddMessage(new Message($"Repair job terminated."));

                    s.SoundBuffer = stop;
                    s.Play();
                }
                return;
            }
            int delta = a.maxHP - a.hp;
            if (delta == 0) {
                return;
            }
            int unitPrice = GetPrice(a);
            if (unitPrice < 0) {
                return;
            }


            if (unitPrice > player.person.money) {
                return;
            }
            job = new RepairEffect(player, a, 2, unitPrice, Done);
            player.world.AddEvent(job);
            player.AddMessage(new Message($"Repair job initiated..."));


            s.SoundBuffer = start;
            s.Play();

            callback?.Invoke();
        }
        void Done(RepairEffect r) {
            player.world.RemoveEvent(r);
            player.AddMessage(new Message($"Repair job {(r.terminated ? "terminated" : "completed")}"));

            s.SoundBuffer = stop;
            s.Play();
        }
        void Escape() {
            if (job?.active == true) {
                job.active = false;
                player.world.RemoveEvent(job);
                job = null;
                player.AddMessage(new Message($"Repair job canceled"));


                s.SoundBuffer = stop;
                s.Play();
                return;
            }
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> DockDeviceRemoval(ScreenSurface prev, PlayerShip player, Func<Device, int> GetPrice, Action callback) {
        ListMenu<Device> screen = null;
        var installed = player.devices.Installed;
        return screen = new(prev,
            player,
            $"Device Removal",
            installed,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Device d) => $"{d.GetType().Name.PadRight(7)}: {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var result = GenerateDesc(d);
            int unitPrice = GetPrice(d);
            if (unitPrice < 0) {
                result.Add(new("Removal service is not available for this device", Color.Yellow, Color.Black));
            } else {

                result.Add(new($"Removal fee: {unitPrice}"));
                result.Add(new($"Your money:  {player.person.money}"));
                result.Add(new(""));
                if (unitPrice > player.person.money) {
                    result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(new($"Remove device", Color.Yellow, Color.Black));
                }
            }
            return result;
        }
        void Invoke(Device a) {
            var price = GetPrice(a);
            if (price < 0) {
                return;
            }
            ref var money = ref player.person.money;
            if (price > money) {
                return;
            }
            money -= price;
            player.devices.Remove(a);
            player.cargo.Add(a.source);
            player.AddMessage(new Message($"Removed {GetName(a)}"));
            callback?.Invoke();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Device> DockDeviceInstall(ScreenSurface prev, PlayerShip player, Func<Device, int> GetPrice, Action callback) {
        ListMenu<Device> screen = null;
        var cargo = player.cargo.Select(i =>
            i.engine ?? i.reactor ?? i.service ?? i.shield ?? (Device)i.solar ?? i.weapon)
            .Except(new Device[] { null });
        return screen = new(prev,
            player,
            $"Device Install",
            cargo,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Device d) => $"{d.GetType().Name.PadRight(7)}: {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var result = GenerateDesc(d);
            if (d is Weapon && player.shipClass.restrictWeapon?.Matches(d.source) == false) {
                result.Add(new("This weapon is not compatible", Color.Yellow, Color.Black));
                return result;
            }

            int price = GetPrice(d);
            if (price < 0) {
                result.Add(new(""));
                result.Add(new("Install service is not available for this device", Color.Yellow, Color.Black));
                return result;
            }


            result.Add(new($"Install fee: {price}"));
            result.Add(new($"Your money:  {player.person.money}"));
            result.Add(new(""));
            if (price > player.person.money) {
                result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"Install device", Color.Yellow, Color.Black));
            }

            return result;
        }
        void Invoke(Device d) {
            var price = GetPrice(d);
            if (price < 0) {
                return;
            }
            ref var money = ref player.person.money;
            if (price > money) {
                return;
            }
            money -= price;

            player.cargo.Remove(d.source);
            player.devices.Install(d);

            player.AddMessage(new Message($"Installed {GetName(d)}"));
            callback?.Invoke();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Armor> DockArmorReplacement(ScreenSurface prev, PlayerShip player, Func<Armor, int> GetPrice, Action callback) {
        ListMenu<Armor> screen = null;
        var armor = (player.hull as LayeredArmor)?.layers ?? new List<Armor>();
        return screen = new(prev,
            player,
            $"Armor Replacement",
            armor,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Armor a) => $"{a.source.type.name}";
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;
            var result = GenerateDesc(a);
            int price = GetPrice(a);
            if (price < 0) {
                result.Add(new("Removal service is not available for this armor", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"Your money:  {player.person.money}"));
                result.Add(new($"Removal fee: {price}"));
                result.Add(new(""));
                if (price > player.person.money) {
                    result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(new($"Select replacement", Color.Yellow, Color.Black));
                }
            }
            return result;
        }
        void Invoke(Armor removed) {
            var removalPrice = GetPrice(removed);
            if (removalPrice < 0) {
                return;
            }
            ref var money = ref player.person.money;
            if (removalPrice > money) {
                return;
            }


            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(GetReplacement(prev));
            ListMenu<Armor> GetReplacement(ScreenSurface prev) {
                ListMenu<Armor> screen = null;
                var armor = player.cargo.Select(i => i.armor).Where(i => i != null);
                return screen = new(prev,
                    player,
                    $"Armor Replacement (continued)",
                    armor,
                    GetName,
                    GetDesc,
                    Invoke,
                    Escape
                    ) { IsFocused = true };
                string GetName(Armor a) => $"{a.source.type.name}";
                List<ColoredString> GetDesc(Armor a) {
                    var item = a.source;
                    var result = GenerateDesc(a);
                    if (player.shipClass.restrictArmor?.Matches(a.source) == false) {
                        result.Add(new("This armor is not compatible", Color.Yellow, Color.Black));
                        return result;
                    }
                    int installPrice = GetPrice(a);
                    if (installPrice < 0) {
                        result.Add(new("Install service is not available for this armor", Color.Yellow, Color.Black));
                        return result;
                    }
                    var totalCost = removalPrice + installPrice;
                    result.Add(new($"Your money:  {player.person.money}"));
                    result.Add(new($"Removal fee: {removalPrice}"));
                    result.Add(new($"Install fee: {installPrice}"));
                    result.Add(new($"Total cost:  {totalCost}"));
                    result.Add(new(""));
                    if (totalCost > player.person.money) {
                        result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                        return result;
                    }
                    result.Add(new($"Replace with this armor", Color.Yellow, Color.Black));
                    return result;
                }
                void Invoke(Armor installed) {
                    if (player.shipClass.restrictArmor?.Matches(installed.source) == false) {
                        return;
                    }
                    var pr = GetPrice(installed);
                    if (pr < 0) {
                        return;
                    }
                    var price = removalPrice + pr;
                    ref var money = ref player.person.money;
                    if (price > money) {
                        return;
                    }
                    money -= price;
                    var l = ((LayeredArmor)player.hull).layers;
                    l[l.IndexOf(removed)] = installed;
                    player.cargo.Add(removed.source);
                    player.cargo.Remove(installed.source);

                    callback?.Invoke();

                    Escape();
                }
                void Escape() {
                    var p = screen.Parent;
                    p.Children.Remove(screen);
                    p.Children.Add(prev);
                    p.IsFocused = true;
                }
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Item> SetMod(ScreenSurface prev, PlayerShip player, Item source, Modifier mod, Action callback) {
        ListMenu<Item> screen = null;
        IEnumerable<Item> cargo;
        IEnumerable<Item> installed;
        List<Item> all = new();
        void UpdateList() {
            cargo = player.cargo;
            installed = player.devices.Installed.Select(d => d.source);
            all.Clear();
            all.AddRange(installed.Concat(cargo));
            all.Remove(source);
        }
        UpdateList();

        return screen = new(prev,
            player,
            $"{player.name}: Item Modify",
            all,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installed.Contains(i) ? "[*] " : "[c] ")}{i.type.name}";
        List<ColoredString> GetDesc(Item i) {
            var result = GenerateDesc(i);
            result.Add(new("[Enter] Apply modifier", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Item i) {
            i.mod = mod;
            player.cargo.Remove(source);
            player.AddMessage(new Message($"Applied {source.name} to {i.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListMenu<Reactor> RefuelReactor(ScreenSurface prev, PlayerShip player) {
        ListMenu<Reactor> screen = null;
        var devices = player.devices.Reactor;
        return screen = new(prev,
            player,
            $"{player.name}: Refuel",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            var a = (string s) => result.Add(new(s));
            if (r.energy < r.desc.capacity) {
                result.Add(new("[Enter] Refuel this reactor", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This reactor is at full capacity", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            if (r.energy < r.desc.capacity) {
                var p = screen.Parent;
                p.Children.Remove(screen);
                p.Children.Add(ChooseFuel(prev, player));
            }
            ListMenu<Item> ChooseFuel(ScreenSurface prev, PlayerShip player) {
                ListMenu<Item> screen = null;
                var items = player.cargo.Where(i => i.type.Invoke is Refuel r);
                return screen = new(prev, player, $"{player.name}: Refuel (continued)", items,
                    GetName, GetDesc, Invoke, Escape
                    ) { IsFocused = true };
                string GetName(Item i) => i.type.name;
                List<ColoredString> GetDesc(Item i) {
                    var result = GenerateDesc(i);
                    result.Add(new($"Fuel amount: {(i.type.Invoke as Refuel).energy}"));
                    result.Add(new(""));
                    result.Add(new(r.energy < r.desc.capacity ?
                        "[Enter] Use this item" : "Reactor is at full capacity",
                        Color.Yellow, Color.Black));
                    return result;
                }
                void Invoke(Item i) {
                    var refuel = i.type.Invoke as Refuel;
                    var before = r.energy;
                    var refuelEnergy = Math.Min(refuel.energy, r.desc.capacity - r.energy);
                    if (refuelEnergy > 0) {
                        r.energy += refuelEnergy;
                        player.cargo.Remove(i);
                        player.AddMessage(new Message($"Used {i.type.name} to refuel {refuelEnergy:0} energy on {r.source.type.name}"));
                    }
                }
                void Escape() {
                    var p = screen.Parent;
                    p.Children.Remove(screen);
                    p.Children.Add(prev);
                    p.IsFocused = true;
                }
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }

}

public class ListMenu<T> : ScreenSurface {
    public PlayerShip player;

    public ListPane<T> list;
    public DescPanel<T> descPane;

    public ref string title => ref list.title;


    public Action escape;
    public ListMenu(ScreenSurface prev, PlayerShip player, string title, IEnumerable<T> items, ListPane<T>.GetName getName, DescPanel<T>.GetDesc getDesc, ListPane<T>.Invoke invoke, Action escape):base(prev.Surface.Width, prev.Surface.Height) {
        this.player = player;

        descPane = new DescPanel<T>(40, 26) { Position = new(45, 17) };
        this.list = new(title, items, getName, UpdateDesc) {
            Position = new(4, 16),
            invoke = invoke,
            IsFocused = true
        };

        void UpdateDesc(T i) {
            if(i != null) {
                descPane.SetInfo(getName(i), getDesc(i));
            } else {
                descPane.SetInfo("", new());
            }
        }

        this.escape = escape;
        Children.Add(list);

        Children.Add(descPane);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Escape)) {
            escape?.Invoke();
        } else {
            list.ProcessKeyboard(keyboard);
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Render(TimeSpan delta) {
        Surface.Clear();
        this.RenderBackground();
        base.Render(delta);
    }
}

public class DescPanel<T> : ScreenSurface {
    public delegate List<ColoredString> GetDesc(T t);

    private string name = "";
    private List<ColoredString> desc = new();
    public DescPanel(int width, int height, ListPane<T> list, GetDesc getDesc) : base(width, height) {
        list.indexChanged += i => {
            if (i != null) {
                (name, desc) = (list.getName(i), getDesc(i));
            } else {
                (name, desc) = ("", new());
            }
        };
    }
    public DescPanel(int width, int height) : base(width, height) {}
    public void SetInfo(string name, List<ColoredString> desc) =>
        (this.name, this.desc) = (name, desc);
    public override void Render(TimeSpan delta) {
        Surface.Clear();
        Surface.Print(0, 0, name, Color.Yellow, Color.Black);

        int y = 2;
        foreach(var line in desc) {
            Surface.Print(0, y++, line);
        }

        base.Render(delta);
    }
}
public class ListPane<T> : ScreenSurface {
    public string title;
    public bool groupMode = true;
    public bool active = true;
    public IEnumerable<T> items;

    public T[] singles;
    public (T item, int count)[] groups;

    public GetName getName;
    public Invoke invoke;
    public IndexChanged indexChanged;

    public delegate string GetName(T t);
    public delegate void Invoke(T t);
    public delegate void IndexChanged(T t);

    private int? _index;
    private bool enterDown = false;
    private double time;

    public int? index {
        set {
            _index = value;
            indexChanged?.Invoke(currentItem);
        }
        get => _index;
    }
    public int count => groupMode ? groups.Length : singles.Count();
    public T currentItem => index is { } i ? (groupMode ? groups[i].item : singles[i]) : default;
    public ListPane(string title, IEnumerable<T> items, GetName getName, IndexChanged indexChanged) : base(42, 30) {
        this.title = title;
        this.items = items;
        this.getName = getName;
        this.indexChanged = indexChanged;
        UpdateIndex();
        time = -0.1;
    }
    public void UpdateGroups() {
        groups = singles.GroupBy(i => getName(i))
            .OrderBy(g => Array.IndexOf(singles, g.First()))
            .Select(g => (g.Last(), g.Count()))
            .ToArray();
    }
    public void UpdateIndex() {
        singles = items.ToArray();
        if (groupMode) {
            UpdateGroups();
        }
        index = count > 0 ? Math.Min(index ?? 0, count - 1) : null;
        time = 0;
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        enterDown = keyboard.IsKeyDown(Keys.Enter);
        void Up(int inc) {
            Tones.pressed.Play();
            index =
                count == 0 ?
                    null :
                index == null ?
                    (count - 1) :
                index == 0 ?
                    null :
                    Math.Max(index.Value - inc, 0);
            time = 0;
        }
        void Down(int inc) {
            Tones.pressed.Play();
            index =
                count == 0 ?
                    null :
                index == null ?
                    0 :
                index == count - 1 ?
                    null :
                    Math.Min(index.Value + inc, count - 1);
            time = 0;
        }
        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.Up:       Up(1); break;
                case Keys.PageUp:   Up(26); break;
                case Keys.Down:     Down(1);break;
                case Keys.PageDown: Down(26); break;
                case Keys.Enter:
                    if(currentItem is { } i) {
                        Tones.pressed.Play();
                        invoke?.Invoke(i);
                        UpdateIndex();
                    }
                    break;
                case Keys.Tab: groupMode = !groupMode; break;
                default:
                    if(char.ToLower(key.Character) is var ch and >= 'a' and <= 'z') {
                        Tones.pressed.Play();
                        int start = Math.Max((index ?? 0) - 13, 0);
                        var letterIndex = start + SMenu.letterToIndex(ch);
                        if (letterIndex == index) {
                            invoke?.Invoke(currentItem);
                            UpdateIndex();
                        } else if (letterIndex < count) {
                            index = letterIndex;
                            time = 0;
                        }
                    }
                    break;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }

    bool mouseOnItem;
    public override bool ProcessMouse(MouseScreenObjectState state) {
        
        var paneRect = new Rectangle(1, 3, lineWidth + 8, 26);
        if (mouseOnItem = paneRect.Contains(state.SurfaceCellPosition)) {
            var (start, _) = GetVisibleRange();
            var ind = start + state.SurfaceCellPosition.Y - 3;
            if(ind < count) {
                index = ind;
                enterDown = state.Mouse.LeftButtonDown;
                if (state.Mouse.LeftClicked) {
                    invoke?.Invoke(currentItem);
                }
            }
        }
        return base.ProcessMouse(state);
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;


        base.Update(delta);
    }
    const int lineWidth = 36;
    public (int, int) GetVisibleRange() {
        int start = Math.Max((index ?? 0) - 25, 0),
            end = Math.Min(count, start + 26);
        return (start, end);
    }
    public override void Render(TimeSpan delta) {
        int x = 0;
        int y = 0;
        int w = lineWidth + 4;

        var t = title;
        if(time < 0) {
            w = (int) Main.Lerp(time, -0.1, -0.0, 1, w, 1);
            t = title.LerpString(time, -0.1, 0, 1);
        }
        Surface.DrawRect(x, y, w, 3);

        Surface.Print(x + 2, y + 1, t, active ? Color.Yellow : Color.White, Color.Black);
        Surface.DrawRect(x, y + 2, w, 26 + 2, connectAbove: true);
        x += 2;
        y += 3;
        if (count > 0) {

            int? highlight = index;
            Func<int, string> nameAt =
                groupMode ?
                    i => {
                        var(item, count) = groups[i];
                        return $"{count}x {getName(item)}";
                    } :
                    i => getName(singles[i]);
            var (start, end) = GetVisibleRange();
            for(int i = start; i < end; i++) {
                var n = nameAt(i);
                var (f, b) = (Color.White, Color.Black);
                if (active && i == highlight) {
                    if (n.Length > lineWidth) {
                        double initialDelay = 1;
                        int index = time < initialDelay ? 0 : (int)Math.Min((time - initialDelay) * 15, n.Length - lineWidth);

                        n = n.Substring(index);
                    }
                    (f, b) = (Color.Yellow, Color.Black.Blend(Color.Yellow.SetAlpha(51)));
                    if (enterDown) {
                        (f, b) = (b, f);
                    }
                }
                if (n.Length > lineWidth) {
                    n = $"{n.Substring(0, lineWidth - 3)}...";
                }
                var key = ColorCommand.Front(active ? Color.White : Color.Gray, $"{SMenu.indexToLetter(i - start)}.");
                var name = ColoredString.Parser.Parse(
                    ColorCommand.Recolor(f, b, $"{key} {n}".PadRight(lineWidth)));
                if(time < 0) {
                    Surface.Print(x, y++, name.LerpString(time, -0.1, 0, 1));
                } else {
                    Surface.Print(x, y++, name);
                }
                


            }
            const int height = 26;
            int barStart = height * start / count;
            int barEnd = Math.Min(height - 1, height * end / count);
            int barX = x - 2;

            int Box(BoxGlyph desc) => BoxInfo.IBMCGA.glyphFromInfo[desc];
            ColoredGlyph BarGlyph(BoxGlyph bg) =>
                new(Color.White, Color.Black, Box(bg));
            for (int i = 0; i < height; i++) {
                var cg =
                    (i < barStart || i > barEnd) ?
                        new(Color.LightGray, Color.Black, Box(new() { n = Line.Single, s = Line.Single })) :
                    i == barStart ?
                        BarGlyph(new() { s = Line.Double, e = Line.Single, w = Line.Single }) :
                    i == barEnd ?
                        BarGlyph(new() { n = Line.Double, e = Line.Single, w = Line.Single }) :
                        BarGlyph(new() { n = Line.Double, s = Line.Double });
                Surface.SetCellAppearance(barX, 3 + i, cg);
            }
        } else {
            Surface.Print(x, y, new ColoredString("<Empty>", Color.Yellow, Color.Black));
        }
        base.Render(delta);
    }
}

public class ScrollBar : ScreenSurface {
    public int index;
    public int windowSize;
    public int count;

    public ScrollBar(int windowSize) : base(1, windowSize) {
        this.windowSize = windowSize;
    }
    public (int, int) GetVisibleRange() {
        int start = Math.Max(index - 25, 0),
            end = Math.Min(count, start + 26);
        return (start, end);
    }
    public override void Render(TimeSpan delta) {

        base.Render(delta);
    }
}

public static class SListWidget {
    public static ListWidget<Item> Usables(ScreenSurface prev, PlayerShip player) {
        ListWidget<Item> screen = null;
        IEnumerable<Item> cargoInvokable;
        IEnumerable<Item> installedInvokable;
        List<Item> usable = new();
        void UpdateList() {
            cargoInvokable = player.cargo.Where(i => i.type.Invoke != null);
            installedInvokable = player.devices.Installed.Select(d => d.source).Where(i => i.type.Invoke != null);
            usable.Clear();
            usable.AddRange(installedInvokable.Concat(cargoInvokable));
        }
        UpdateList();

        return screen = new(prev,
            usable,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installedInvokable.Contains(i) ? "[*] " : "[c] ")}{i.type.name}";
        List<ColoredString> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = SMenu.GenerateDesc(i);
            if (invoke != null) {
                var action = $"[Enter] {invoke.GetDesc(player, i)}";
                result.Add(new(action, Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item i) {
            i.type.Invoke?.Invoke(screen, player, i, Update);
            screen.UpdateIndex();
        }
        void Update() {
            UpdateList();
            screen.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.IsFocused = true;
        }
    }
    public static ListWidget<Device> DeviceManager(ScreenSurface prev, PlayerShip player) {
        ListWidget<Device> screen = null;
        var disabled = player.energy.off;
        var powered = player.devices.Powered;
        return screen = new(prev,
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            ) { groupMode=false, title="Device Manager" };
        string GetName(Device d) => $"{(disabled.Contains(d) ? "[ ]" : "[*]")} {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var result = SMenu.GenerateDesc(d);
            result.Add(new($"Status: {(disabled.Contains(d) ? "OFF" : "ON")}"));
            result.Add(new(""));
            var off = disabled.Contains(d);
            var word = (off ? "Enable" : "Disable");
            result.Add(new($"[Enter] {word} this device", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Device p) {
            if (disabled.Contains(p)) {
                disabled.Remove(p);
                player.AddMessage(new Message($"Enabled {p.source.type.name}"));
            } else {
                disabled.Add(p);
                p.OnDisable();
                player.AddMessage(new Message($"Disabled {p.source.type.name}"));
            }
            screen.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.IsFocused = true;
        }
    }
    public static ListWidget<IDockable> DockMenu(ScreenSurface prev, List<IDockable> d, PlayerShip player) {
        ListWidget<IDockable> screen = null;
        return screen = new(prev,
            d,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            ) { groupMode=false, title="Dock At"};
        string GetName(IDockable d) => $"{d.name, -24}"; //{(d.position - player.position).magnitude,4:0}
        List<ColoredString> GetDesc(IDockable d) => new() {
            new($"Distance: {(d.position - player.position).magnitude:0}")
        };
        void InvokeItem(IDockable d) {
            player.dock = new() { Target = d, Offset = d.GetDockPoints().First() };
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.IsFocused = true;
        }
    }
}
public class ListWidget<T> : ScreenSurface {

    public string title = "";

    public bool groupMode = true;
    public IEnumerable<T> items;
    public IEnumerable<(T item, int count)> groups;
    public int count => groupMode ? groups.Count() : items.Count();
    public T currentItem => index.HasValue ? (groupMode ? groups.ElementAt(index.Value).item : items.ElementAt(index.Value)) : default;
    int? index;
    GetName getName;
    GetDesc getDesc;
    Invoke invoke;
    Escape escape;
    int tick;
    public delegate string GetName(T t);
    public delegate List<ColoredString> GetDesc(T t);
    public delegate void Invoke(T t);
    public delegate void Escape();
    public ListWidget(ScreenSurface prev, IEnumerable<T> items, GetName getName, GetDesc getDesc, Invoke invoke, Escape escape) : base(prev.Surface.Width, prev.Surface.Height) {
        this.items = items;
        this.getName = getName;
        this.getDesc = getDesc;
        this.invoke = invoke;
        this.escape = escape;
        UpdateIndex();
    }
    public void UpdateGroups() {
        var l = items.ToList();
        groups = items.GroupBy(i => getName(i))
            .OrderBy(g => l.IndexOf(g.First()))
            .Select(g => (g.Last(), g.Count()))
            .ToHashSet();
    }
    public void UpdateIndex() {
        if (groupMode) UpdateGroups();
        index = count > 0 ? Math.Min(index ?? 0, count - 1) : null;
        tick = 0;
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.Up:
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 1, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageUp:
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 26, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.Down:
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 1, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageDown:
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 26, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.Enter:
                    var i = currentItem;
                    if (i != null) {
                        invoke(i);
                        UpdateIndex();
                    }
                    break;
                case Keys.Escape:
                    escape();
                    break;
                default:
                    var ch = char.ToLower(key.Character);
                    if (ch >= 'a' && ch <= 'z') {
                        int start = Math.Max((index ?? 0) - 13, 0);
                        var letterIndex = start + SMenu.letterToIndex(ch);
                        if (letterIndex == index) {
                            invoke(currentItem);
                            UpdateIndex();
                        } else if (letterIndex < count) {
                            //var item = items.ElementAt(letterIndex);
                            index = letterIndex;
                            tick = 0;
                        }
                    }
                    break;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        tick++;
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        int x = 6;
        int y = 14;

        void line(Point from, Point to, int glyph) {
            Surface.DrawLine(from, to, '-', Color.White, null);
        }
        const int WIDTH = 36;
        Surface.Clear();
        Surface.Print(x, y, title);
        y += 2;


        int start = 0;
        int? highlight = null;
        if (index.HasValue) {
            start = Math.Max(index.Value - 16, 0);
            highlight = index;
        }
        Func<int, string> NameAt = groupMode ? i => {
            var g = groups.ElementAt(i);
            return $"{g.count}x {getName(g.item)}";
        }
        : i => getName(items.ElementAt(i));

        int end = Math.Min(count, start + 26);

        if (count > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = NameAt(i);
                if (n.Length > WIDTH) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - WIDTH);

                        n = n.Substring(index);
                        if (n.Length > WIDTH) {
                            n = $"{n.Substring(0, WIDTH - 3)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, WIDTH - 3)}...";
                    }
                }
                var name = new ColoredString($"{(SMenu.indexToLetter(i - start))}. ", highlightColor, Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                Surface.Print(x, y, name);

                i++;
                y++;
            }


            int height = 26;
            int barStart = (height * (start)) / count;
            int barEnd = (height * (end)) / count;
            int barX = x - 2;

            for (i = 0; i < height; i++) {
                ColoredGlyph cg = (i < barStart || i > barEnd) ?
                    new ColoredGlyph(Color.LightGray, Color.Black, '|') :
                    new ColoredGlyph(Color.White, Color.Black, '#');
                Surface.SetCellAppearance(barX, 16 + i, cg);
            }

            /*
            line(new(barX, 15), new(barX + WIDTH + 7, 15), '-');
            line(new(barX, 16 + 26), new(barX + WIDTH + 7, 16 + 26), '-');
            barX += WIDTH + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            */
        } else {
            var highlightColor = Color.Yellow;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            Surface.Print(x, y, name);


            /*
            int barX = x - 2;
            line(new(barX, 15), new(barX + WIDTH + 7, 15), '-');
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            line(new Point(barX, 16 + 26), new Point(barX + WIDTH + 7, 16 + 26), '-');
            barX += WIDTH + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            */
        }

        x += WIDTH + 7;
        y = 16;
        var item = currentItem;
        if (item != null) {
            Surface.Print(x, y, getName(item), Color.Yellow, Color.Black);
            y += 2;
            foreach (var l in getDesc(item)) {
                Surface.Print(x, y++, l);
            }
        }

        base.Render(delta);
    }

}