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

	HashSet<(int x, int y)> visible = [];

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
	public Player (World level, XYI pos) {
		this.level = level;
		this.pos = pos;
	}
	public void Walk (XYI dir) {
		var dest = pos + dir;
		var ent = level.entityMap.GetValueOrDefault(dest, []);
		if(!ent.OfType<Floor>().Any()) {
			AddMessage(new(Tile.Arr("Error_Floor_Not_Found", ABGR.RGB(255, 201, 255)), time, tick));
			return;
		}
		if(ent.OfType<Wall>().Any()) {
			AddMessage(new Message(Tile.Arr("Error_Access_Blocked", ABGR.RGB(255, 201, 255)), time, tick));
			return;
		}
		pos = dest;
		busy = true;
		delay = 5;
	}
	void IActor.UpdateTick () {
		tick++;
		UpdateVision();

		delay--;

		busy = false;
	}
	void IActor.UpdateReal(System.TimeSpan delta) {
		time += delta.TotalSeconds;
	}
	public ConcurrentDictionary<(int, int), Tile> vision = [];
	public void UpdateVision () {
		vision.Clear();
		foreach(var e in level.entities) {
			vision[e.pos] = e.tile;
		}
	}
}
