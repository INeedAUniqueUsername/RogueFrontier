using Common;
using LibGamer;
using RogueFrontier;
using System.Collections.Concurrent;
namespace LibAtomics;
public class PlayerMain : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Sf sf_main;
	int Width => sf_main.Width;
	int Height=> sf_main.Height;
	public XYI center => (sf_main.Width / 2, sf_main.Height / 2);
	public Sf sf_ui;
	Rand r = new Rand();
	Level level;
	Player player;
	public PlayerMain (int Width, int Height) {
		sf_main = new Sf(Width, Height, Fonts.FONT_8x8);
		sf_ui = new Sf(Width, Height, Fonts.FONT_6x8);
		level = new Level();
		foreach(var x in 0..30) {
			foreach(var y in 0..30) {
				level.entities.Add(new Floor((x, y), r));
			}
		}
		level.entities.Add(player = new Player(level, (0, 0)));
	}
	void IScene.Update(System.TimeSpan delta) {
		marker.time += delta.TotalSeconds;
	}
	void IScene.Render(System.TimeSpan delta) {
		sf_main.Clear();
		player.UpdateVision();
		var pov = player.pos;
		foreach(var x in 0..Width) {
			foreach(var y in 0..Height) {
				var loc = pov + (x, y) - center;
				var t = player.vision.GetValueOrDefault(loc, new Tile(ABGR.SetA(ABGR.White, (byte)(r.NextFloat() * 5 + 51)), ABGR.Black, 'p' + 64));
				sf_main.Print(x, Height - y - 1, t);
			}
		}
		sf_ui.Clear();
		if(level.entities.Contains(marker)){
			int x = 0, y = 0;
			foreach(var e in level.entityMap.GetValueOrDefault(marker.pos, []).Except([marker])) {
				sf_ui.Print(x, y, e.tile);
				sf_ui.Print(x + 2, y, e switch {
					Floor => "Floor",
					Player => "Player"
				});
				y++;
			}
		}
		Draw?.Invoke(sf_main);
		Draw?.Invoke(sf_ui);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
		if(player.busy) {

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

		level.AddEntity(marker);
		marker.pos = pos;
		return;
	}
}
public interface IEntity {
	XYI pos { get; }
	Tile tile { get; }
	Action Removed { get; set; }
	void UpdateTick () { }
}
public class Marker : IEntity {
	public XYI pos { get; set; } = (0, 0);
	public Tile tile => 
		new Tile(ABGR.Black, ABGR.Yellow, GetGlyph());
	public char GetGlyph() {
		var re = Math.IEEERemainder(time, 0.5);
		return re < 0 ? '?' : ' ';
	}
	public Action Removed { get; set; }
	public double time = 0;
}
public class Wall :IEntity {
	public XYI pos { get; set; } = (0, 0);
	public Tile tile { get; set; } = new Tile(ABGR.White, ABGR.Black, '#');
	public Action Removed { get; set; }
}
public class Floor : IEntity {
	public XYI pos { get; set; }
	public Tile tile { get; set; }
	public Action Removed { get; set; }
	public Floor(XYI pos, Rand r) {
		this.pos = pos;
		tile = new Tile(ABGR.RGBA(25, 25, 36, (byte)(204 + (r.NextFloat()*51))), ABGR.Black, (char)254);
	}
	void IEntity.UpdateTick () {

	}
}
public class Player : IEntity {
	Level level;
	public Tile tile => new Tile(ABGR.Magenta, ABGR.Black, '@');
	public XYI pos { get; set; }
	public Action Removed { get; set; }
	public bool busy;
	HashSet<(int x, int y)> visible = [];
	public Player (Level level, XYI pos) {
		this.level = level;
		this.pos = pos;
	}
	public void Walk (XYI dir) {

		var dest = pos + dir;

		var ent = level.entityMap.GetValueOrDefault(dest, []);
		if(!ent.OfType<Floor>().Any()) {
			return;
		}
		if(ent.OfType<Wall>().Any()) {
			return;
		}
		pos = dest;
	}
	void IEntity.UpdateTick () {
		visible.Clear();
	}
	public ConcurrentDictionary<(int, int), Tile> vision = [];
	public void UpdateVision () {
		vision.Clear();
		foreach(var e in level.entities) {
			vision[e.pos] = e.tile;
		}
	}
}
public class Level {
	public HashSet<IEntity> entitiesAdd = [];
	public HashSet<IEntity> entitiesRemove = [];
	public HashSet<IEntity> entities = [];
	public ConcurrentDictionary<(int, int), HashSet<IEntity>> entityMap {
		get {
			ConcurrentDictionary<(int, int), HashSet<IEntity>> result = [];
			foreach(var e in entities) {
				result.GetOrAdd(e.pos, []).Add(e);
			}
			return result;
		}
	}
	bool busy = false;
	public void AddEntity(IEntity e) {
		e.Removed += () => RemoveEntity(e);
		if(busy)
			entitiesAdd.Add(e);
		else
			entities.Add(e);
	}
	public void RemoveEntity(IEntity e) {
		if(busy)
			entitiesRemove.Add(e);
		else
			entities.Remove(e);
	}
	public void Update() {
		busy = true;
		foreach(var e in entities) {
			e.UpdateTick();
		}
		busy = false;
		entities.UnionWith(entitiesAdd);
		entities.ExceptWith(entitiesRemove);
		entitiesAdd.Clear();
		entitiesRemove.Clear();
	}
}