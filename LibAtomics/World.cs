using System.Collections.Concurrent;
namespace LibAtomics;
public class World {
	public HashSet<IEntity> entitiesAdd = [];
	public HashSet<IEntity> entitiesRemove = [];
	public HashSet<IEntity> entities = [];
	public IActor[] actors = [];
	public ConcurrentDictionary<(int, int), IEntity[]> entityMap =>
		new(entities.GroupBy(e => (e.pos.x, e.pos.y)).Select(g => (g.Key, g.ToArray())).ToDictionary());
	bool busy = false;
	public void AddEntity(IEntity e) {
		e.Removed += () => RemoveEntity(e);
		entitiesAdd.Add(e);
		PlaceEntity();
	}
	public void RemoveEntity(IEntity e) {
			entitiesRemove.Add(e);
		PlaceEntity();
	}
	public void PlaceEntity () {
		if(!busy)
			UpdatePresent();
	}
	private void UpdatePresent() {
		entities.UnionWith(entitiesAdd);
		entities.ExceptWith(entitiesRemove);
		entitiesAdd.Clear();
		entitiesRemove.Clear();
		actors = [..entities.OfType<IActor>()];
	}
	public record Subticks (Action[][] lines, Action update, Action end) {
		public HashSet<Action[]> remaining => [..from line in lines where subtick < line.Length select line];
		public int length => lines.Max(line => line.Length);
		public bool done => remaining.Count == 0;
		public int subtick = 0;
		public void Update() {
			remaining.RemoveWhere(line => subtick >= line.Length);
			foreach(var line in remaining) {
				line[subtick]();
			}
			subtick++;
			update();
			if(done) {
				end();
			}
		}
	}
	public Subticks UpdateStep() {
		busy = true;
		var s = new Subticks([..from a in actors select a.UpdateTick()], () => {
			UpdatePresent();
		}, () => {
		});
		busy = false;
		return s;
	}
	public void UpdateReal(TimeSpan delta) {
		foreach(var e in actors) e.UpdateReal(delta);
	}
}