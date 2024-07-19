using System.Collections.Concurrent;
using LibGamer;
namespace LibAtomics;
public class World {
	public HashSet<IEntity> entities => _entities.items;
	BufferedSet<IEntity> _entities = new();
	public IActor[] actors = [];
	public double time;
	public double lastUpdate;

	private ConcurrentDictionary<(int, int), IEntity[]> _entityMap = new();
	public ConcurrentDictionary<(int, int), IEntity[]> entityMap {
		get {
			if(updateMap) {
				_entityMap.Clear();
				foreach(var g in entities.GroupBy(e => (e.pos.x, e.pos.y))) {
					_entityMap[g.Key] = g.ToArray();
				}
				updateMap = false;
			}
			return _entityMap;
		}
	}
	bool busy = false;
	bool updateMap = true;
	public void AddEntity(IEntity e) {
		if(_entities.Add(e, busy)) {
			e.Removed += () => RemoveEntity(e);
			TryUpdatePresent();
		}
	}
	public void RemoveEntity(IEntity e) {
		if(_entities.Remove(e, busy)) {
			TryUpdatePresent();
		}
	}
	public void TryUpdatePresent () {
		if(!busy)
			UpdatePresent();
	}
	private void UpdatePresent() {
		_entities.Update();
		actors = [.._entities.items.OfType<IActor>()];
		updateMap = true;
		lastUpdate = time;
	}
	public record Subticks (Action[][] lines = null, Action step = null, Action end = null) {
		public List<Action[]> remaining = [.. lines ?? []];
		public int length => lines.Max(line => line.Length);
		public bool done => remaining.Count == 0;
		public int subtick = 0;
		public void Update() {
			remaining.RemoveAll(line => subtick >= line.Length);
			remaining.ForEach(line => line[subtick].Invoke());
			step?.Invoke();
			if(done)
				end?.Invoke();
			subtick++;
		}
	}
	public Subticks UpdateStep() {
		busy = true;
		var s = new Subticks([..from a in actors select a.UpdateTick()], UpdatePresent);
		busy = false;
		return s;
	}
	public void UpdateReal(TimeSpan delta) {
		time += delta.TotalSeconds;
		foreach(var e in actors) e.UpdateReal(delta);
	}
}
