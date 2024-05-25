
global using Dbg = System.Diagnostics.Debug;
using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RogueFrontier;

public interface IConsoleHook {

}
public class ArenaScreen : IScene, Ob<PlayerShip.Destroyed> {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public void Observe(PlayerShip.Destroyed ev) => Reset();

	TitleScreen prev;
	ShipControls settings;
	System World;
	public XY camera;
	public Dictionary<(int, int), Tile> tiles;
	XY screenCenter;
	Hand mouse;
	public ActiveObject pov;
	ActiveObject nearest;
	public Mainframe mainframe;

	bool passTime = true;
	Sf sf_world;
	Sf sf_ui;
	int Width => sf_world.Width;
	int Height => sf_world.Height;

	public List<SfControl> controls = new();

	public ArenaScreen(TitleScreen prev, ShipControls settings, System World) {
		this.prev = prev;
		this.sf_world = new Sf(prev.Width, prev.Height, Fonts.FONT_8x8);
		this.sf_ui = new Sf(prev.Width, prev.Height, Fonts.FONT_6x8);
		this.settings = settings;
		this.World = World;
		this.camera = (0.1, 0.1);
		this.tiles = new();
		this.screenCenter = (Width / 2, Height / 2);
		this.mouse = new();
		{
			int x = 1, y = 1;
			Enumerable.ToList([
				"[A] Assume control of nearest ship",
				"[F] Lock camera onto nearest ship",
				"[K] Kill nearest ship",
				"[Tab] Show/Hide Menus",
				"[Hold left] Move camera"
			]).ForEach((string s) => controls.Add(new SfText(sf_ui, (x, y++), s)));
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
				var label = new SfText(sf_ui, (x,y++), "Sovereign");
				var sovereignField = new SfField(sf_ui, (x, y++), 24);
				var buttons = new SfLinkGroup(controls, (x, y++), sf_ui);
				sovereignField.TextChanged += _ => UpdateSovereignListing();
				controls.Add(label);
				controls.Add(sovereignField);
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
					label.text = Tile.Arr($"Sovereign: {sovereign?.codename ?? "None"}");
			}
			void AddStationField() {
				var x = 1 + 32;
				var y = 7;
				controls.Add(new SfText(sf_ui, (x,y++), "Spawn Station"));
				var stationField = new SfField(sf_ui, (x, y++), 24);
				var buttons = new SfLinkGroup(controls, (x,y++), sf_ui);
				stationField.TextChanged += _ => UpdateStationListing();
				controls.Add(stationField);
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

				controls.Add(new SfText(sf_ui, (x, y++), "Spawn Ship"));
				var shipField = new SfField(sf_ui, (x, y++), 24);
				var buttons = new SfLinkGroup(controls, (x, y++), sf_ui);
				shipField.TextChanged += _ => UpdateShipListing();
				controls.Add(shipField);
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

				controls.Add(new SfText(sf_ui, (x, y++), "Cargo"));
				var cargoField = new SfField(sf_ui, (x, y++) , 24);
				var addButtons = new SfLinkGroup(controls, (x,y++), sf_ui);
				var removeButtons = new SfLinkGroup(controls, (x, y + 18), sf_ui);

				cargoField.TextChanged += _ => UpdateAddListing();
				controls.Add(cargoField);
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

				controls.Add(new SfText(sf_ui, (x,y++), "Devices"));
				var deviceField = new SfField(sf_ui, (x, y++), 24);
				var addButtons = new SfLinkGroup(controls,(x,y++),sf_ui);
				var removeButtons = new SfLinkGroup(controls, (x, y+18), sf_ui);

				deviceField.TextChanged += _ => UpdateAddListing();
				controls.Add(deviceField);
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
#if false
		foreach (var c in Children) {
			c.IsVisible = false;
		}
#endif
	}
	bool showUI = true;
	public void ToggleArena() {
		showUI = !showUI;
	}
	public void Reset() => Reset(mainframe.camera.position);
	public void Reset(XY camera) {

		this.camera = camera;
		mainframe = null;
#if false
		foreach (var c in Children) {
			c.IsVisible = true;
		}
#endif
	}
	private void UpdatePresent() {
		World.UpdateAdded();
		World.UpdateRemoved();
		tiles.Clear();
		World.PlaceTiles(tiles);
	}
	public void Update(TimeSpan timeSpan) {
		if (mainframe != null) {
			mainframe.Update(timeSpan);
			//IsFocused = true;
			return;
		}

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
			Heading.Crosshair(World, nearest.position, ABGR.Yellow);
		}
	}
	public void Render(TimeSpan delta) {
		if (mainframe != null) {
			mainframe.Render(delta);
			return;
		}


		sf_world.Clear();
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				var g = sf_world.GetGlyph(x, y);

				var offset = new XY(x, Height - y) - screenCenter;
				var location = camera + offset;
				Tile t;
				if (g == 0 || g == ' ' || ABGR.A(sf_world.GetFront(x, y)) == 0) {
					if (tiles.TryGetValue(location.roundDown, out t)) {
						if (t.Background == ABGR.Transparent) {
							t = t with { Background = World.backdrop.GetBackground(location, camera) };
						}
					} else {
						t = World.backdrop.GetTile(location, camera);
					}
					sf_world.SetTile(x, y, t);
				} else {
					sf_world.SetBack(x, y, World.backdrop.GetBackground(location, camera));
				}
			}
		}
		Draw(sf_world);
		if(showUI) {
			sf_ui.Clear();
			controls.ForEach(c => c.Render(delta));
			Draw(sf_ui);
		}
	}
	public void HandleKey(KB kb) {
		if(showUI)
			foreach(var c in controls.ToList()) {
				c.HandleKey(kb);
				if(kb.Handled) {
					return;
				}
			}
		if (kb.IsPress(KC.Escape)) {
			if (mainframe != null) {

				if (mainframe.dialog != null) {
					mainframe.HandleKey(kb);
					return;
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
				Go(prev);
			}
		} else if (mainframe != null) {
			mainframe.HandleKey(kb);
			return;
		}

		if (kb.IsPress(KC.Tab)) {
			ToggleArena();
		}
		if (kb.IsPress(KC.Space)) {
			passTime = !passTime;
		}
		//Dbg.WriteLine($"Press: {string.Join(' ', kb.Press)}");
		if (kb.IsPress(KC.A)) {
			if (nearest is AIShip a) {
				a.ship.active = false;
				World.RemoveEntity(a);

				var p = new Player() { Genome = new GenomeType() { name = "Human" } };
				var playerShip = new PlayerShip(p, new BaseShip(a.ship), a.sovereign);

				mainframe = new Mainframe(Width, Height, new Profile(), playerShip);

				mainframe.PlaySound += snd => PlaySound?.Invoke(snd);
				mainframe.Draw += sf => Draw?.Invoke(sf);


				mainframe.camera.position = camera;
				playerShip.onDestroyed += this;
				World.AddEntity(playerShip);
				World.AddEffect(new Heading(playerShip));

				pov = playerShip;

				HideArena();
			}
		}
		if (kb.IsPress(KC.F)) {
			if (pov == nearest) {
				pov = null;
			} else {
				pov = nearest;
			}
		}
		if (kb.IsPress(KC.K) && nearest != null) {
			nearest.Destroy();
			if (kb.IsDown(KC.LeftShift)) {
				foreach (var s in World.entities.all.OfType<ActiveObject>()) {
					s.Destroy();
				}
			}
		}

		foreach (var pressed in kb.Down) {
			var delta = 1 / 3f;
			switch (pressed) {
				case KC.Up:
					camera += new XY(0, delta);
					break;
				case KC.Down:
					camera += new XY(0, -delta);
					break;
				case KC.Right:
					camera += new XY(delta, 0);
					break;
				case KC.Left:
					camera += new XY(-delta, 0);
					break;
			}
		}
	}
	XY prevPos = (0, 0);
	public void HandleMouse (HandState state) {
		if (mainframe != null) {
			mainframe.HandleMouse(state);
			return;
		}

		if(showUI) {
			foreach(var c in controls.ToList()) {
				c.HandleMouse(state);
				if(state.Handled) {
					return;
				}
			}
		}

		mouse.Update(state with { pos = (state.pos.x, Height - state.pos.y) });
		if (mouse.left == Pressing.Down) {
			camera += -((XY)mouse.deltaPos)/8;
		}
	}
}
