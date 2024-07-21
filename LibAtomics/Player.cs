using Common;
using LibGamer;
using RangeExtensions;
using System.Collections.Concurrent;
using System.Xml.Linq;
namespace LibAtomics;
public class Player : IActor, IEntity {
	public World level;
	public Tile tile => new(Mainframe.PINK, ABGR.Black, '@');
	public XYI pos { get; set; }
	public Action Removed { get; set; }
	public bool ready => !busy && delay < 1;
	public bool busy;
	public double lastUpdateTime;
	public double time;
	public int tick;
	public int delay = 0;
	public Body body = new(Body.Parse(XElement.Parse(Body.cockroach)));
	public HashSet<Item> items = [];
	public record Message (Tile[] str, double time, int tick) {
		public int repeats = 1;
		public bool once => repeats == 1;
		public double fadeTime;
		public string text => new([.. from t in str select (char)t.Glyph]);
	};
	public List<Message> messages = [];
	public void Tell(Message m) {
		if(messages.LastOrDefault() is { }prev && prev.text == m.text && prev.tick == m.tick) {
			var other = messages[^1];
			m.repeats += other.repeats;
			messages.RemoveAt(messages.Count - 1);
		}
		messages.Add(m);
	}
	public void Tell(string msg) =>
		Tell(new Message(Tile.Arr(msg), time, tick));
	public Player (World level, XYI pos) {
		this.level = level;
		this.pos = pos;
	}
	public void Walk (XYI dir) {
		var dest = pos + dir;
		var ent = level.entityMap.GetValueOrDefault(dest, []);
		if(!ent.OfType<Floor>().Any()) {
			string msg = true ? "cannot walk there." : "ERROR_FLOOR_NOT_FOUND";
			Tell((Message)new(Tile.Arr(msg, ABGR.White), time, tick));
			return;
		}
		if(ent.OfType<Wall>().Any()) {
			string msg = true ? "blocked by wall." : "ERROR_ACCESS_DENIED_BY_WALL";
			Tell(new Message(Tile.Arr(msg, ABGR.White), time, tick));
			return;
		}
		pos = dest;
		busy = true;
		delay = 1;
	}
	public Shoot shoot;
	Action[] IActor.UpdateTick () {
		foreach(var m in Enumerable.Reverse(messages)) {
			if(m.tick != tick) {
				break;
			}
			m.fadeTime = time;
		}
		tick++;
		body.UpdateTick();
		UpdateVision();
		delay--;
		busy = false;

		HashSet<Action[]> r = [shoot?.Act(this)?? []];

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
		lastUpdateTime = time;
		return [..Zip()];
		return [

			() => Tell(new Message(Tile.Arr("subtick 1"), time, tick)),
			() => Tell(new Message(Tile.Arr("subtick 2"), time, tick)),
			() => Tell(new Message(Tile.Arr("subtick 3"), time, tick)),
			() => Tell(new Message(Tile.Arr("subtick 4"), time, tick)),
			() => Tell(new Message(Tile.Arr("subtick 5"), time, tick)),
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

interface IShootTarget { XYI pos { get; } }
public record ShootEntity(IEntity target) : IShootTarget {
	XYI IShootTarget.pos => target.pos;
}
public record ShootLoc(XYI loc) : IShootTarget {
	XYI IShootTarget.pos => loc;
}
public class Shoot {
	public XY target;
	Reticle reticle;

	bool precise = false;
	bool locked = false;
	public bool done = false;
	public void Init(Player p) {
		reticle = new () { _pos = (XY)p.pos, visible = true };
		p.level.AddEntity(reticle);
		p.Tell("selecting target");
	}
	public Action[] Act(Player p) {
		if(done) return [];
		void Fire() {
			var r = new Rand();
			foreach(var i in 2) {
				var spread = (reticle._pos - ((XY)p.pos)).magnitude / 8;
				var loc = (reticle._pos + (r.NextDouble(-spread, spread), r.NextDouble(-spread, spread))).roundDownI;
				p.level.AddEntity(new Splat(loc, new Tile(ABGR.Blanca, ABGR.Transparent, '*')));
				var hits = p.level.entityMap[loc].Where(e => e is not Splat and not Reticle and not Marker).ToList();
				if(hits is { Count: > 0 } any) {
					switch(any.GetRandom(r)) {
						case Roach:
							p.Tell("hit roach");
							break;
						case Floor:
							p.Tell("miss");
							break;
						default:
							break;
					}
				}
			}
		}
		void Aim (XY to, double time) {
			reticle._pos += (to - reticle._pos) / (time / 3);
		}
		void RemoveReticle () => p.level.RemoveEntity(reticle);
		if(precise) {
			if(!locked) {
				p.Tell("acquiring target");
				var from = (XY)p.pos;
				var to = target;
				locked = true;
				//var disp = (to - from);
				reticle.visible = true;

				var Acquire = () => {
					//reticle._pos = from + disp * i / 30f;
					Aim(to, 30);
				};
				return [..from i in 30 select Acquire];
			}

			done = true;
			bool msg = true;
			var Attack = () => {
				if(msg) {
					p.Tell("firing weapon");
					msg = false;
				}
				Fire();
			};
			return [..from i in 15 select Attack, RemoveReticle];
		} else {
			done = true;
			var from = (XY)p.pos;
			var to = target;
			locked = true;
			//var disp = (to - from);
			reticle.visible = true;
			var Attack = () => {
				Aim(to, 20);
				Fire();
			};
			return [..from i in 20 select Attack, RemoveReticle];
		}
	}
}