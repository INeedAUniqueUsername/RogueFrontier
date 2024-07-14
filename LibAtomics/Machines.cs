using Common;
using LibGamer;

namespace LibTerminator;
/// <summary>Takes items and stashes them somewhere. Usually runs away if encountered. If provoked, summons other Roach bots to fight. Melee attacks.</summary>
public class Roach : IEntity, IActor {
	public XYI pos { get; set; }
	public Tile tile { get; set; } = new Tile(ABGR.Tan, ABGR.Black, 'r');
	public Action Removed { get; set; }

	World world;

	public Roach(World world, XYI pos) {
		this.world = world;
		this.pos = pos;
	}
	Player target;
	Action[] IActor.UpdateTick () {
		target = world.entities.OfType<Player>().FirstOrDefault();
		var r = new Rand();
		var nf = () => r.NextFloat();

		var d = target.pos - pos;
		if(d.manhattan < 2) {
			target.AddMessage(new Player.Message(Tile.Arr($"Roachbot bites Player"), target.time, target.tick));
			//target.delay = 5;
		}
		//var dest = target.pos + new XYI((int)(nf() * d.x - d.x / 2), (int)(nf() * d.y - d.y / 2));

		var dirs = (XYI[])[(-1, 0), (1, 0), (0, -1), (0, 1)];
		var dest = target.pos + Main.GetRandom(dirs, r);
		for(int i = 0; i < 30; i++) {
			var next = dest + Main.GetRandom(dirs, r);
			dest = next;
		}
		var path = GetPath(pos, dest);

		return [.. path.Take(Math.Min(3, path.Count)).Select<XYI, Action>(p => () => pos = p)];
		/*
		for(int i = 0; i < 1; i++) {
			if(path.ElementAtOrDefault(i) is { } p) {
				world.AddEntity(new AfterImage(pos, new Tile(tile.Foreground, tile.Background, '.')));
				pos = p;
			}
		}
		*/
		List<XYI> GetPath(XYI from, XYI to) {
			Dictionary<(int, int), (int, int)> path = [];
			path[from] = from;
			PriorityQueue<XYI, double> queue = new([(from, (from - to).magnitude2)]);
			while(queue.Count > 0) {
				var prev = queue.Dequeue();
				if(prev.x == to.x && prev.y == to.y) {
					List<XYI> results = [];
					var p = to;
					while(!(p.x == from.x && p.y == from.y) && path.ContainsKey(p)) {
						results.Add(p);
						p = path[p];
					}
					results.Reverse();
					return results;
				}
				foreach(var adj in dirs) {
					var next = prev + adj;
					if(path.ContainsKey(next)) {
						continue;
					}
					path[next] = prev;
					queue.Enqueue(next, (next - to).magnitude2);
				}
			}
			return null;
		}
	}
	//Roamer
	public void Expire () {
		string me = "Roach bot";
		var msg = $"{me} flips over.";
	}

}
/// <summary>Ranged attacks.</summary>
public class Sentry {

}