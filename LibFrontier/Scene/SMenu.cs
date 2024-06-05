using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using LibGamer;
using Tile = LibGamer.Tile;
using Color = LibGamer.ABGR;
using ColoredString = LibGamer.Tile[];
using System.IO;
using System.Xml.Linq;
namespace RogueFrontier;
public static partial class SMenu {
	public static void RenderBackground (this Sf c) {
		c.Clear(new Tile(ABGR.Black, ABGR.SetA(ABGR.Black, 128), ' '));
		/*
        var back = new Console(c.Width, c.Height);

        foreach (var point in new Rectangle(0, 0, c.Width, c.Height).Positions()) {

            var h = point.X % 4 == 0;
            var v = point.Y % 4 == 0;

            var f = new Color(255, 255, 255, 255 * 4 / 8);

            if (h && v) {
                f = new Color(255, 255, 255, 255 * 6 / 8);
            } else if (h || v) {
                f = new Color(255, 255, 255, 255 * 5 / 8);
            }
            back.SetCellAppearance(point.X, point.Y, new ColoredGlyph(f, Color.Black.SetAlpha(102), '.'));
        }
        back.Render(new TimeSpan());
        */
	}
	public class RectOptions {
		public bool connectBelow, connectAbove;
		public Line width = Line.Single;
		public uint f = ABGR.White, b = ABGR.Black;
	}
	public static void DrawRect (this Sf surf, int xStart, int yStart, int dx, int dy, RectOptions op) {
		char Box (Line n = Line.None, Line e = Line.None, Line s = Line.None, Line w = Line.None) =>
			(char)BoxInfo.IBMCGA.glyphFromInfo[new(n, e, s, w)];

		var width = op.width;
		var aboveWidth = op.connectAbove ? width : Line.None;
		var belowWidth = op.connectBelow ? width : Line.None;
			var vert = Box(n: width, s: width);
			var hori = Box(e: width, w: width);

		void c (int x, int y, char c) =>
				surf.Print(x, y, new Tile(op.f, op.b, c));
        void l (int x, int y, string line) =>
                surf.Print(x, y, Tile.Arr(line, op.f, op.b));

		int y = yStart;

		void p (string line) =>
				surf.Print(xStart, y++, Tile.Arr(line, op.f, op.b));
		bool fill = ABGR.A(op.b) != 0;
        if(dx == 0 || dy == 0)
            return;
        if(dx == 1) {
			var n = Box(e: Line.Single, w: Line.Single, s: width, n: aboveWidth);
			var s = Box(e: Line.Single, w: Line.Single, n: width, s: belowWidth);
			p($"{n}");
            foreach(var i in 0..Math.Max(0, dy - 1))
				p($"{vert}");
            p($"{s}");
        } else if(dy == 1) {
            var e = Box(n: Line.Single, s: Line.Single, w: width);
            var w = Box(n: Line.Single, s: Line.Single, e: width);
            p($"{w}{new string(hori, dx - 2)}{e}");
        } else {
            var nw = Box(e: width, s: width, n: aboveWidth);
            var ne = Box(w: width, s: width, n: aboveWidth);
            var sw = Box(e: width, n: width, s: belowWidth);
            var se = Box(w: width, n: width, s: belowWidth);

            if(fill) {

				p($"{nw}{new string(hori, dx - 2)}{ne}");
				foreach(var i in 0..Math.Max(0,dy - 2))
					p($"{vert}{new string(' ', dx - 2)}{vert}");
				p($"{sw}{new string(hori, dx - 2)}{se}");
			} else {
                var x = xStart;
                l(x, y, $"{nw}{new string(hori, dx - 2)}{ne}"); y++;
                foreach(var i in 0..Math.Max(0, dy - 1)) {
                    c(x, y, vert); c(x + dx - 1, y, vert); y++;
                }
                l(x, y, $"{sw}{new string(hori, dx - 2)}{se}"); y++;
            }
        }
	}



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
    public static ListMenu<IPlayerMessage> Logs(SceneCtx c) {
        (IScene prev, PlayerShip player) = (c.prev, c.playerShip);
        ListMenu<IPlayerMessage> screen = null;
        List<IPlayerMessage> logs = player.logs;
        return screen = new(c,
            $"{player.name}: Logs",
            logs,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(IPlayerMessage i) => i switch {
            Message { text: { }t } => t,
            Transmission { text: { }t } => $"{t}",
            _ => throw new NotImplementedException()
        };
        List<Tile[]> GetDesc(IPlayerMessage i) {
            return i switch {
                Message => [],
                Transmission t => [
                    [..Tile.Arr("Source: "), ..Tile.Arr((t.source as ActiveObject)?.name ?? "N/A")]
                    ],
                _ => throw new NotImplementedException()
            };
        }
        void Invoke(IPlayerMessage item) {
            screen.list.UpdateIndex();
        }
        void Escape() {
            prev.Show();
        }
    }
    public static ListMenu<IPlayerInteraction> Missions(SceneCtx c, Timeline story) {
        var (prev, player) = (c.prev, c.playerShip);
        ListMenu<IPlayerInteraction> screen = null;
        List<IPlayerInteraction> missions = new();
        void UpdateList() {
            missions.Clear();
            missions.AddRange(story.mainInteractions);
            missions.AddRange(story.secondaryInteractions);
            missions.AddRange(story.completedInteractions);
        }
        UpdateList();
        return screen = new(c,
            $"{player.name}: Missions",
            missions,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );
        string GetName(IPlayerInteraction i) => i switch {
            DestroyTarget => "Destroy Target",
            _ => "Mission"
        };
        List<ColoredString> GetDesc(IPlayerInteraction i) {
            List<ColoredString> result = new();
            switch (i) {
                case DestroyTarget dt:
                    if (dt.complete) {
                        result.Add(Tile.Arr($"Mission complete"));
                        result.Add(Tile.Arr($"Return to {dt.source.name}"));
                    } else {
                        result.Add(Tile.Arr("Destroy the following targets:"));
                        foreach (var t in dt.targets) {
                            result.Add(Tile.Arr($"- {t.name}"));
                        }
                        result.Add([]);
                        result.Add(Tile.Arr($"Source: {dt.source.name}"));
                    }
                    result.Add([]);
                    result.Add(Tile.Arr($"[Enter] Update targets", Color.Yellow, Color.Black));
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
            prev.Show();
        }
    }
    public static List<Tile[]> GenerateDesc(ItemType t) {
        var result = new List<Tile[]>();
        var desc = t.desc.SplitLine(64);
        if (desc.Any()) {
            result.AddRange(desc.Select(d => Tile.Arr(d)));
            result.Add([]);
        }
        return result;
    }
    public static List<Tile[]> GenerateDesc(Item i) => GenerateDesc(i.type);
    public static List<Tile[]> GenerateDesc(Device d) {
        var r = GenerateDesc(d.source.type);
        ((Action)(d switch {
            Weapon w => () => {
                Tile[]?[] lines = [
                    Tile.Arr($"Damage range: {w.desc.Projectile.damageHP.str}"),
                    Tile.Arr($"Fire cooldown:{w.desc.fireCooldown/60.0:0.00} SEC"),
                    Tile.Arr($"Power rating: {w.desc.powerUse}"),
                    w.desc.recoil != 0 ?
                        Tile.Arr($"Recoil force: {w.desc.recoil}") : null,
                    w.ammo switch {
                        ItemAmmo ia =>      Tile.Arr($"Ammo type:    {ia.itemType.name}"),
                        ChargeAmmo ca =>    Tile.Arr($"Charges left: {ca.charges}"),
                        _ => null
                    },
                    w.aiming switch {
                        Omnidirectional =>  Tile.Arr($"Turret:       Omnidirectional"),
                        Swivel s =>         Tile.Arr($"Turret:       {(int)((s.leftRange + s.rightRange) * 180 / Math.PI)}-degree swivel"),
                        _ => null
                    }
                    ];
                r.AddRange(lines.Except([null]));
            },
            Shield s => () => {

                r.AddRange(new Tile[][] {
					Tile.Arr(        $"Max HP:  {s.desc.maxHP} HP"),
					Tile.Arr(        $"Regen:   {s.desc.regen:0.00} HP/s"),
					Tile.Arr(        $"Stealth: {s.desc.stealth}"),
					Tile.Arr(        $"Idle power use:  {s.desc.idlePowerUse}"),
					Tile.Arr(        $"Regen power use: {s.desc.powerUse}"),
                    s.desc.reflectFactor is > 0 and var reflectFactor ?
						Tile.Arr(  $"Reflect factor:  {reflectFactor}") : null,
                }.Except([null]));
            }
            ,
            Solar solar => () => {
                r.AddRange(new Tile[][] {
					Tile.Arr($"Peak output:    {solar.maxOutput} EL"),
					Tile.Arr($"Current output: {solar.energyDelta} EL")
                });
            }
            ,
            Reactor reactor => () => {
                r.AddRange(new Tile[][] {
					Tile.Arr($"Peak output:     {reactor.maxOutput, -4} EL"),
					Tile.Arr($"Current output:  {-reactor.energyDelta, -4} EL"),
					Tile.Arr($"Energy capacity: {reactor.desc.capacity, -4} EN"),
					Tile.Arr($"Energy content:  {(int)reactor.energy, -4} EN"),
					Tile.Arr($"Efficiency:      {reactor.efficiency, -4} EL/EN"),

                });
            },
            Armor armor => () => {
                r.AddRange(new Tile[][] {
					Tile.Arr($"Max HP: {armor.maxHP}"),
                });
            },

            _ => () => { }
        })).Invoke();
        r.Add(Tile.Arr(""));
        return r;
    }
    public static ListMenu<Item> Usable(SceneCtx c) {

        var (prev, player) = (c.prev, c.playerShip);
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
        return screen = new(c,
            $"{player.name}: Useful Items",
            usable,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installedInvokable.Contains(i) ? "[*] " : "[c] ")}{i.type.name}";
        List<Tile[]> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = GenerateDesc(i);
            if (invoke != null) {
                string action = $"[Enter] {invoke.GetDesc(player, i)}";
                result.Add(Tile.Arr(action, ABGR.Yellow, ABGR.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Device> Installed(SceneCtx c) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(c,
            $"{player.name}: Device System",
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );
        string GetName(Device d) => d.source.type.name;
        List<Tile[]> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(d);

            if (invoke != null) {
                result.Add(Tile.Arr($"[Enter] {invoke.GetDesc(player, item)}", ABGR.Yellow, ABGR.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Item> Cargo(SceneCtx c) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Item> screen = null;
        var items = player.cargo;
        return screen = new(c,
            $"{player.name}: Cargo",
            items,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );
        string GetName(Item i) => i.type.name;
        List<Tile[]> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = GenerateDesc(i);
            if (invoke != null) {
                result.Add(Tile.Arr($"[Enter] {invoke.GetDesc(player, i)}", ABGR.Yellow, ABGR.Black));
            }
            return result;
        }
        void InvokeItem(Item item) {
            var invoke = item.type.Invoke;
            invoke?.Invoke(screen, player, item);
            screen.list.UpdateIndex();
        }
        void Escape() {
            screen.Go(prev);
        }
    }
    public static ListMenu<Device> DeviceManager(SceneCtx c) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var disabled = player.energy.off;
        var powered = player.devices.Powered;
        return screen = new(c,
            $"{player.name}: Device Power",
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );
        string GetName(Device d) => $"{(disabled.Contains(d) ? "[ ]" : "[*]")} {d.source.type.name}";
        List<Tile[]> GetDesc(Device d) {
            var result = GenerateDesc(d);
            result.Add(Tile.Arr($"Status: {(disabled.Contains(d) ? "OFF" : "ON")}"));
            result.Add(Tile.Arr(""));
            var off = disabled.Contains(d);
            var word = (off ? "Enable" : "Disable");
            result.Add(Tile.Arr($"[Enter] {word} this device", ABGR.Yellow, ABGR.Black));
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
        void Escape () {
            prev.Show();
        }
    }
    public static ListMenu<Device> RemoveDevice(SceneCtx c) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(c,
            $"{player.name}: Device Removal",
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );
        string GetName(Device d) => d.source.type.name;
        List <Tile[]> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(d);
            if (invoke != null) {
                result.Add(Tile.Arr($"[Enter] Remove this device", ABGR.Yellow, ABGR.Black));
            }
            return result;
        }
        void InvokeDevice(Device device) {
            var item = device.source;
            player.devices.Remove(device);
            player.cargo.Add(item);
            screen.list.UpdateIndex();
        }
        void Escape () {
            prev.Show();
        }
    }
    public static ListMenu<Armor> RepairArmorFromItem(SceneCtx c, Item source, RepairArmor repair, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Armor> screen = null;
        var devices = (player.hull as LayeredArmor).layers;

        return screen = new(c,
            $"{player.name}: Armor Repair",
            devices,
            GetName,
            GetDesc,
            Repair,
            Escape
            );

        string GetName(Armor a) => $"{$"[{a.hp} / {a.maxHP}]",-12}{a.source.type.name}";
        List<Tile[]> GetDesc(Armor a) {
            var item = a.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(a);
            if (a.desc.RestrictRepair?.Matches(source) == false) {
                result.Add(Tile.Arr("This armor is not compatible", ABGR.Yellow, ABGR.Black));
            } else if (a.hp < a.maxHP) {
                result.Add(Tile.Arr("[Enter] Repair this armor", ABGR.Yellow, ABGR.Black));
            } else if(a.maxHP < a.desc.maxHP) {
                result.Add(Tile.Arr("This armor cannot be repaired any further", ABGR.Yellow, ABGR.Black));
            } else {
                result.Add(Tile.Arr("This armor is at full HP", ABGR.Yellow, ABGR.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Reactor> RefuelFromItem(SceneCtx c, Item source, Refuel refuel, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Reactor> screen = null;
        var devices = player.devices.Reactor;
        return screen = new(c,
            $"{player.name}: Refuel Reactor",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<Tile[]> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            result.Add(Tile.Arr($"Refuel amount: {refuel.energy}"));
            result.Add(Tile.Arr($"Fuel needed:   {r.desc.capacity - (int)r.energy}"));
            result.Add(Tile.Arr(""));


            if(!r.desc.allowRefuel) {
                result.Add(Tile.Arr("This reactor does not accept fuel", ABGR.Yellow, ABGR.Black));
            } else if (r.energy < r.desc.capacity) {
                result.Add(Tile.Arr("[Enter] Refuel", ABGR.Yellow, ABGR.Black));
            } else {
                result.Add(Tile.Arr("This reactor is full", ABGR.Yellow, ABGR.Black));
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
        void Escape () {
            prev.Show();
        }
    }
    public static ListMenu<Device> ReplaceDeviceFromItem(SceneCtx c, Item source, ReplaceDevice replace, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var devices = player.devices.Installed.Where(i => i.source.type == replace.from);
        return screen = new(c,
            $"{player.name}: Device Replacement",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Device d) => $"{d.source.type.name}";
        List<Tile[]> GetDesc(Device r) {
            var item = r.source;
            var result = GenerateDesc(r);
            result.Add(Tile.Arr("Replace this device", ABGR.Yellow, ABGR.Black));
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
        void Escape () {
            prev.Show();
        }
    }
    public static ListMenu<Weapon> RechargeWeaponFromItem(SceneCtx c, Item source, RechargeWeapon recharge, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Weapon> screen = null;
        var devices = player.devices.Weapon.Where(i => i.desc == recharge.weaponType);
        return screen = new(c,
            $"{player.name}: Recharge Weapon",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Weapon w) => $"{w.source.type.name}";
        List<Tile[]> GetDesc(Weapon w) {
            var item = w.source;
			var result = GenerateDesc(w);
            result.Add(Tile.Arr("Recharge this weapon", ABGR.Yellow, ABGR.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<ItemType> Workshop(SceneCtx c, Dictionary<ItemType, Dictionary<ItemType, int>> recipes, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
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
        return screen = new(c,
            $"Workshop",
            recipes.Keys,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );
        string GetName(ItemType type) => $"{type.name}";
        List<Tile[]> GetDesc(ItemType type) {
            var result = GenerateDesc(type);
            var rec = recipes[type];
            foreach ((var compType, var minCount) in rec) {
                var count = listing[type][compType].Count;
                result.Add(Tile.Arr($"{compType.name}: {count} / {minCount}", count >= minCount ? ABGR.Yellow : ABGR.Gray, ABGR.Black));
            }
            result.Add([]);

            if (available[type]) {
                result.Add(Tile.Arr("[Enter] Fabricate this item", ABGR.Yellow, ABGR.Black));
            } else {
                result.Add(Tile.Arr("Additional materials required", ABGR.Yellow, ABGR.Black));
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
        void Escape () {
            prev.Show();
        }
    }
    public static ListMenu<Reactor> DockReactorRefuel(SceneCtx c, Func<Reactor, int> GetPrice, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Reactor> screen = null;
        var reactors = player.devices.Reactor;
        RefuelEffect job = null;
        return screen = new(c,
            $"Refuel Service",
            reactors,
            GetName,
            GetDesc, Invoke, Escape);
        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<Tile[]> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            int unitPrice = GetPrice(r);
            if (unitPrice < 0) {
                result.Add(Tile.Arr("Refuel services not available for this reactor", ABGR.Yellow, ABGR.Black));
                return result;
            }
            var delta = r.desc.capacity - (int)r.energy;
            result.Add(Tile.Arr($"Fuel needed: {delta}"));
            result.Add(Tile.Arr($"Total cost:  {unitPrice * delta}"));
            result.Add(Tile.Arr($"Your money:  {player.person.money}"));
            result.Add([]);
            if (delta <= 0) {
                result.Add(Tile.Arr("This reactor is full", ABGR.Yellow, ABGR.Black));
            } else if (job?.active == true) {
                if (job.reactor == r) {
                    result.Add(Tile.Arr("This reactor is currently refueling.", ABGR.Yellow, ABGR.Black));
                } else {
                    result.Add(Tile.Arr("Please wait for current refuel job to finish.", ABGR.Yellow, ABGR.Black));
                }
            } else if (unitPrice > player.person.money) {
                result.Add(Tile.Arr($"You cannot afford refueling", Color.Yellow, Color.Black));
            } else {
                result.Add(Tile.Arr($"[Enter] Order refueling", Color.Yellow, Color.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Armor> DockArmorRepair(SceneCtx c, Func<Armor, int> GetPrice, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Armor> screen = null;
        var layers = (player.hull as LayeredArmor)?.layers ?? new();
        RepairEffect job = null;

        byte[]
            start = File.ReadAllBytes($"{Assets.ROOT}/sounds/repair_start.wav"),
            stop = File.ReadAllBytes($"{Assets.ROOT}/sounds/repair_stop.wav");


        return screen = new(c,
            $"Armor Repair",
            layers,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

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
                result.Add(Tile.Arr("Repair services not available for this armor", Color.Yellow, Color.Black));
                return result;
            }
            int delta = a.maxHP - a.hp;
            result.Add(Tile.Arr($"Price per HP: {unitPrice}"));
            result.Add(Tile.Arr($"HP to repair: {delta}"));
            result.Add(Tile.Arr($"Your money:   {player.person.money}"));
            result.Add(Tile.Arr($"Full price:   {unitPrice * delta}"));
            result.Add([]);
            if (delta <= 0) {
                if(a.maxHP == 0) {
                    result.Add(Tile.Arr("This armor cannot be repaired.", Color.Yellow, Color.Black));
                } else if (a.maxHP < a.desc.maxHP) {
                    result.Add(Tile.Arr("This armor cannot be repaired any further.", Color.Yellow, Color.Black));
                } else {
                    result.Add(Tile.Arr("This armor is at full HP.", Color.Yellow, Color.Black));
                }
                goto Done;
            }
            if (job?.active == true) {
                if (job.armor == a) {
                    result.Add(Tile.Arr("This armor is currently under repair.", Color.Yellow, Color.Black));
                } else {
                    result.Add(Tile.Arr("Another armor is currently under repair.", Color.Yellow, Color.Black));
                }
                goto Done;
            }
            if (unitPrice > player.person.money) {
                result.Add(Tile.Arr($"You cannot afford repairs", Color.Yellow, Color.Black));
                goto Done;
            }
            result.Add(Tile.Arr($"[Enter] Order repairs", Color.Yellow, Color.Black));

        Done:
            return result;
        }
        void Invoke(Armor a) {
            if (job?.active == true) {
                if (job.armor == a) {
                    job.active = false;
                    player.AddMessage(new Message($"Repair job terminated."));

					screen.PlaySound?.Invoke(new(stop,33));
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

            screen.PlaySound?.Invoke(new(start, 33));

            callback?.Invoke();
        }
        void Done(RepairEffect r) {
            player.world.RemoveEvent(r);
            player.AddMessage(new Message($"Repair job {(r.terminated ? "terminated" : "completed")}"));

			screen.PlaySound?.Invoke(new(stop, 33));
		}
        void Escape() {
            if (job?.active == true) {
                job.active = false;
                player.world.RemoveEvent(job);
                job = null;
                player.AddMessage(new Message($"Repair job canceled"));

				screen.PlaySound?.Invoke(new(stop,33));
				return;
            }
            prev.Show();
        }
    }
    public static ListMenu<Device> DockDeviceRemoval(SceneCtx c, Func<Device, int> GetPrice, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var installed = player.devices.Installed;
        return screen = new(c,
            $"Device Removal",
            installed,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Device d) => $"{d.GetType().Name.PadRight(7)}: {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var result = GenerateDesc(d);
            int unitPrice = GetPrice(d);
            if (unitPrice < 0) {
                result.Add(Tile.Arr("Removal service is not available for this device", Color.Yellow, Color.Black));
            } else {

                result.Add(Tile.Arr($"Removal fee: {unitPrice}"));
                result.Add(Tile.Arr($"Your money:  {player.person.money}"));
                result.Add(Tile.Arr(""));
                if (unitPrice > player.person.money) {
                    result.Add(Tile.Arr($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(Tile.Arr($"Remove device", Color.Yellow, Color.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Device> DockDeviceInstall(SceneCtx c, Func<Device, int> GetPrice, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Device> screen = null;
        var cargo = player.cargo.Select(i =>
            i.engine ?? i.reactor ?? i.service ?? i.shield ?? (Device)i.solar ?? i.weapon)
            .Except([null]);
        return screen = new(c,
            $"Device Install",
            cargo,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Device d) => $"{d.GetType().Name.PadRight(7)}: {d.source.type.name}";
        List<Tile[]> GetDesc(Device d) {
            var item = d.source;
            var result = GenerateDesc(d);
            if (d is Weapon && player.shipClass.restrictWeapon?.Matches(d.source) == false) {
                result.Add(Tile.Arr("This weapon is not compatible", Color.Yellow, Color.Black));
                return result;
            }

            int price = GetPrice(d);
            if (price < 0) {
                result.Add([]);
                result.Add(Tile.Arr("Install service is not available for this device", Color.Yellow, Color.Black));
                return result;
            }


            result.Add(Tile.Arr($"Install fee: {price}"));
            result.Add(Tile.Arr($"Your money:  {player.person.money}"));
            result.Add(Tile.Arr(""));
            if (price > player.person.money) {
                result.Add(Tile.Arr($"You cannot afford service", Color.Yellow, Color.Black));
            } else {
                result.Add(Tile.Arr($"Install device", Color.Yellow, Color.Black));
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
            prev.Show();
        }
    }
    public static ListMenu<Armor> DockArmorReplacement(SceneCtx c, Func<Armor, int> GetPrice, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Armor> screenA = null;
        var armor = (player.hull as LayeredArmor)?.layers ?? new List<Armor>();
        return screenA = new(c,
            $"Armor Replacement",
            armor,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Armor a) => $"{a.source.type.name}";
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;
            var result = GenerateDesc(a);
            int price = GetPrice(a);
            if (price < 0) {
                result.Add(Tile.Arr("Removal service is not available for this armor", Color.Yellow, Color.Black));
            } else {
                result.Add(Tile.Arr($"Your money:  {player.person.money}"));
                result.Add(Tile.Arr($"Removal fee: {price}"));
                result.Add(Tile.Arr(""));
                if (price > player.person.money) {
                    result.Add(Tile.Arr($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(Tile.Arr($"Select replacement", Color.Yellow, Color.Black));
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

            prev.Go(GetReplacement(screenA));

            ListMenu<Armor> GetReplacement(IScene prev) {
                ListMenu<Armor> screenB = null;
                var armor = player.cargo.Select(i => i.armor).Where(i => i != null);
                return screenB = new(
                    c,
                    $"Armor Replacement (continued)",
                    armor,
                    GetName,
                    GetDesc,
                    Invoke,
                    Escape
                    );
                string GetName(Armor a) => $"{a.source.type.name}";
                List<ColoredString> GetDesc(Armor a) {
                    var item = a.source;
                    var result = GenerateDesc(a);
                    if (player.shipClass.restrictArmor?.Matches(a.source) == false) {
                        result.Add(Tile.Arr("This armor is not compatible", Color.Yellow, Color.Black));
                        return result;
                    }
                    int installPrice = GetPrice(a);
                    if (installPrice < 0) {
                        result.Add(Tile.Arr("Install service is not available for this armor", Color.Yellow, Color.Black));
                        return result;
                    }
                    var totalCost = removalPrice + installPrice;
                    result.Add(Tile.Arr($"Your money:  {player.person.money}"));
                    result.Add(Tile.Arr($"Removal fee: {removalPrice}"));
                    result.Add(Tile.Arr($"Install fee: {installPrice}"));
                    result.Add(Tile.Arr($"Total cost:  {totalCost}"));
                    result.Add(Tile.Arr(""));
                    if (totalCost > player.person.money) {
                        result.Add(Tile.Arr($"You cannot afford service", Color.Yellow, Color.Black));
                        return result;
                    }
                    result.Add(Tile.Arr($"Replace with this armor", Color.Yellow, Color.Black));
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
                    ((IScene)screenA).Show();
                }
            }
        }
        void Escape() {
            prev.Show();
        }
    }
    public static ListMenu<Item> SetMod(SceneCtx c, Item source, FragmentMod mod, Action callback) {
		var (prev, player) = (c.prev, c.playerShip);
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

        return screen = new(c,
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
            result.Add(Tile.Arr("[Enter] Apply modifier", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Item i) {
            //i.mod = mod;
            player.cargo.Remove(source);
            player.AddMessage(new Message($"Applied {source.name} to {i.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            prev.Show();
        }
    }
    public static ListMenu<Reactor> RefuelReactor(SceneCtx c) {
		var (prev, player) = (c.prev, c.playerShip);
		ListMenu<Reactor> screenA = null;
        var devices = player.devices.Reactor;
        return screenA = new(c,
            $"{player.name}: Refuel",
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.Invoke;
            var result = GenerateDesc(r);
            var a = (string s) => result.Add(Tile.Arr(s));
            if (r.energy < r.desc.capacity) {
                result.Add(Tile.Arr("[Enter] Refuel this reactor", Color.Yellow, Color.Black));
            } else {
                result.Add(Tile.Arr("This reactor is at full capacity", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            if (r.energy < r.desc.capacity) {
                prev.Go(ChooseFuel(screenA));
            }
            ListMenu<Item> ChooseFuel(IScene prev) {
                ListMenu<Item> screen = null;
                var items = player.cargo.Where(i => i.type.Invoke is Refuel r);
                return screen = new(c, $"{player.name}: Refuel (continued)", items,
                    GetName, GetDesc, Invoke, Escape
                    );
                string GetName(Item i) => i.type.name;
                List<ColoredString> GetDesc(Item i) {
                    var result = GenerateDesc(i);
                    result.Add(Tile.Arr($"Fuel amount: {(i.type.Invoke as Refuel).energy}"));
                    result.Add(Tile.Arr(""));
                    result.Add(Tile.Arr(r.energy < r.desc.capacity ?
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
                    prev.Show();
                }
            }
        }
        void Escape() {
            prev.Show();
        }
    }
}
public class ListMenu<T> : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

    Sf sf;
    public PlayerShip player;
    public ListPane<T> list;
    public DescPanel<T> descPane;
    public ref string title => ref list.title;
	public Action escape;
    public ListMenu(SceneCtx c, string title, IEnumerable<T> items, ListPane<T>.GetName getName, DescPanel<T>.GetDesc getDesc, ListPane<T>.Invoke invoke, Action escape) {
        this.player = c.playerShip;
        sf = new Sf(c.Width, c.Height, Fonts.FONT_8x8);
        descPane = new DescPanel<T>();
        this.list = new(title, items, getName, UpdateDesc) {
            invoke = invoke,
        };
        void UpdateDesc(T i) {
            if(i != null) {
                descPane.SetInfo(getName(i), getDesc(i));
            } else {
                descPane.Clear();
            }
        }
        this.escape = escape;
    }
    void IScene.HandleKey(LibGamer.KB keyboard) {
        if (keyboard.IsPress(KC.Escape)) {
            escape?.Invoke();
        } else {
            list.HandleKey(keyboard);
        }
    }
    void IScene.Render(TimeSpan delta) {
        sf.Clear();
        sf.RenderBackground();
        Draw(sf);
    }
}

public class DescPanel<T> {
    public delegate List<Tile[]> GetDesc(T t);
    public record Entry(string name, List<Tile[]> desc);
    Entry entry;
    public DescPanel(ListPane<T> list, GetDesc getDesc) {
        list.indexChanged += i => {
            if (i != null) {
                entry = new(list.getName(i), getDesc(i));
            } else {
                entry = null;
            }
        };
    }
    public DescPanel () { }

    public void Clear() {
        entry = null;
    }
    public void SetInfo(string name, List<Tile[]> desc) =>
        entry = new(name, desc);
    public void Render(Sf Surface, TimeSpan delta) {
        Surface.Clear();
        Surface.Print(0, 0, Tile.Arr(entry.name, ABGR.Yellow, ABGR.Black));

        int y = 2;
        foreach(var line in entry.desc) {
            Surface.Print(0, y++, line);
        }
    }
}
public class ListPane<T> {
    public string title;
    public bool groupMode = true;
    public bool active = true;
    public IEnumerable<T> items;

    public T[] singles;
    public (T item, int count)[] groups;

    public GetName getName;
    public Invoke invoke;
    public IndexChanged indexChanged;

    public Action<SoundCtx> PlaySound;

    public delegate string GetName(T t);
    public delegate void Invoke(T t);
    public delegate void IndexChanged(T t);

    private int? _index;
    private bool enterDown = false;
    private double time;

    private ScrollBar scroll = new(26);
    public int? index {
        set {
            _index = value;

            if(value != null) {
                scroll.ScrollToShow(value.Value);
            }

            indexChanged?.Invoke(currentItem);
        }
        get => _index;
    }
    public int count => groupMode ? groups.Length : singles.Count();
    public T currentItem => index is { } i ? (groupMode ? groups[i].item : singles[i]) : default;
    public ListPane(string title, IEnumerable<T> items, GetName getName, IndexChanged indexChanged) {
        this.title = title;
        this.items = items;
        this.getName = getName;
        this.indexChanged = indexChanged;

        scroll = new(26);

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
        scroll.count = count;
        if(count > 0) {
            index = Math.Min(index ?? 0, count - 1);
        } else {
            index = null;
        }
        time = 0;
    }
    public void HandleKey(KB keyboard) {
        enterDown = keyboard[KC.Enter, 1];
        void Up(int inc) {
            PlaySound?.Invoke(Tones.pressed);
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
			PlaySound?.Invoke(Tones.pressed);
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
        foreach (var key in keyboard.Press) {
            switch (key) {
                case KC.Up:       Up(1); break;
                case KC.PageUp:   Up(26); break;
                case KC.Down:     Down(1);break;
                case KC.PageDown: Down(26); break;
                case KC.Enter:
                    if(currentItem is { } i) {
                        PlaySound(Tones.pressed);
                        invoke?.Invoke(i);
                        UpdateIndex();
                    }
                    break;
                case KC.Tab: groupMode = !groupMode; UpdateIndex(); break;
                default:
                    if(char.ToLower((char)key) is var ch and >= 'a' and <= 'z') {
						PlaySound?.Invoke(Tones.pressed);
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
    }

    bool mouseOnItem;
    public void ProcessMouse(Hand state) {
        
        var paneRect = new Rect(1, 3, lineWidth + 8, 26);
        if (mouseOnItem = paneRect.Contains(state.nowPos)) {
            var (start, _) = scroll.GetIndexRange();
            var ind = start + state.nowPos.y - 3;
            if(ind < count) {
                index = ind;
                enterDown = state.nowLeft;
                //TO DO: impl click
                if (state.left == Pressing.Released) {
                    invoke?.Invoke(currentItem);
                }
            }
        }
    }
    public void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    const int lineWidth = 36;
    public void Render(Sf Surface, TimeSpan delta) {
        int x = 0;
        int y = 0;
        int w = lineWidth + 7;

        var t = title;
        if(time < 0) {
            w = (int) Main.Lerp(time, -0.1, -0.0, 1, w, 1);
            t = title.LerpString(time, -0.1, 0, 1);
        }
        Surface.DrawRect(x, y, w, 3, new());

        Surface.Print(x + 2, y + 1, Tile.Arr(t, active ? Color.Yellow : Color.White, Color.Black));
        Surface.DrawRect(x, y + 2, w, 26 + 2, new() { connectAbove = true });
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
            var (start, end) = scroll.GetIndexRange();
            for(int i = start; i < end; i++) {
                var n = nameAt(i);
                var (f, b) = (Color.White, i%2 == 0 ? Color.Black : ABGR.Blend(Color.Black,ABGR.SetA(Color.White,36)));
                if (active && i == highlight) {
                    if (n.Length > lineWidth) {
                        double initialDelay = 1;
                        int index = time < initialDelay ? 0 : (int)Math.Min((time - initialDelay) * 15, n.Length - lineWidth);

                        n = n.Substring(index);
                    }
                    (f, b) = (Color.Yellow, ABGR.Blend(Color.Black, ABGR.SetA(Color.Yellow,51)));
                    if (enterDown) {
                        (f, b) = (b, f);
                    }
                }
                if (n.Length > lineWidth) {
                    n = $"{n.Substring(0, lineWidth - 3)}...";
                }
                var key = @$"<S f=""{(active ? ABGR.White : ABGR.Gray)}"">{SMenu.indexToLetter(i - start)}.</S>";
                var name = Tile.ArrFrom(XElement.Parse($"<S>{$"{key} {n}":lineWidth}</S>"), f, b);
                if(time < 0) {
                    Surface.Print(x, y++, name.LerpString(time, -0.1, 0, 1));
                } else {
                    Surface.Print(x, y++, name);
                }
            }
        } else {
            Surface.Print(x, y, Tile.Arr("<Empty>", Color.White, Color.Black));
        }
    }
}

public class ScrollBar {
    public int index;
    public int windowSize;
    public int count;

    public ScrollBar(int windowSize) {
        this.windowSize = windowSize;
    }
    public (int, int) GetIndexRange() {
        int start = Math.Max(index, 0),
            end = Math.Min(count, start + 26);
        return (start, end);
    }
    public (int barStart, int barEnd) GetBarRange() {
        if (count <= 26) {
            return (0, windowSize - 1);
        }
        var (start, end) = GetIndexRange();
        return (windowSize * start / count, Math.Min(windowSize - 1, windowSize * end / count));
    }
    public void ScrollToShow(int index) {
        if(index < this.index) {
            this.index = index;
        } else if(index >= this.index + 25) {
            this.index = Math.Max(0, index - 25);
        }
    }
    public void Scroll(int delta) {
        index += delta * count / windowSize;
        index = Math.Clamp(index, 0, count - 25);
    }

    bool mouseOnBar;
    bool clickOnBar;
    int prevClick = 0;
    public void ProcessMouse(HandState state) {
        if (state.on) {
            var (barStart, barEnd) = GetBarRange();
            var y = state.pos.y;
            if (state.leftDown) {

                if (clickOnBar) {
                    int delta = y - prevClick;
                    Scroll(delta);

                    prevClick = y;
                } else {
                    mouseOnBar = clickOnBar = false;
                    if (y < barStart) {
                        Scroll(Math.Sign(y - barStart));
                    } else if (y > barEnd) {
                        Scroll(Math.Sign(y - barEnd));
                    } else {
                        prevClick = y;
                        mouseOnBar = clickOnBar = true;
                    }
                }
            } else {
                mouseOnBar = y >= barStart && y <= barEnd;
                clickOnBar = false;
            }
        } else {
            if(clickOnBar && state.leftDown) {
                var y = state.pos.y;
                int delta = y - prevClick;
                Scroll(delta);

                prevClick = y;
            } else {
                mouseOnBar = clickOnBar = false;
            }

            if(state.wheelValue != 0) {
                Scroll(state.wheelValue);
            }
        }
    }
    public void Render(Sf Surface, TimeSpan delta) {
        Surface.Clear();
        if(count <= 26) {
            return;
        }
        Surface.DrawRect(0, 0, 1, 26, new() { width = Line.Single, f = Color.Gray });

        var (barStart, barEnd) = GetBarRange();

        var (f, b) =
            clickOnBar ?
                (Color.Black, Color.White) :
            mouseOnBar ?
                (Color.White, Color.Gray) :
                (Color.White, Color.Black);

        Surface.DrawRect(0, barStart, 1, barEnd - barStart + 1, new() {
            width = Line.Double,
            f = f,
            b = b
        });
    }
}

public static class SListWidget {
    public static ListWidget<Item> UsefulItems(IScene prev, PlayerShip player) {
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

        return screen = new(
            "Use Item",
            usable,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installedInvokable.Contains(i) ? "[*] " : "[c] ")}{i.type.name}";
        List<Tile[]> GetDesc(Item i) {
            var invoke = i.type.Invoke;
            var result = SMenu.GenerateDesc(i);
            if (invoke != null) {
                var action = $"[Enter] {invoke.GetDesc(player, i)}";
                result.Add(Tile.Arr(action, Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item i) {
            i.type.Invoke?.Invoke(prev, player, i, Update);
            screen.list.UpdateIndex();
        }
        void Update() {
            UpdateList();
            screen.list.UpdateIndex();
        }
        void Escape() {
            prev.Close();

        }
    }
    public static ListWidget<Device> ManageDevices(IScene prev, PlayerShip player) {
        ListWidget<Device> screen = null;
        var disabled = player.energy.off;
        var powered = player.devices.Powered;
        return screen = new(
            "Manage Devices",
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            ) { groupMode=false };
        string GetName(Device d) => $"{(disabled.Contains(d) ? "[ ]" : "[*]")} {d.source.type.name}";
        List<ColoredString> GetDesc(Device d) {
            var result = SMenu.GenerateDesc(d);
            result.Add(Tile.Arr($"Status: {(disabled.Contains(d) ? "OFF" : "ON")}"));
            result.Add(Tile.Arr(""));
            var off = disabled.Contains(d);
            var word = (off ? "Enable" : "Disable");
            result.Add(Tile.Arr($"[Enter] {word} this device", Color.Yellow, Color.Black));
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
            prev.Show();
        }
    }
    public static ListWidget<IDockable> DockList(IScene prev, List<IDockable> d, PlayerShip player) {
        ListWidget<IDockable> screen = null;
        return screen = new(
            "Docking",
            d,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            ) { groupMode=false};
        string GetName(IDockable d) => $"{d.name, -24}"; //{(d.position - player.position).magnitude,4:0}
        List<ColoredString> GetDesc(IDockable d) => [
			Tile.Arr($"Distance: {(d.position - player.position).magnitude:0}")
        ];
        void InvokeItem(IDockable d) {
            player.dock = new() { Target = d, Offset = d.GetDockPoints().First() };
            Escape();
        }
        void Escape() {
            prev.Show();
        }
    }
    public static ListWidget<AIShip> Communications(IScene prev, PlayerShip player) {
        ListWidget<AIShip> screen = null;
        screen = new(
            "Communications",
            player.wingmates,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        Dictionary<string, Action> commands = new();
        //var buttons = new ButtonPane();
        void UpdateButtons(AIShip s) {
            //buttons.Children.Clear();
            if(s == null) {
                return;
            }
            EscortShip GetEscortOrder(int i) {
                int root = (int)Math.Sqrt(i);
                int lower = root * root;
                int upper = (root + 1) * (root + 1);
                int range = upper - lower;
                int index = i - lower;
                return new EscortShip(player, XY.Polar(
                        -(Math.PI * index / range), root * 2));
            }
            switch (s.behavior) {
                case Wingmate w:
                    commands["Form Up"] = () => {
                        player.AddMessage(new Transmission(s, $"Ordered {s.name} to Form Up"));
                        w.order = GetEscortOrder(0);
                    };
                    if (s.devices.Weapon.FirstOrDefault(w => w.projectileDesc.tracker != 0) is Weapon weapon) {
                        commands["Fire Tracker"] = () => {
                            if (!player.GetTarget(out ActiveObject target)) {
                                player.AddMessage(new Transmission(s, $"{s.name}: Firing tracker at nearby enemies"));
                                w.order = new FireTrackerNearby(weapon);
                                return;
                            }
                            player.AddMessage(new Transmission(s, $"{s.name}: Firing tracker at target"));
                            w.order = new FireTrackerAt(weapon, target);
                        };
                    }
                    commands["Attack Target"] = () => {
                        if (player.GetTarget(out ActiveObject target)) {
                            w.order = new AttackTarget(target);
                            player.AddMessage(new Transmission(s, $"{s.name}: Attacking target"));
                        } else {
                            player.AddMessage(new Transmission(s, $"{s.name}: No target selected"));
                        }
                    };
                    commands["Wait"] = () => {
                        w.order = new GuardAt(new TargetingMarker(player, "Wait", s.position));
                        player.AddMessage(new Transmission(s, $"Ordered {s.name} to Wait"));
                    };
                    break;
                default:
                    commands["Form Up"] = () => {
                        player.AddMessage(new Message($"Ordered {s.name} to Form Up"));
                        s.behavior = GetEscortOrder(0);
                    };
                    commands["Attack Target"] = () => {
                        if (player.GetTarget(out ActiveObject target)) {
                            var attack = new AttackTarget(target);
                            var escort = GetEscortOrder(0);
                            s.behavior = attack;
                            OrderOnDestroy.Register(s, attack, escort, target);
                            player.AddMessage(new Message($"Ordered {s.name} to Attack Target"));
                        } else {
                            player.AddMessage(new Message($"No target selected"));
                        }
                    };
                    break;
            }
#if false
            foreach(var(key, action) in commands) {
                buttons.Add(key, () => {
                    action();
                    screen.list.UpdateIndex();
                });
            }
#endif
        }
        //screen.Children.Add(buttons);
        screen.list.indexChanged += UpdateButtons;
        return screen;

        string GetName(AIShip s) => $"{s.name,-24}";
        List<ColoredString> GetDesc(AIShip s) => new() {
			Tile.Arr($"Distance: {(s.position - player.position).magnitude:0}"),
			Tile.Arr($"Order: {s.behavior.GetOrderName()}")
        };
        void InvokeItem(AIShip d) {
            
        }
        void Escape() {
            prev.Show();
        }
    }
}

#if false
public class ButtonPane {
    public ButtonPane() {}
    public void ProcessKeyboard(KB keyboard) {
        int i = 0;
        foreach(var button in buttons) {
            if(keyboard[KC.NumPad1 + i] == KS.Pressed){
                button.leftClick?.Invoke();
            }
            i++;
        }
    }
    List<LabelButton> buttons = new();
    public void Add(string label, Action clicked) {
        var index = Children.Count + 1;
        var b = new LabelButton($"{index}> {label}", clicked) {
            Position = new Point(0, index),
        };
        buttons.Add(b);
    }
    public void Clear() {
        buttons.Clear();
    }
}
#endif

public class ListWidget<T>:IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	public ListPane<T> list;
    public DescPanel<T> descPane;

    public ref string title => ref list.title;
    public ref bool groupMode => ref list.groupMode;


	public Action escape;
    public Sf Surface;
    public ListWidget(string title, IEnumerable<T> items, ListPane<T>.GetName getName, DescPanel<T>.GetDesc getDesc, ListPane<T>.Invoke invoke, Action escape){
        descPane = new DescPanel<T>();
        list = new(title, items, getName, UpdateDesc) {
            invoke = invoke,
        };

        void UpdateDesc(T i) {
            if (i != null) {
                descPane.SetInfo(getName(i), getDesc(i));
            } else {
                descPane.SetInfo("", new());
            }
        }

        this.escape = escape;
    }
    public void ProcessKeyboard(KB keyboard) {
        if (keyboard[KC.Escape] == KS.Press) {
            escape?.Invoke();
        } else {
            list.HandleKey(keyboard);
        }
    }
}

