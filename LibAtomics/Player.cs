using Common;
using LibGamer;
using System.Collections.Concurrent;
namespace LibAtomics;
public class Player : IActor, IEntity {
	public World level;
	public Tile tile => new Tile(Mainframe.tint, ABGR.Black, '@');
	public XYI pos { get; set; }
	public Action Removed { get; set; }
	public bool ready => !busy && delay < 1;
	public bool busy;
	public double time;
	public int tick;

	public int delay = 0;

	public record Message (Tile[] msg, double time, int tick) {
		public string text => new string([.. from t in msg select (char)t.Glyph]);
	};
	public List<Message> messages = [];
	public void AddMessage(Message m) {
		if(messages.LastOrDefault()?.text == m.text) {
			//messages.RemoveAt(messages.Count - 1);
		}
		messages.Add(m);
	}
	public void Tell(string msg) {
		AddMessage(new Message(Tile.Arr(msg), time, tick));
	}
	public Player (World level, XYI pos) {
		this.level = level;
		this.pos = pos;
	}
	public void Walk (XYI dir) {
		var dest = pos + dir;
		var ent = level.entityMap.GetValueOrDefault(dest, []);
		if(!ent.OfType<Floor>().Any()) {
			AddMessage(new(Tile.Arr("Err_Floor_Not_Found", ABGR.White), time, tick));
			return;
		}
		if(ent.OfType<Wall>().Any()) {
			AddMessage(new Message(Tile.Arr("Err_Access_Blocked", ABGR.White), time, tick));
			return;
		}
		pos = dest;
		busy = true;
		delay = 1;
	}

	public Shoot shoot;
	Action[] IActor.UpdateTick () {
		tick++;
		UpdateVision();
		delay--;
		busy = false;

		HashSet<Action[]> r = [];

		IEnumerable<Action> Zip() {
			HashSet<Action[]> remaining = r;
			for(int i = 0; remaining.Count > 0; i++) {
				remaining.RemoveWhere(l => i >= l.Length);
				yield return GetRow();
				Action GetRow() {
					Action r = () => { };
					foreach(var l in remaining) {
						r += l[i];
					}
					return r;
				}
			}
		}
		if(shoot != null) {
			r.Add(shoot.Act(this));
		}
		return [..Zip()];
		return [

			() => AddMessage(new Message(Tile.Arr("subtick 1"), time, tick)),
			() => AddMessage(new Message(Tile.Arr("subtick 2"), time, tick)),
			() => AddMessage(new Message(Tile.Arr("subtick 3"), time, tick)),
			() => AddMessage(new Message(Tile.Arr("subtick 4"), time, tick)),
			() => AddMessage(new Message(Tile.Arr("subtick 5"), time, tick)),
			];
	}
	void IActor.UpdateReal(System.TimeSpan delta) {
		time += delta.TotalSeconds;
	}
	public ConcurrentDictionary<(int, int), Tile> _visibleTiles = null;
	public ConcurrentDictionary<(int, int), Tile> visibleTiles => _visibleTiles ??=
		new(visibleAt.Select(pair => (pair.Key, pair.Value.Select(v => v.tile).Except([null]).LastOrDefault())).Where(pair => pair.Item2 != null).ToDictionary());
	public ConcurrentDictionary<(int, int), HashSet<IEntity>> visibleAt = [];
	HashSet<IEntity> visible = [];

	double lastUpdate = -1;
	public void UpdateVision () {
		/*
		if(lastUpdate == level.lastUpdate) {
			return;
		}
		*/
		lastUpdate = level.lastUpdate;
		_visibleTiles = null;
		visibleAt.Clear();
		visible.Clear();
		if(true) {
			_visibleTiles = [];
			foreach(var e in level.entities) {
				_visibleTiles[e.pos] = e.tile;
			}
		} else {
			foreach(var e in level.entities) {
				if(visible.Contains(e)) {
					continue;
				}
				var dest = e.pos;
				bool add = new Lazy<bool>(() => {
					if(level.entityMap.GetValueOrDefault(dest, []).Except([e]).OfType<Wall>().Any()) {
						return false;
					}
					if(dest == pos) {
						return true;
					}
					
					IEnumerable<XYI> GetPath () {
						var disp = dest - pos;
						var dir = disp.Normalize(out var dist);
						for(int i = 1; i < dist; i++) {
							yield return pos + dir;
						}
					}
					foreach(var p in GetPath()) {

						var other = level.entityMap.GetValueOrDefault(p, []);
						if(!other.Any()) {
							return false;
						}
						if(level.entityMap[p].OfType<Floor>().Any()) {
							continue;
						}
					}
					return true;
				}).Value;
				if(add) {
					visible.Add(e);
					visibleAt.GetOrAdd(dest, []).Add(e);
					//Additional effects
				}
			}
		}
	}
}
public class Shoot {
	public XY target;
	Reticle reticle;
	bool locked = false;
	public bool done = false;
	public void Init(Player p) {
		reticle = new Reticle() { _pos = (XY)p.pos, visible = true };
		p.level.AddEntity(reticle);
		p.Tell("Attack_Select_Target");
	}
	public Action[] Act(Player p) {
		if(done) return [];
		if(!locked) {
			p.Tell("Attack_Calibrate_Aim");
			var from = (XY)p.pos;
			var to = target;
			locked = true;
			var disp = (to - from);
			reticle.visible = true;
			return [
				..Enumerable.Range(0, 30).Select<int,Action>(i => () => {
					//reticle._pos = from + disp * i / 30f;
					reticle._pos += (to - reticle._pos) / 10f;
				}),
			];
		}
		done = true;
		bool msg = true;
		var r = new Rand();
		var a = () => {
			if(msg) {
				p.Tell("Attack_Fire_Weapon");
				msg = false;
			}
			foreach(var i in Enumerable.Range(0, 2)) {	
				p.level.AddEntity(new Splat(reticle.pos + (r.NextInteger(-2, 3), r.NextInteger(-2, 3)), new Tile(ABGR.Blanca, ABGR.Transparent, '*')));
			}
		};
		return [
			a,a,a,a,a,
			a,a,a,a,a,
			() => p.level.RemoveEntity(reticle)
			];
	}
}