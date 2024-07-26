global using static System.Math;
global using TileImg = System.Collections.Generic.Dictionary<(int X, int Y), (uint F, uint B, int G)>;
using Common;
using LibGamer;
using RogueFrontier;
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


	SfLink start;

	public TitleScreen (int Width, int Height, Assets assets) {
		this.assets = assets;
		sf = new Sf(Width / 2, Height / 2, assets.RF_8x8) { scale = 2 };
		var str = "New Adventure";
		start = new SfLink(sf, (sf.GridWidth / 2 - str.Length / 2, sf.GridHeight / 2 - 4), str, () => {
			Go?.Invoke(new Mainframe(Width, Height, assets));
		});
		controls.Add(start);

		glow = new double[Width, Height];
		foreach(var pos in sf.Positions) {
			glow[pos.x, pos.y] = 0;
		}
	}
	void IScene.HandleMouse(LibGamer.HandState mouse) {
		foreach(var c in controls) c.HandleMouse(mouse);
		var pos = sf.GetMousePos(mouse.pos);
		if(pos.x >= 0 && pos.x < sf.GridWidth && pos.y >= 0 && pos.y < sf.GridHeight) {
			glow[pos.x, pos.y] = 255;
		}
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
		var d = new ConcurrentDictionary<(int x, int y), HashSet<Particle>>();
		particles.ForEach(p => {
			p.x += p.speed * delta.TotalSeconds;
			if(p.x > sf.GridWidth - 1 || p.x < 0) {
				p.active = false;
				return;
			}
			glow[(int)p.x, (int)p.y] = 255;
			var others = d.GetOrAdd(((int)p.x, (int)p.y), []);
			others.Add(p);
			if(others.Count > 1) {
				foreach(var o in others)
					o.active = false;
				foreach(var y in sf.GridHeight) {
					glow[(int)p.x, y] = 255;
				}
			}
		});
		particles.RemoveAll(p => !p.active);
		time += delta.TotalSeconds;

		hiveAlpha = (byte)Main.Lerp(IEEERemainder(time, 1), 0, 1, 255, 51, 1);
		foreach(var(p, t) in assets.hive.Sprite) {
			assets.hive.Sprite[p] = t with { Foreground = ABGR.SetA(t.Foreground, hiveAlpha) };
		}
    }

	byte hiveAlpha = 0;
	double time;
	double[,] glow = new double[0,0];

	double factor = 1;
	void IScene.Render(System.TimeSpan delta) {
		var r = new Random();
		var factorDest = 1;
		if(start.mouse.nowOn) {
			factorDest = 0;
		}

		factor += (factorDest - factor) * delta.TotalSeconds * 4;
		foreach(var pos in sf.Positions) {
			ref var g = ref glow[pos.x, pos.y];
			var dest = r.Next(0, 102);
			g += (dest - g) * delta.TotalSeconds * 4;
			sf.Tile[pos] = new(
				ABGR.SetA(ABGR.DeepPink, (byte)(g * factor)),
				ABGR.Blend(ABGR.Black, ABGR.SetA(ABGR.DeepPink, (byte)((1 - factor) * hiveAlpha/10))),
				'=');
		}

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

	SfPane pane_sprite;
	SfPane pane_look;
	SfPane pane_cargo;
	SfPane pane_body;
	SfPane pane_log;

	Assets assets;
	public Mainframe (int Width, int Height, Assets assets) {
		this.assets = assets;
		sf_main = new(Width, Height, assets.IBMCGA_8x8);
		sf_ui = new(Width, Height, assets.IBMCGA_6x8);
		sf_portrait = new(20, 20, assets.RF_8x8);

		pane_body = new SfPane(sf_ui, new Rect(27, 0, 32, 20), new() {
			f = ABGR.DeepPink,
			b = ABGR.SetA(ABGR.Black, 128),
		});

		pane_cargo = new SfPane(sf_ui, new Rect(0, 20, 26, 26), new() {
			f = ABGR.DeepPink,
			b = ABGR.SetA(ABGR.Black, 128),
		});
		pane_look = new SfPane(sf_ui, new Rect(0, 46, 26, 20), new() {
			f = ABGR.DeepPink,
			b = ABGR.SetA(ABGR.Black, 128)
		});
		pane_log = new SfPane(sf_ui, new Rect(0, Height - 24, 64, 24), new() {
			f = ABGR.DeepPink,
			b = ABGR.SetA(ABGR.Black, 128),
		});


		noise = new byte[Width, Height];
		level = new World();
		foreach(var (y,x) in 30.Product(30)) {
			level.entities.Add(new Floor((x, y), r));
		}
		level.entities.Add(player = new Player(level, (0, 0)));

		player.body.parts.Last().hp = 50;

		player.items.Add(new Item(new ItemType() { name = "Marrow Ring", tile = new(ABGR.White, ABGR.Black, 'o') }));
		player.items.Add(new Item(new ItemType() { name = "Rattle Blade", tile = new(ABGR.LightGreen, ABGR.Black, 'l') }));
		player.items.Add(new Item(new ItemType() { name = "Miasma Grenade", tile = new(ABGR.Tan, ABGR.Black, 'g') }));
		player.items.Add(new Item(new ItemType() { name = "Machine Gun", tile = new(ABGR.LightGray, ABGR.Black, 'm') }));
		level.entities.Add(new Tankroach(level, (10, 10)));
		level.TryUpdatePresent();


	}
	bool ready => player.ready && (subticks?.done ?? true);
	double delay = 0;
	World.Subticks subticks = null;
	void IScene.Update(TimeSpan delta) {
		marker.time += delta.TotalSeconds;
		level.UpdateReal(delta);
		foreach(var(y,x) in Height.Product(Width)) {
			noise[x, y] = (byte)(r.NextFloat() * 5 + 51);
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

		pane_body.Render(delta);
		foreach(var (i, part) in player.body.parts.Index()) {
			pane_body.Print(i, Tile.Arr($"{part.name,-12} {part.hp,5:00.0}", ABGR.Blend(ABGR.White, ABGR.SetA(PINK, 0)), BACK));
		}
		pane_cargo.Render(delta);
		foreach(var (i, item) in player.items.Index()) {
			pane_cargo.Print(i, [item.type.tile, new Tile(0, ABGR.Black, 0), .. Tile.Arr($"{item.type.name}")]);
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
		pane_log.Render(delta);
		foreach(var (i, m) in player.messages.TakeLast(Math.Min(pane_log.rect.height - 2, player.messages.Count)).Reverse().Index()) {
			var str = m.str.Concat([new Tile(0, ABGR.Black, 0), .. (m.once ? [] : Tile.Arr($"x{m.repeats}"))]);
			if(player.tick > m.tick) {
				var ft = player.time - m.fadeTime;
				str = str.Select(tile => tile with {
					Foreground = ABGR.Blend(ABGR.Black, ABGR.SetA(tile.Foreground, (byte)Main.Lerp(ft, 0, 0.4, 255, 128, 1)))
				});
			}
			if(player.time - m.time is < 0.4 and { } bt) {
				str = str.Select(tile => tile with {
					Background = ABGR.SetA(ABGR.DeepPink, (byte)Main.Lerp(bt, 0, 0.4, 255, 0, 1))
				});
			}
			pane_log.Print(i, [.. str]);
		}

		pane_look.Render(delta);
		if(level.entities.Contains(marker)){
			foreach(var (i, e) in level.entityMap.GetValueOrDefault(marker.pos, []).Except([marker]).Index()) {
				pane_look.Print(i, [e.tile, new Tile(0, ABGR.Black, 0), ..Tile.Arr(e switch {
					Floor => "Floor",
					Player => "Player",
					Tankroach => "Tankroach",
					Splat => "Bullet",
					_ => "UNKNOWN"
				}, ABGR.White, ABGR.Black)]);
			}
		}
		sf_portrait.Clear();
		DrawBorder(sf_portrait, new() { f = PINK, b = BACK, width = Line.Double });
		assets.giantCockroachRobot.Render(sf_portrait, (2, 2));
		Draw?.Invoke(sf_main);
		Draw?.Invoke(sf_ui);
		Draw?.Invoke(sf_portrait);
		if(dialog is { })
			dialog.Render(delta);
	}
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
				dialog = new ShootDialog(Width, Height, assets, player);
			} else if(p(KC.W)) {
				//dialog = new WearDialog(player);
			} else if(kb.IsDown(KC.OemPeriod)) {
				player.busy = true;
			}
		}
	}
	Marker marker = new();
	Hand hand = new();
	void IScene.HandleMouse(HandState mouse) {
		hand.Update(mouse);
		bool look = hand.nowOn;

		pane_cargo.HandleMouse(mouse.OnRect(pane_cargo.screenRect));
		if(pane_cargo.hand.nowOn) {
			look = false;


			var pos = pane_cargo.MouseCellPos;
			pos = (pos.x - 1, pos.y - 1);
			if(pos is (>-1, >-1)) {
				var i = pos.y;
				if(i < player.items.Count) {
					var item = player.items.ElementAt(i);

				}
			}
		}
		if(look) {
			var pos = (XYI)mouse.pos / sf_main.font.GlyphSize - center;
			pos = player.pos + (pos.x, -pos.y) + (0, -1);
			if(!level.entities.Contains(marker))
				level.AddEntity(marker);
			marker._pos = (pos.x, pos.y);
		} else {
			level.RemoveEntity(marker);
		}
		return;
	}
}

record SfPane(Sf on, Rect rect, RectOptions border) {
	public Hand hand = new();
	public void HandleMouse(HandState state) {
		hand.Update(state);
	}
	public (int x, int y) MouseCellPos => (hand.nowPos.x / on.GlyphWidth, hand.nowPos.y / on.GlyphHeight);
	public Rect screenRect => on.SubRect(rect);

	public void Render(TimeSpan delta) {
		DrawRect(on, rect.x, rect.y, rect.width, rect.height, border);
	}
	public void Print (int line, Tile[] str) => on.Print(rect.x + 1, rect.y + 1 + line, str);
	public (int x, int y) firstLine => (rect.x + 1, rect.y + 1);
}