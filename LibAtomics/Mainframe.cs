using Common;
using LibGamer;
using RogueFrontier;
namespace LibAtomics;
public class Mainframe : IScene {

	public static readonly uint tint = ABGR.RGB(255, 0, 128);

	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Sf sf_main;
	int Width => sf_main.Width;
	int Height=> sf_main.Height;
	public XYI center => (sf_main.Width / 2, sf_main.Height / 2);
	public Sf sf_ui;
	Rand r = new Rand();
	World level;
	Player player;
	public Mainframe (int Width, int Height) {
		sf_main = new Sf(Width, Height, Fonts.FONT_8x8);
		sf_ui = new Sf(Width, Height, Fonts.FONT_6x8);
		level = new World();
		foreach(var x in 0..30) {
			foreach(var y in 0..30) {
				level.entities.Add(new Floor((x, y), r));
			}
		}
		level.entities.Add(player = new Player(level, (0, 0)));
		level.entities.Add(new Roach(level, (10, 10)));

		level.PlaceEntity();
	}

	bool ready => player.ready && (subticks?.done ?? true);
	double delay = 0;
	World.Subticks subticks = null;
	void IScene.Update(System.TimeSpan delta) {
		marker.time += delta.TotalSeconds;
		level.UpdateReal(delta);
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
	void IScene.Render(System.TimeSpan delta) {
		sf_main.Clear();
		player.UpdateVision();
		var pov = player.pos;
		foreach(var x in 0..Width) {
			foreach(var y in 0..Height) {
				var loc = pov + (x, y) - center;
				var t = player.visibleTiles.GetValueOrDefault(loc, new Tile(ABGR.SetA(ABGR.White, (byte)(r.NextFloat() * 5 + 51)), ABGR.Black, 'p' + 64));
				sf_main.Print(x, Height - y - 1, t);
			}
		}
		sf_ui.Clear();


		{
			var x = 1;
			var y = 1;
			sf_ui.DrawRect(x, y, 32, 3, new() {
				f = tint,
				b = ABGR.SetA(ABGR.Black, 128)
			});
			x++;
			y++;
			sf_ui.Print(x,y,$"Tick: {player.tick}");
		}
		{
			var x = 1;
			var y = Height - 26 - 5;
			sf_ui.DrawRect(x, y, 32, 5, new() {
				f = tint,
				b = ABGR.SetA(ABGR.Black, 128)
			});
			x++;
			y++;
			sf_ui.Print(x, y, "Goal_Terminate_Enemy", tint, ABGR.Black);
			y++;
			sf_ui.Print(x, y, "Goal_Calibrate_Aim", tint, ABGR.Black);
		}
		{
			var x = 1; var y = Height - 26;
			var _m = player.messages;
			sf_ui.DrawRect(x, y, 32, 26, new() {
				f = tint,
				b = ABGR.SetA(ABGR.Black, 128)
			});
			x++;
			y++;
			foreach(var m in _m[Math.Max(0, _m.Count - 24)..].Reverse<Player.Message>()) {
				IEnumerable<Tile> msg = m.msg;
				var dt = player.time - m.time;
				if(player.tick - m.tick > 0) {
					msg = from t in msg select t with {
						Foreground = ABGR.SetA(t.Foreground, (byte)Main.Lerp(dt, 0, 0.4, 255, 128, 1)) };
				}
				if(dt < 0.4) {
					msg = from t in msg select t with {
						Background = ABGR.Blend(sf_ui.Tile[x,y].Background, ABGR.SetA(tint, (byte)Common.Main.Lerp(dt, 0, 0.4, 255, 0, 1))) };
				} 
				sf_ui.Print(x, y++, [..msg]);
			}
		}
		if(level.entities.Contains(marker)){
			int x = Width/2 - 16, y = 1;
			sf_ui.DrawRect(x, y, 32, 5, new() {
				f = tint,
				b = ABGR.SetA(ABGR.Black, 128)
			});
			x++;
			y++;
			foreach(var e in level.entityMap.GetValueOrDefault(marker.pos, []).Except([marker])) {
				sf_ui.Print(x, y, e.tile);
				sf_ui.Print(x + 2, y, e switch {
					Floor => "Floor",
					Player => "Player",
					Roach => "Roach",
					_ => ""
				});
				y++;
			}
		}
		Draw?.Invoke(sf_main);
		Draw?.Invoke(sf_ui);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
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
			}
		}
	}
	Marker marker = new Marker();
	void IScene.HandleMouse(LibGamer.HandState mouse) {
		if(!mouse.on) {
			level.RemoveEntity(marker);
			return;
		}
		var pos = (XYI)mouse.pos / sf_main.font.GlyphSize - center;
		pos = player.pos + (pos.x, -pos.y) + (0, -1);

		if(!level.entities.Contains(marker))
			level.AddEntity(marker);
		marker._pos = (pos.x, pos.y);

		if(mouse.leftDown && player.shoot?.done != false) {
			player.shoot = new Shoot() { target = (XY)pos };
			player.shoot.Init(player);
		}

		return;
	}
}