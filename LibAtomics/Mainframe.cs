global using static System.Math;
global using TileImg = System.Collections.Generic.Dictionary<(int X, int Y), (uint F, uint B, int G)>;
using Common;
using LibGamer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static LibGamer.Sf;
namespace LibAtomics;

public class TitleScreen : IScene {


	class Particle {
		public double x, y;
		public double speed;
		public bool active = true;
	}

	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	Sf sf;
	List<SfControl> controls = [];

	Assets assets;
	public TitleScreen (int Width, int Height, Assets assets) {
		this.assets = assets;
		sf = new Sf(Width / 2, Height / 2, assets.RF_8x8) { scale = 2 };



		var str = "New Adventure";
		controls.Add(new SfLink(sf, (sf.GridWidth/2- str.Length/2, sf.GridHeight/2-4), str, () => {
			Go(new Mainframe(Width, Height, assets));
		}));


		foreach(var pos in sf.Positions) {
			glow[pos] = 0;
		}
	}

	void IScene.HandleMouse(LibGamer.HandState mouse) {
		foreach(var c in controls) c.HandleMouse(mouse);

		var pos = mouse.pos;
		pos = sf.GetMousePos(pos);

		if(pos.x >= 0 && pos.x < sf.GridWidth && pos.y >= 0 && pos.y < sf.GridHeight) {
			glow[pos] = 255;
		}

		return;
	}

	List<Particle> particles = [];
	void IScene.Update(System.TimeSpan delta) {

		var r = new Rand();
		for(var i = particles.Count; i < 50; i++) {
			var x = r.NextBool() ? 0 : sf.GridWidth - 1;
			particles.Add(new() {
				x = x,
				y = r.NextInteger(sf.GridHeight),
				speed = (x > 0 ? -1 : 1) * r.NextInteger(5, 10)
			});
		}

		ConcurrentDictionary<(int x, int y), HashSet<Particle>> d = [];
		particles.ForEach(p => {
			p.x += p.speed * delta.TotalSeconds;

			if(p.x > sf.GridWidth - 1 || p.x < 0) {
				p.active = false;
				return;
			}

			glow[((int)p.x, (int)p.y)] = 255;




			var others = d.GetOrAdd(((int)p.x, (int)p.y), []);
			others.Add(p);
			if(others.Count > 1) {
				foreach(var o in others)
					o.active = false;
				foreach(var y in sf.GridHeight) {
					glow[((int)p.x, y)] = 255;
				}
			}
		});
		particles.RemoveAll(p => !p.active);

		time += delta.TotalSeconds;

		foreach(var(p, t) in assets.hive.Sprite) {
			assets.hive.Sprite[p] = t with { Foreground = ABGR.SetA(t.Foreground, (byte)Main.Lerp(IEEERemainder(time, 1), 0, 1, 255, 51, 1)) };
		}
    }
	double time;
	Dictionary<(int x, int y), double> glow = [];
	void IScene.Render(System.TimeSpan delta) {
		var r = new Random();
		foreach(var pos in sf.Positions) {
			var dest = r.Next(0, 102);
			var g = glow[pos];
			g += (dest - g) / 15;
			glow[pos] = g;
			sf.Tile[pos] = new Tile(ABGR.SetA(ABGR.DeepPink, (byte)g), ABGR.Black, '=');
		}
		/*
		foreach(var p in particles) {
			sf.Front[(int)p.x, (int)p.y] = ABGR.DeepPink;
		}
		*/
		assets.title.Render(sf, (sf.GridWidth/2 - assets.title.Width/2, 8));
		assets.hive.Render(sf, (sf.GridWidth / 2- assets.hive.Width / 2, 24));
		foreach(var c in controls) c.Render(delta);		
		Draw?.Invoke(sf);
	}
}
public class Mainframe : IScene {
	public static readonly uint PINK = ABGR.RGB(255, 0, 128);
	public static readonly uint BACK = ABGR.SetA(ABGR.Black, 128);
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Sf sf_main;
	int Width => sf_main.GridWidth;
	int Height=> sf_main.GridHeight;
	public XYI center => (sf_main.GridWidth / 2, sf_main.GridHeight / 2);
	public Sf sf_ui;
	public Sf sf_portrait;
	Rand r = new();
	World level;
	Player player;
	private IScene _dialog;
	public IScene dialog {
		get => _dialog;
		set {
			void Go(IScene dest)=>
				dialog = dest;
			if(_dialog is { } prev) {
				prev.Draw -= Draw;
				prev.Go -= Go;
				prev.PlaySound -= PlaySound;
			}
			_dialog = value;
			if(value is { } v) {
				v.Draw += Draw;
				v.Go += Go;
				v.PlaySound += PlaySound;
			}
		}
	}
	public Mainframe (int Width, int Height, Assets assets) {
		sf_main = new(Width, Height, assets.IBMCGA_8x8);
		sf_ui = new(Width, Height, assets.IBMCGA_6x8);
		sf_portrait = new(18, 18, assets.RF_8x8);
		noise = new byte[Width, Height];
		level = new World();
		foreach(var x in 30) {
			foreach(var y in 30) {
				level.entities.Add(new Floor((x, y), r));
			}
		}
		level.entities.Add(player = new Player(level, (0, 0)));

		player.body.parts.Last().hp = 50;

		player.items.Add(new Item(new ItemType() { name = "Marrow Ring", tile = new(ABGR.White, ABGR.Black, 'o') }));
		player.items.Add(new Item(new ItemType() { name = "Rattle Blade", tile = new(ABGR.LightGreen, ABGR.Black, 'l') }));
		player.items.Add(new Item(new ItemType() { name = "Miasma Grenade", tile = new(ABGR.Tan, ABGR.Black, 'g') }));
		player.items.Add(new Item(new ItemType() { name = "Machine Gun", tile = new(ABGR.LightGray, ABGR.Black, 'm') }));
		level.entities.Add(new Roach(level, (10, 10)));
		level.TryUpdatePresent();
	}
	bool ready => player.ready && (subticks?.done ?? true);
	double delay = 0;
	World.Subticks subticks = null;
	void IScene.Update(TimeSpan delta) {
		marker.time += delta.TotalSeconds;
		level.UpdateReal(delta);

		foreach(var y in Height) {
			foreach(var x in Width) {
				noise[x, y] = (byte)(r.NextFloat() * 5 + 51);
			}
		}

		if(dialog is { } d) {
			d.Update(delta);
			return;
		}
		delay -= delta.TotalSeconds;
		if(delay > 0) {
			return;
		}
		if(subticks is { done:false }) {
			subticks.Update();
			if(!subticks.done) {
				//delay = 0.025;
				return;
			}
		}
		if(!player.ready) {
			subticks = level.UpdateStep();
			if(subticks.done) {
				subticks = null;
			}
		}
		if(player.ready) {
			delay = 0;
		}
	}

	byte[,] noise;
	void IScene.Render(TimeSpan delta) {
		sf_main.Clear();
		player.UpdateVision();
		var pov = player.pos;
		foreach(var y in Height) {
			foreach(var x in Width) {
				var loc = pov + (x, y) - center;
				if(!player.visibleTiles.TryGetValue(loc, out var t))
					t = new Tile(ABGR.SetA(ABGR.White, noise[x,y]), ABGR.Black, 'p' + 64);
				sf_main.Print(x, Height - y - 1, t);
			}
		}
		sf_ui.Clear();
		{
			var (x, y) = (24, 0);
			DrawRect(sf_ui, x, y, 32, 3, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;
			sf_ui.Print(x,y, $"Tick: {player.tick}");
		}
		{
			var (x, y) = (0, 18);
			DrawRect(sf_ui, x, y, 32, 15, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;

			var a = (byte)Main.Lerp(IEEERemainder(player.time, 1), 0, 0.5, 255, 0, 1);
			foreach(var part in player.body.parts) {
				sf_ui.Print(x, y++, $"{part.name,-12} {part.hp, 5:00.0}", ABGR.Blend(ABGR.White, ABGR.SetA(PINK, a)), BACK);
			}
		}
		{
			var (x, y) = (0, 33);
			DrawRect(sf_ui, x, y, 32, 26, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;
			foreach(var (i,item) in player.items.Index()) {
				sf_ui.Print(x, y++, [item.type.tile, Tile.empty, ..Tile.Arr($"{item.type.name,-27}{(char)('a' + i)}")]);
			}
		}

#if false
		{
			var x = 0;
			var y = Height - 26 - 5;
			Sf.DrawRect(sf_ui, x, y, 32, 5, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;
			sf_ui.Print(x, y, "Goal: Terminate Enemy", ABGR.White, ABGR.Black);
			y++;
			sf_ui.Print(x, y, "Goal: Calibrate Aim", ABGR.White, ABGR.Black);
		}
#endif
		{
			var x = 0; var y = Height - 31;
			var _m = player.messages;
			DrawRect(sf_ui, x, y, 32, 31, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;
			foreach(var m in _m[Max(_m.Count - 29, 0)..].Reverse<Player.Message>()) {
				IEnumerable<Tile> str = m.str.Concat([Tile.empty, .. (m.once ? [] : Tile.Arr($"x{m.repeats}"))]);
				if(player.tick > m.tick) {
					var ft = player.time - m.fadeTime;
					str = from tile in str select tile with {
						Foreground = ABGR.SetA(tile.Foreground, (byte)Main.Lerp(ft, 0, 0.4, 255, 128, 1)) };
				}
				var bt = player.time - m.time;
				if(bt < 0.4) {
					str = from tile in str select tile with {
						Background = ABGR.Blend(sf_ui.Tile[x,y].Background, ABGR.SetA(PINK, (byte)Main.Lerp(bt, 0, 0.4, 255, 0, 1))) };
				} 
				sf_ui.Print(x, y++, [..str]);
			}
		}
		if(level.entities.Contains(marker)){
			int x = 32, y = 33;
			DrawRect(sf_ui, x, y, 32, 26, new() {
				f = PINK,
				b = BACK
			});
			x++;
			y++;
			foreach(var (i, e) in level.entityMap.GetValueOrDefault(marker.pos, []).Except([marker]).Index()) {
				sf_ui.Print(x, y, e.tile);
				sf_ui.Print(x + 2, y, $"{e switch {
					Floor => "Floor",
					Player => "Player",
					Roach => "Roach",
					Splat => "Bullet",
					_ => "UNKNOWN"
				}, -12}{(char)('a'+i)}");
				y++;
			}
		}
		sf_portrait.Clear();
		DrawRect(sf_portrait, 0, 0, 18, 18, new() {
			f = PINK,
			b = BACK
		});

		foreach(var pair in playerImg) {
			sf_portrait.Tile[(pair.Key.X + 1, pair.Key.Y + 1)] = pair.Value;
		}
		Draw?.Invoke(sf_main);
		Draw?.Invoke(sf_ui);
		Draw?.Invoke(sf_portrait);
		if(dialog is { })
			dialog.Render(delta);
	}
	Dictionary<(int X, int Y), (uint F, uint B, int G)> playerImg = ImageLoader.LoadTile("Assets/sprite/giant_cockroach_robot.dat");
	void IScene.HandleKey(KB kb) {

		if(dialog is { }d) {
			d.HandleKey(kb);
			return;
		}
		if(!ready) {

		} else {
			var p = kb.IsPress;
			if(p(KC.Up)) {
				player.Walk((0, 1));
			} else if(p(KC.Right)) {
				player.Walk((1, 0));
			} else if(p(KC.Down)) {
				player.Walk((0, -1));
			} else if(p(KC.Left)) {
				player.Walk((-1, 0));
			} else if(p(KC.S)) {
				dialog = new AimDialog(player);
			}
			if(kb.IsDown(KC.OemPeriod)) {
				player.busy = true;
			}

		}
	}
	Marker marker = new();
	void IScene.HandleMouse(HandState mouse) {
		if(!mouse.on) {
			level.RemoveEntity(marker);
			return;
		}
		var pos = (XYI)mouse.pos / sf_main.font.GlyphSize - center;
		pos = player.pos + (pos.x, -pos.y) + (0, -1);
		if(!level.entities.Contains(marker))
			level.AddEntity(marker);
		marker._pos = (pos.x, pos.y);
#if true
		if(mouse.leftDown && player.shoot?.done != false) {
			player.shoot = new Shoot() { target = (XY)pos };
			player.shoot.Init(player);
		}
#endif
		return;
	}
}