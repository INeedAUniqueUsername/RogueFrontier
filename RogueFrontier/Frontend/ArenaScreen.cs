using SadConsole.Input;
using System;
using System.Collections.Generic;
using SadRogue.Primitives;
using Common;
using SadConsole;
using Console = SadConsole.Console;
using System.Linq;
using static SadConsole.Input.Keys;
using ArchConsole;
using static RogueFrontier.PlayerShip;
using System.Text.Json.Serialization;

namespace RogueFrontier;

public interface IConsoleHook {

}
public class ArenaScreen : Console, Ob<PlayerShip.Destroyed> {
    public void Observe(PlayerShip.Destroyed ev) => Reset();

    TitleScreen prev;
    Settings settings;
    System World;
    public XY camera;
    public Dictionary<(int, int), ColoredGlyph> tiles;
    XY screenCenter;
    MouseWatch mouse;
    public ActiveObject pov;
    ActiveObject nearest;
    public Mainframe mainframe;

    bool passTime = true;
    public ArenaScreen(TitleScreen prev, Settings settings, System World) : base(prev.Width, prev.Height) {
        this.prev = prev;
        this.settings = settings;
        this.World = World;
        this.camera = new XY(0.1, 0.1);
        this.tiles = new Dictionary<(int, int), ColoredGlyph>();
        this.screenCenter = new XY(Width / 2, Height / 2);
        this.mouse = new MouseWatch();

        UseKeyboard = true;
        FocusOnMouseClick = true;
        {
            int x = 1, y = 1;
            Children.Add(new Label("[A] Assume control of nearest ship") { Position = new Point(x, y++) });
            Children.Add(new Label("[F] Lock camera onto nearest ship") { Position = new Point(x, y++) });
            Children.Add(new Label("[K] Kill nearest ship") { Position = new Point(x, y++) });
            Children.Add(new Label("[Tab] Show/Hide Menus") { Position = new Point(x, y++) });
            Children.Add(new Label("[Hold left] Move camera") { Position = new Point(x, y++) });
        }
        InitControls();
        void InitControls() {
            var sovereign = Sovereign.Gladiator;
            List<Item> cargo = new List<Item>();
            List<Device> devices = new List<Device>();
            AddSovereignField();
            AddStationField();
            AddShipField();
            AddCargoField();
            AddDeviceField();
            void AddSovereignField() {
                var x = 1;
                var y = 7;
                var label = new Label("Sovereign") { Position = new Point(x, y++) };
                var sovereignField = new TextField(24) { Position = new Point(x, y++) };
                ButtonList buttons = new ButtonList(this, new Point(x, y++));
                sovereignField.TextChanged += _ => UpdateSovereignListing();
                Children.Add(label);
                Children.Add(sovereignField);
                UpdateSovereignLabel();
                UpdateSovereignListing();
                void UpdateSovereignListing() {
                    var text = sovereignField.text;
                    buttons.Clear();
                    var sovereignDict = World.types.GetDict<Sovereign>();
                    int i = 0;
                    foreach (var type in sovereignDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                        buttons.Add(type, () => {
                            sovereign = (sovereign == sovereignDict[type]) ? null : sovereignDict[type];
                            UpdateSovereignLabel();
                        });
                        if (++i > 16) {
                            break;
                        }
                    }
                }
                void UpdateSovereignLabel() =>
                    label.text = new ColoredString($"Sovereign: {sovereign?.codename ?? "None"}");
            }
            void AddStationField() {
                var x = 1 + 32;
                var y = 7;
                Children.Add(new Label("Spawn Station") { Position = new Point(x, y++) });
                var stationField = new TextField(24) { Position = new Point(x, y++) };
                ButtonList buttons = new ButtonList(this, new Point(x, y++));
                stationField.TextChanged += _ => UpdateStationListing();
                Children.Add(stationField);
                UpdateStationListing();
                void UpdateStationListing() {
                    var text = stationField.text;
                    buttons.Clear();
                    var stationTypeDict = World.types.GetDict<StationType>();

                    int i = 0;
                    foreach (var type in stationTypeDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                        buttons.Add(type, () => {
                            var station = new Station(World, stationTypeDict[type], camera);
                            if(sovereign != null) {
                                station.sovereign = sovereign;
                            }
                            if (cargo.Any()) {
                                station.cargo.Clear();
                                station.cargo.UnionWith(cargo.Select(s => new Item(s)));
                            }
                            if (devices.Any()) {
                                station.weapons.Clear();
                                station.weapons.AddRange(devices.Select(d => new Item(d.source).weapon).Where(d => d != null));
                            }

                            World.AddEntity(station);
                            station.CreateSatellites(new() { pos = camera, focus=camera, world=World });
                            station.CreateSegments();
                            station.CreateGuards();

                            UpdatePresent();
                        });

                        if (++i > 16) {
                            break;
                        }
                    }
                }
            }

            void AddShipField() {
                var x = 1 + 32 + 32;
                var y = 7;

                Children.Add(new Label("Spawn Ship") { Position = new Point(x, y++) });
                var shipField = new TextField(24) { Position = new Point(x, y++) };
                ButtonList buttons = new ButtonList(this, new Point(x, y++));
                shipField.TextChanged += _ => UpdateShipListing();
                Children.Add(shipField);
                UpdateShipListing();

                void UpdateShipListing() {
                    var text = shipField.text;
                    buttons.Clear();
                    var shipClassDict = World.types.GetDict<ShipClass>();

                    int i = 0;
                    foreach (var type in shipClassDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                        buttons.Add(type, () => {
                            var ship = new AIShip(new(World, shipClassDict[type], camera), sovereign ?? Sovereign.Gladiator, new AttackNearby());

                            if (cargo.Any()) {
                                ship.cargo.Clear();
                                ship.cargo.UnionWith(cargo.Select(s => new Item(s)));
                            }
                            if (devices.Any()) {
                                ship.devices.Clear();
                                ship.devices.Install(devices.Select(d => {
                                    var source = new Item(d.source);
                                    return (Device)(d switch {
                                        Weapon w => source.weapon,
                                        Shield s => source.shield,
                                        Reactor r => source.reactor,
                                        Service m => source.service,
                                        _ => throw new NotImplementedException()
                                    });
                                }));
                            }

                            World.AddEntity(ship);
                            World.AddEffect(new Heading(ship));

                            UpdatePresent();
                        });

                        if (++i > 16) {
                            break;
                        }
                    }
                }
            }


            void AddCargoField() {
                var x = 1;
                var y = 7 + 18;

                Children.Add(new Label("Cargo") { Position = new Point(x, y++) });
                var cargoField = new TextField(24) { Position = new Point(x, y++) };
                ButtonList addButtons = new ButtonList(this, new Point(x, y++));
                ButtonList removeButtons = new ButtonList(this, new Point(x, y + 18));

                cargoField.TextChanged += _ => UpdateAddListing();
                Children.Add(cargoField);
                UpdateAddListing();



                UpdateRemoveListing();

                void UpdateAddListing() {
                    var text = cargoField.text;
                    addButtons.Clear();
                    var itemDict = World.types.GetDict<ItemType>();

                    int i = 0;
                    foreach (var type in itemDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                        addButtons.Add(type, () => {
                            cargo.Add(new Item(itemDict[type]));
                            UpdateRemoveListing();
                        });


                        if (++i > 16) {
                            break;
                        }
                    }
                }



                void UpdateRemoveListing() {
                    removeButtons.Clear();
                    foreach (var i in cargo) {
                        removeButtons.Add(i.type.codename, () => {
                            cargo.Remove(i);
                            UpdateRemoveListing();
                        });
                    }
                }
            }





            void AddDeviceField() {
                var x = 1 + 32;
                var y = 7 + 18;

                Children.Add(new Label("Devices") { Position = new Point(x, y++) });
                var deviceField = new TextField(24) { Position = new Point(x, y++) };
                ButtonList addButtons = new ButtonList(this, new Point(x, y++));
                ButtonList removeButtons = new ButtonList(this, new Point(x, y + 18));

                deviceField.TextChanged += _ => UpdateAddListing();
                Children.Add(deviceField);
                UpdateAddListing();



                UpdateRemoveListing();

                void UpdateAddListing() {
                    var text = deviceField.text;
                    addButtons.Clear();
                    var itemDict = World.types.GetDict<ItemType>();
                    var keys = itemDict.Keys
                        .OrderBy(k => k)
                        .Where(k => k.Contains(text));

                    int i = 0;
                    foreach (var type in keys) {
                        var item = new Item(itemDict[type]);
                        var device = (Device)item.Get<Reactor>() ?? (Device)item.Get<Shield>() ?? (Device)item.Get<Weapon>() ?? (Device)item.Get<Service>();

                        if (device == null) {
                            continue;
                        }
                        addButtons.Add(type, () => {
                            devices.Add(device);
                            UpdateRemoveListing();
                        });

                        if (++i > 16) {
                            break;
                        }
                    }
                }



                void UpdateRemoveListing() {
                    removeButtons.Clear();
                    foreach (var i in devices) {
                        removeButtons.Add(i.source.type.codename, () => {
                            devices.Remove(i);
                            UpdateRemoveListing();
                        });
                    }
                }
            }
        }
    }

    public void HideArena() {
        foreach (var c in Children) {
            c.IsVisible = false;
        }
    }
    public void ToggleArena() {
        foreach (var c in Children) {
            c.IsVisible = !c.IsVisible;
        }
    }
    public void Reset() => Reset(mainframe.camera.position);
    public void Reset(XY camera) {

        this.camera = camera;
        mainframe = null;
        IsFocused = true;
        foreach (var c in Children) {
            c.IsVisible = true;
        }
    }
    private void UpdatePresent() {
        World.UpdateAdded();
        World.UpdateRemoved();
        tiles.Clear();
        World.PlaceTiles(tiles);
    }
    public override void Update(TimeSpan timeSpan) {
        if (mainframe != null) {
            mainframe.IsFocused = true;
            mainframe.Update(timeSpan);
            IsFocused = true;
            base.Update(timeSpan);
            return;
        }

        base.Update(timeSpan);

        if (passTime) {

            World.UpdateAdded();

            World.UpdateActive(timeSpan.TotalSeconds);
            World.UpdateRemoved();

            tiles.Clear();
            World.PlaceTiles(tiles);

        }

        if (pov?.active == false) {
            pov = null;
        }

        if (pov != null) {
            if (pov.active) {
                UpdateNearest();

                //Smoothly move the camera to where it should be
                if ((camera - pov.position).magnitude < pov.velocity.magnitude / 15 + 1) {
                    camera = pov.position;
                } else {
                    var step = (pov.position - camera) / 15;
                    if (step.magnitude < 1) {
                        step = step.normal;
                    }
                    camera += step;
                }
            } else {
                pov = null;
                UpdateNearest();
            }
        } else {
            UpdateNearest();
        }

        void UpdateNearest() {
            XY worldPos = new XY(mouse.nowPos) - screenCenter + camera;
            nearest = World.entities.all.OfType<ActiveObject>().OrderBy(e => (e.position - worldPos).magnitude).FirstOrDefault();
        }


        if (nearest != null) {
            Heading.Crosshair(World, nearest.position, Color.Yellow);
        }
    }
    public override void Render(TimeSpan drawTime) {
        if (mainframe != null) {
            mainframe.Render(drawTime);
            return;
        }

        this.Clear();
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var g = this.GetGlyph(x, y);

                var offset = new XY(x, Height - y) - screenCenter;
                var location = camera + offset;
                if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {


                    if (tiles.TryGetValue(location.roundDown, out var tile)) {
                        if (tile.Background == Color.Transparent) {
                            tile.Background = World.backdrop.GetBackground(location, camera);
                        }
                        this.SetCellAppearance(x, y, tile);
                    } else {
                        this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                    }
                } else {
                    this.SetBackground(x, y, World.backdrop.GetBackground(location, camera));
                }
            }
        }
        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {

        if (info.IsKeyPressed(Escape)) {
            if (mainframe != null) {

                if (mainframe.sceneContainer.Children.Any()) {
                    return mainframe.ProcessKeyboard(info);
                }

                World.RemoveEntity(mainframe.playerShip);
                var aiShip = new AIShip(mainframe.playerShip.ship, mainframe.playerShip.sovereign, new AttackNearby());
                World.AddEntity(aiShip);
                World.AddEffect(new Heading(aiShip));

                pov = aiShip;
                Reset(mainframe.camera.position);


            } else {
                prev.pov = null;
                prev.camera = camera;
                SadConsole.Game.Instance.Screen = prev;
                prev.IsFocused = true;
            }
        } else if (mainframe != null) {
            return mainframe.ProcessKeyboard(info);
        }

        if (info.IsKeyPressed(Tab)) {
            ToggleArena();
        }
        if (info.IsKeyPressed(Keys.Space)) {
            passTime = !passTime;
        }
        if (info.IsKeyPressed(A)) {
            if (nearest is AIShip a) {
                a.ship.active = false;
                World.RemoveEntity(a);

                var p = new Player() { Settings = settings, Genome = new GenomeType() { name = "Human" } };
                var playerShip = new PlayerShip(p, new BaseShip(a.ship), a.sovereign);

                mainframe = new Mainframe(Width, Height, null, playerShip) { IsFocused = true };
                mainframe.camera.position = camera;
                playerShip.onDestroyed += this;
                World.AddEntity(playerShip);
                World.AddEffect(new Heading(playerShip));

                pov = playerShip;

                HideArena();
            }
        }
        if (info.IsKeyPressed(Keys.F)) {
            if (pov == nearest) {
                pov = null;
            } else {
                pov = nearest;
            }
        }
        if (info.IsKeyPressed(K) && nearest != null) {
            nearest.Destroy();
            if (info.IsKeyDown(LeftShift)) {
                foreach (var s in World.entities.all.OfType<ActiveObject>()) {
                    s.Destroy();
                }
            }
        }

        foreach (var pressed in info.KeysDown) {
            var delta = 1 / 3f;
            switch (pressed.Key) {
                case Keys.Up:
                    camera += new XY(0, delta);
                    break;
                case Keys.Down:
                    camera += new XY(0, -delta);
                    break;
                case Keys.Right:
                    camera += new XY(delta, 0);
                    break;
                case Keys.Left:
                    camera += new XY(-delta, 0);
                    break;
            }
        }

        return base.ProcessKeyboard(info);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        if (mainframe != null) {
            return mainframe.ProcessMouse(state);
        }

        mouse.Update(state, IsMouseOver);
        mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
        if (mouse.left == ClickState.Held) {
            camera += new XY(mouse.prevPos - mouse.nowPos);
        }

        return base.ProcessMouse(state);
    }
}
