using LibGamer;
using System;
using System.Linq;
namespace RogueFrontier;
using Keys = KC;
class ShipMenu : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public IScene prev;
    public PlayerShip playerShip;
    public Timeline story;
    public Sf sf;
	//Idea: Show an ASCII-art map of the ship where the player can walk around
	public ShipMenu(IScene prev, Sf sf_prev, PlayerShip playerShip, Timeline story) {
        this.sf = new Sf(sf_prev.Width, sf_prev.Height, Fonts.FONT_6x8);
        this.prev = prev;
        this.playerShip = playerShip;
        this.story = story;
        int x = 1, y = sf.Height - 9;
#if false
        Children.Add(new LabelButton("[A] Active Devices", ShowPower) { Position = (x, y++) });
        Children.Add(new LabelButton("[C] Cargo", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[D] Devices", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[I] Invoke Items", ShowInvokable) { Position = (x, y++) });
        Children.Add(new LabelButton("[M] Missions", ShowMissions) { Position = (x, y++) });
        Children.Add(new LabelButton("[R] Refuel", ShowRefuel) { Position = (x, y++) });
#endif
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        sf.RenderBackground();
        var name = playerShip.shipClass.name;
        var x = sf.Width / 4 - name.Length / 2;
        var y = 4;
        void Print(int x, int y, string s) =>
            sf.Print(x, y, s, ABGR.White, ABGR.Black);
        void Print2(int x, int y, string s) =>
            sf.Print(x, y, s, ABGR.White, ABGR.SetA(ABGR.Black, 102));
        Print(x, y, name);
        
        y++;
        x = 1;
        Print(x, y, $"{$"Thrust:    {playerShip.shipClass.thrust}",-16}{$"Rotate acceleration: {playerShip.shipClass.rotationAccel,3} deg/s^2"}");
        y++;
        Print(x, y, $"{$"Max Speed: {playerShip.shipClass.maxSpeed}",-16}{$"Rotate deceleration: {playerShip.shipClass.rotationDecel,3} deg/s^2"}");
        y++;
        Print(x, y, $"{"",-16}{$"Rotate max speed:    {playerShip.shipClass.rotationMaxSpeed * 30,3} deg/s^2"}");
        x = sf.Width / 2;
        y = 2;
        var pl = playerShip.person;
        Print(x, y++, "[Player]");
        Print(x, y++, $"Name:       {pl.name}");
        Print(x, y++, $"Identity:   {pl.Genome.name}");
        Print(x, y++, $"Money:      {pl.money}");
        Print(x, y++, $"Title:      Harmless");
        y++;
        var reactors = playerShip.ship.devices.Reactor;
        if (reactors.Any()) {
            Print(x, y++, "[Reactors]");
            foreach (var r in reactors) {
                Print(x, y++, $"{r.source.type.name}");
                Print(x, y++, $"Output:     {-r.energyDelta}");
                Print(x, y++, $"Max output: {r.desc.maxOutput}");
                Print(x, y++, $"Fuel:       {r.energy:0}");
                Print(x, y++, $"Max fuel:   {r.desc.capacity}");
                y++;
            }
        }
        var ds = playerShip.ship.damageSystem;
        if (ds is HP hp) {
            Print(x, y++, "[Health]");
            Print(x, y++, $"HP: {hp.hp}");
            y++;
        } else if (ds is LayeredArmor las) {
            Print(x, y++, "[Armor]");
            foreach (var a in las.layers) {
                Print(x, y++, $"{a.source.type.name}: {a.hp} / {a.maxHP}");
            }
            y++;
        }
        var weapons = playerShip.ship.devices.Weapon;
        if (weapons.Any()) {
            Print(x, y++, "[Weapons]");
            foreach (var w in weapons) {
                Print(x, y++, $"{w.source.type.name,-32}{w.GetBar(8)}");
                Print(x, y++, $"Projectile damage: {w.desc.damageHP.str}");
                Print(x, y++, $"Projectile speed:  {w.desc.missileSpeed}");
                Print(x, y++, $"Shots per second:  {60f / w.desc.fireCooldown:0.00}");
                if (w.ammo is ChargeAmmo c) {
                    Print(x, y++, $"Ammo Remaining:    {c.charges}");
                }
                y++;
            }
        }
        var misc = playerShip.ship.devices.Installed.OfType<Service>();
        if (misc.Any()) {
            Print(x, y++, "[Misc]");
            foreach (var m in misc) {
                Print(x, y++, $"{m.source.type.name}");
                y++;
            }
        }
        if (playerShip.messages.Any()) {
            Print(x, y++, "[Messages]");
            foreach (var m in playerShip.messages) {
                sf.Print(x, y++, m.Draw());
            }
            y++;
        }
        Draw(sf);
    }
    public void HandleKey(KB info) {
        var pr = info.IsPress;
        if (pr(KC.S) || pr(KC.Escape)) {
#if false
            Tones.pressed.Play();
#endif
            Go(prev);
        } else if (pr(KC.A)) {
            ShowPower();
        } else if (pr(KC.C)) {
            ShowCargo();
        } else if (pr(KC.D)) {
            ShowLoadout();
        } else if (pr(KC.I)) {
            ShowInvokable();
        } else if (pr(KC.L)) {
            ShowLogs();
        } else if (pr(KC.M)) {
            ShowMissions();
        } else if (pr(KC.R)) {
            ShowRefuel();
        }
    }
    public void ShowInvokable() => Go(SMenu.Usable(new(this, sf, playerShip)));
    public void ShowPower() => Go(SMenu.DeviceManager(new(this, sf, playerShip)));
    public void ShowCargo() => Go(SMenu.Cargo(new(this, sf, playerShip)));
    public void ShowLoadout() => Go(SMenu.Installed(new(this, sf, playerShip)));
    public void ShowLogs() => Go(SMenu.Logs(new(this, sf, playerShip)));
    public void ShowMissions() => Go(SMenu.Missions(new(this, sf, playerShip), story));
    public void ShowRefuel() => Go(SMenu.RefuelReactor(new(this, sf, playerShip)));
}