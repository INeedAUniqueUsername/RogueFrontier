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

		shoot = shoot ?? new Shoot() { target = (10, 1) };
		
	}

	Shoot shoot;
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

	int lastCheck = -1;
	public void UpdateVision () {
		if(lastCheck == tick) {
			return;
		}
		lastCheck = tick;
		_visibleTiles = null;
		visibleAt.Clear();
		visible.Clear();
		if(false) {
			foreach(var e in level.entities) {
				visibleTiles[e.pos] = e.tile;
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
	public Action[] Act(Player p) {

		if(!locked) {
			reticle = new Reticle() { _pos = (p.pos.x, p.pos.y) };
			p.level.AddEntity(reticle);
			locked = true;
			return [
				..Enumerable.Range(0, 10).Select<int,Action>(i => () => {
					reticle._pos += (target - reticle._pos) / 5f;
				}),
			];
		}

		var a = () => {

			p.Tell("Attack!");
			var r = new Rand();
			p.level.AddEntity(new Splat(reticle.pos + (r.NextInteger(-3, 4), r.NextInteger(-3, 4)), new Tile(ABGR.Blanca, ABGR.Transparent, '*')));
		};
		return [
			a,a,a,a,a,
			a,a,a,a,a,
			a,a,a,a,a,
			a,a,a,a,a,
			];
	}
}