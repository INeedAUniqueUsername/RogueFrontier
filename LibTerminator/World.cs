using System.Collections.Concurrent;
namespace LibTerminator;
public class World {
	public HashSet<IEntity> entitiesAdd = [];
	public HashSet<IEntity> entitiesRemove = [];
	public HashSet<IEntity> entities = [];
	public IActor[] actors = [];

	public double time;
	public double lastUpdate;

	private ConcurrentDictionary<(int, int), IEntity[]> _entityMap;
	public ConcurrentDictionary<(int, int), IEntity[]> entityMap =>
		_entityMap = _entityMap ?? new(entities.GroupBy(e => (e.pos.x, e.pos.y)).Select(g => (g.Key, g.ToArray())).ToDictionary());
	bool busy = false;
	public void AddEntity(IEntity e) {
		entitiesRemove.Remove(e);
		if(entities.Contains(e))
			return;
		e.Removed += () => RemoveEntity(e);
		entitiesAdd.Add(e);
		PlaceEntity();
	}
	public void RemoveEntity(IEntity e) {
		entitiesAdd.Remove(e);
		if(!entities.Contains(e)) return;
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
		_entityMap = null;
		lastUpdate = time;
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
		time += delta.TotalSeconds;
		foreach(var e in actors) e.UpdateReal(delta);
	}
}