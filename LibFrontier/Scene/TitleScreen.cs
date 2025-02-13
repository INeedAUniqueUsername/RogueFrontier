using Common;
using LibGamer;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
namespace RogueFrontier;
public class TitleScreen : IScene {
	public Action<IScene> Go { set; get; }
	public Action<Sf> Draw { set; get; }
	public Action<SoundCtx> PlaySound { get; set; }
	ConfigPane config;
	//LoadPane load;
	//Console credits;
	public Profile profile;
	public World World;
	public static string[] title = File.ReadAllText($"{Assets.ROOT}/sprites/Title.txt").Replace("\r\n", "\n").Split('\n');
	public ShipControls settings;
	public AIShip pov;
	public int povTimer;
	public List<Message> povDesc;
	//XY screenCenter;
	public XY camera;
	public Dictionary<(int, int), Tile> tiles;
	public byte[] titleMusic = File.ReadAllBytes($"{Assets.ROOT}/music/Title.wav");
	public byte[] crawlMusic = File.ReadAllBytes($"{Assets.ROOT}/music/Crawl.wav");
	public int Width => sf.GridWidth;
	public int Height => sf.GridHeight;
	public Sf sf;
	private List<SfLink> buttons = [];
	public TitleScreen(int Width, int Height, World World) {
		this.sf = new Sf(Width, Height, Fonts.FONT_8x8);
		this.World = World;

		profile = Profile.Load(out var p) ? p : new Profile();
		profile.Save();

		//screenCenter = new XY(Width / 2, Height / 2);

		camera = new(0, 0);
		tiles = new();

		int x = 2;
		int y = 9;
		//var fs = FontSize * 1;
		foreach (var (s,a) in new Dictionary<string, Action>{
			["[Enter]   Traveler Mode - New Game"] = StartGame,
			["[Shift-L] Traveler Mode - Load Game"] = null,
			["[Shift-F] Fortress Mode - New Game"] = null,
			["[Shift-A] Arena Mode"] = StartArena,
			["[Shift-C] Controls"] = null,
			["[Shift-Z] Credits"] = null,
			["[Escape]  Exit"] = Exit
		}) {
			var b = new SfLink(sf, (x, y++), s, a);
			b.Draw += sf => Draw?.Invoke(sf);
			buttons.Add(b);
		};
		var f = "Settings.json";
		if (File.Exists(f)) {
			settings = SaveGame.Deserialize<ShipControls>(File.ReadAllText(f));
		} else {
			settings = ShipControls.standard;
		}
		config = new(sf, settings);
		//load = new(profile);
#if false
		credits = new(48, 64) { Position = new(0, 30), FontSize = fs };
		
		y = 0;
		credits.Children.Add(new Label("[Credits]") { Position = new(0, y++) });
		y++;
		credits.Children.Add(new Label("     Developer: Alex Chen") { Position = new(0, y++) });
		credits.Children.Add(new Label("Special Thanks: Andy De George") { Position = new(0, y++) });
		credits.Children.Add(new Label("Special Thanks: George Moromisato") { Position = new(0, y++) });

		y++;
		credits.Children.Add(new Label("Rogue Frontier is an independent project inspired by Transcendence") { Position = new(0, y++) });
		credits.Children.Add(new Label("Transcendence is a trademark of Kronosaur Productions") { Position = new(0, y++) });
#endif
	}
	public void StartArena () =>
		Go(new ArenaScreen(this, settings, World) {
			camera = camera,
			pov = pov
		});
	private void StartGame () {
		//Tones.pressed.Play();
		PlayerCreator pc = null;
		pc = new PlayerCreator(this, sf, World, settings, StartCrawl);
		Go(new TitleSlideIn((sf, this), (pc.sf_img, pc)));
		void StartCrawl (ShipSelectorModel context) {
			var loc = $"{AppDomain.CurrentDomain.BaseDirectory}/save/{context.playerName}";
			string file;
			do { file = $"{loc}-{new Rand().NextInteger(9999)}.sav"; }
			while(File.Exists(file));
			var player = new Player() {
				file = file,
				name = context.playerName,
				Genome = context.playerGenome,
				money = 2000
			};
			var (playable, index) = (context.playable, context.shipIndex);
			var playerClass = playable[index];
			Scene1();
			void Scene1() {
				RogueFrontier.IntroCrawl crawl = null;

				var next = CreateWorld();
				pc.Go?.Invoke(crawl = new(sf.GridWidth, sf.GridHeight, next));

				pc.PlaySound?.Invoke(new SoundCtx(File.ReadAllBytes($"{Assets.ROOT}/music/Crawl.wav"), 33));
				//Task.Run(CreateWorld);

				Action CreateWorld () {
					var universeDesc = new UniverseDesc(World.types, XElement.Parse(
						File.ReadAllText($"{Assets.ROOT}/scripts/Universe.xml")
						));
					//Name is seed
					var seed = player.name.GetHashCode();
					var u = new Universe(universeDesc, World.types, new(seed));

					var start = u.GetAllEntities().OfType<FixedMarker>().First(m => m.Name == "Start");
					var w = start.world;
					start.active = false;
					var playerStart = start.position;
					var playerSovereign = w.types.Lookup<Sovereign>("sovereign_player");
					var playerShip = new PlayerShip(player, new(w, playerClass, playerStart), playerSovereign);
					playerShip.AddMessage(new Message("Welcome to the Rogue Frontier!"));

					w.AddEffect(new Heading(playerShip));
					w.AddEntity(playerShip);

					AddStarterKit(playerShip);
					/*
					File.WriteAllText(file, JsonConvert.SerializeObject(new LiveGame() {
						player = player,
						world = World,
						playerShip = playerShip
					}, SaveGame.settings));
					*/
					return Transition0;
					void Transition0 () {
						FlashTransition ft = null;
						ft = new FlashTransition(Width, Height, crawl.sf, Transition);
						crawl.Go(ft);
						void Transition () {
							PausePrev p = null;
							ft.Go(p = new PausePrev(ft, Transition2, 1));
							void Transition2 () {
								PlainCrawlScreen crawl2 = null;
								crawl2 = new PlainCrawlScreen(Width, Height, "Today has been a long time in the making.    \n\n" + ((new Random(seed).Next(5) + new Random().Next(2)) switch {
									1 => "Maybe history will remember.",
									2 => "Tomorrow will be forever.",
									3 => "The future will not be so far.",
									_ => "Maybe all of it will have been for something.",
								}), Transition3a);
								p.Go(crawl2);
								void Transition3a () {
									PausePrev pause = null;
									crawl2.Go(pause = new PausePrev(crawl2, Transition3, 2));
									void Transition3 () {
										var playerMain = new Mainframe(Width, Height, profile, playerShip);
										playerMain.music = new SoundCtx(crawlMusic, 50);
										playerMain.HideUI();
										playerShip.onDestroyed += playerMain;
										playerMain.Update(new());
										playerMain.PlaceTiles(new());
										playerMain.RenderWorld(new());
										
										PausePrev p = null;
										p = new PausePrev(playerMain, Transition4, 1);

										pause.Go(new FadeIn(p, playerMain.sf));
										void Transition4 () {
											p.Go(playerMain);
											playerMain.ShowUI();
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
#if false

	private void ClearMenu() {
		foreach (var c in new Console[] { config, load, credits }) {
			Children.Remove(c);
		}
	}
	private void StartCredits() {
		if (Children.Contains(credits)) {
			Children.Remove(credits);
		} else {
			ClearMenu();
			Children.Add(credits);
		}
	}
	private void StartConfig() {
		if (Children.Contains(config)) {
			Children.Remove(config);
		} else {
			ClearMenu();
			Children.Add(config);
			config.Reset();
		}
	}
	private void StartLoad() {
		if (Children.Contains(load)) {
			Children.Remove(load);
		} else {
			ClearMenu();
			Children.Add(load);
			load.Reset();
		}
	}
	private void StartProfile() {
		if (Children.Contains(load)) {
			Children.Remove(load);
		} else {
			ClearMenu();
			Children.Add(load);
			load.Reset();
		}
	}
	*/
#endif
	public void UpdatePOVDesc () {
		povDesc = new List<Message> {
					new Message(pov.name),
				};
		if(pov.hull is LayeredArmor las) {
			povDesc.AddRange(las.GetDesc().Select((m, i) => new Message(string.Concat(m.Select(t => (char)t.Glyph)))));
		} else if(pov.hull is HP hp) {
			povDesc.Add(new Message($"HP: {hp}"));
		}
		foreach(var device in pov.ship.devices.Installed) {
			povDesc.Add(new Message(device.source.type.name));
		}
	}
	private void Exit() {
		Environment.Exit(0);
	}
	public void Update(TimeSpan timeSpan) {
		World.UpdateActive(timeSpan.TotalSeconds);
		World.UpdatePresent();
		tiles.Clear();
		World.PlaceTiles(tiles);

		while(World.entities.all.OfType<IShip>().Count() < 100) {
			var shipClasses = World.types.Get<ShipClass>();
			var shipClass = shipClasses.ElementAt(World.karma.NextInteger(shipClasses.Count));
			var angle = World.karma.NextDouble() * PI * 2;
			var distance = World.karma.NextInteger(10, 20);
			var center = World.entities.all.FirstOrDefault()?.position ?? new XY(0, 0);
			var ship = new BaseShip(World, shipClass, center + XY.Polar(angle, distance));
			var enemy = new AIShip(ship, Sovereign.Gladiator, new AttackNearby());
			World.AddEntity(enemy);
			World.AddEffect(new Heading(enemy));
			//Update now in case we need a POV
			World.UpdatePresent();
		}
		if (pov == null || povTimer < 1) {
			pov = World.entities.all.OfType<AIShip>().OrderBy(s => (s.position - camera).magnitude).First();
			UpdatePOVDesc();
			povTimer = 150;
		} else if (!pov.active) {
			povTimer--;
		}

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
	}
	public void Render(TimeSpan delta) {
		sf.Clear();
		//Wait until we are focused to print the POV desc
		//This will happen when TitleSlide transition finishes
		if (true) {
			int descX = Width / 2;
			int descY = Height * 3 / 4;
			bool indent = false;
			foreach (var line in povDesc) {
				line.Update(delta.TotalSeconds);
				var lineX = descX + (indent ? 8 : 0);
				sf.Print(lineX, descY, line.Draw());
				indent = true;
				descY++;
			}
		}
		foreach(var x in Width) {
			foreach(var y in Height) {
				var g = sf.GetGlyph(x, y);
				var offset = new XY(x, Height - y) - new XY(Width / 2, Height / 2);
				var location = camera + offset;
				if (g == 0 || g == ' ' || ABGR.A(sf.GetFront(x, y)) == 0) {
					if (tiles.TryGetValue(location.roundDown, out var tile)) {
						if (ABGR.A(tile.Background) < 255) {
							tile = tile with { Background = ABGR.Blend(World.backdrop.GetBackground(location, camera), tile.Background) };
						}
					} else {
						tile = World.backdrop.GetTile(location, camera);
					}
					sf.SetTile(x, y, tile);
				} else {
					sf.SetBack(x, y, World.backdrop.GetBackground(location, camera));
				}
			}
		}


		var titleY = 0;

		foreach(var line in title) {
			int x = 0;
			foreach(var c in line) {
				if(c != ' ') {
					sf.Print(x, titleY, c, ABGR.White, ABGR.Black);
				}
				x++;
			}
			titleY++;

		}
		config.Render(delta);

		buttons.ForEach(b => b.Render(delta));

		Draw?.Invoke(sf);

	}
	Hand mouse = new();
	public void HandleMouse(HandState state) {
		mouse.Update(state.OnRect(sf.rect));
		if(mouse.nowOn) {
			foreach (var item in buttons){
				item.HandleMouse(state);
			}
		}
		return;
	}
	public void HandleKey (KB info) {
		if(info.IsPress(KC.K)) {
			if(pov.active) {
				pov.Destroy(pov);
			}
		}
		if (info.IsPress(KC.Enter)) {
			PlaySound(Tones.pressed);
			StartGame();
		}

		if(info.IsDown(KC.LeftShift)) {
			if(info.IsPress(KC.A)) {
				StartArena();
			}
			if(info.IsPress(KC.C)) {
				//StartConfig();
			}
			if(info.IsPress(KC.L)) {
				//StartLoad();
			}
			if(info.IsPress(KC.P)) {
				//StartProfile();
			}
			if(info.IsPress(KC.Z)) {
				//StartCredits();
			}
			if(info.IsPress(KC.G)) {
				QuickStart();
			}
		}
#if false
		if (info.IsKeyPressed(Escape)) {
			if (Children.Contains(load)) {
				Children.Remove(load);
			} else if (Children.Contains(config)) {
				Children.Remove(config);
			} else {
				Program.StartRegular();
			}
		}
#endif
	}
#if true
	public void QuickStart() {

		if (false) {
			Go(new OutroCrawl(Width, Height, () => {
				Go(new TitleScreen(Width, Height, World));
			}));
			return;
		}

		var loc = $"{AppDomain.CurrentDomain.BaseDirectory}/save/Debug";
		string file;
		do { file = $"{loc}-{new Random().Next(9999)}.sav"; }
		while (File.Exists(file));
		Player player = new Player() {
			//Settings = settings,
			file = file,
			name = "Player",
			Genome = World.types.Get<GenomeType>().First()
		};
		var universeDesc = new UniverseDesc(World.types, XElement.Parse(
			File.ReadAllText("Assets/scripts/Universe.xml")
			));
		//Name is seed
		var seed = player.name.GetHashCode();
		Universe u = new Universe(universeDesc, World.types, new Rand(seed));

		var quickStartClass = "ship_clark";
		var ent = u.GetAllEntities().OfType<FixedMarker>().ToList();
		var marker = ent.First(e => e.Name == "Start");
		var w = marker.world;
		var playerClass = w.types.Lookup<ShipClass>(quickStartClass);
		var playerStart = marker.position;
		var playerSovereign = w.types.Lookup<Sovereign>("sovereign_player");
		var playerShip = new PlayerShip(player, new BaseShip(w, playerClass, playerStart), playerSovereign);
		//playerShip.powers.Add(new Power(w.types.Lookup<PowerType>("power_declare")));
		//playerShip.powers.AddRange(w.types.Get<PowerType>().Select(pt => new Power(pt)));
		playerShip.AddMessage(new Message("Welcome to Rogue Frontier!"));

		playerShip.powers.Add(new(w.types.Lookup<PowerType>("power_silence_dictator")));
		playerShip.powers.Add(new(w.types.Lookup<PowerType>("power_execute_dictator")));

		w.AddEffect(new Heading(playerShip));
		w.AddEntity(playerShip);

		foreach(var i in 1..100)
		playerShip.cargo.Add(new Item(w.types.Lookup<ItemType>("item_orator_charm_silence")));

		AddStarterKit(playerShip);
		//new LiveGame(w, player, playerShip).Save();
		/*
		var wingmateClass = w.types.Lookup<ShipClass>("ship_beowulf");
		var wingmate = new AIShip(new BaseShip(w, wingmateClass, playerSovereign, playerStart), new EscortOrder(playerShip, new XY(-5, 0)));
		w.AddEntity(wingmate);
		w.AddEffect(new Heading(wingmate));
		*/
		var playerMain = new Mainframe(Width, Height, profile, playerShip);
		playerShip.onDestroyed += playerMain;
		Go(playerMain);
	}
#endif
	void AddStarterKit(PlayerShip playerShip) {
		var tc = playerShip.world.types;
		playerShip.cargo.UnionWith(Group<Item>.From(tc, SGenerator.ParseFrom(tc, SGenerator.ItemFrom),
		  @"<Items>
				<Item codename=""item_orator_charm_silence""       count=""1""/>
			</Items>"));
	}





	class ConfigPane {
		ShipControls settings;
		Control? currentSet;
		Dictionary<Control, SfLink> buttons;

		Sf on;

		public bool visible;
		public ConfigPane (Sf on, ShipControls settings) {
			this.on = on;
			this.settings = settings;

			currentSet = null;
			buttons = new Dictionary<Control, SfLink>();

			Init();
		}
		public void Reset () {
			Init();
		}
		public void Init () {
			int x = 2;
			int y = 0;

			var controls = settings.controls;
			foreach(var control in controls.Keys) {
				var c = control;
				string label = GetLabel(c);
				SfLink b = null;
				b = new SfLink(on, (x, y++), label, () => {

					if(currentSet.HasValue) {
						ResetLabel(currentSet.Value);
					}

					currentSet = c;
					b.text = $"{c,-16} {"[Press Key]",-12}";
				});

				buttons[control] = b;
			}
		}
		string GetLabel (Control control) => $"{control,-16} {settings.controls[control],-12}";
		public void ResetLabel (Control k) => buttons[k].text = GetLabel(k);
		public void HandleKey (KB info) {
			if(info.IsPress(KC.Escape)) {
				if(currentSet.HasValue) {
					ResetLabel(currentSet.Value);
					currentSet = null;
				} else {
				}
			} else if(info.Press.Any()) {
				if(currentSet.HasValue) {
					settings.controls[currentSet.Value] = (KC)info.Press.First();
					ResetLabel(currentSet.Value);
					currentSet = null;
				}
			}
		}
		public void HandleMouse(HandState state) {
			
		}
		public void Render(TimeSpan delta) {
			if(visible) {
				foreach(var (_, button) in buttons) {
					button.Render(delta);
				}
			}
		}
	}

}
