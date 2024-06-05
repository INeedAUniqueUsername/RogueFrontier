using Common;
using LibGamer;

namespace LibTerminator;

public class Splat : IEntity, IActor {
	public XYI pos { get; set; }
	public Tile tile { get; set; }
	public Action Removed { get; set; }
	public Splat(XYI pos, Tile tile) {
		this.pos = pos;
		this.tile = tile;
	}
	Action[] IActor.UpdateTick () {
		Removed?.Invoke();
		return [];
	}
	public double time = 0.1;
	void IActor.UpdateReal(System.TimeSpan delta) {
		if(time < 0) {
			return;
		}
		time -= delta.TotalSeconds;
		if(time < 0) {
			tile = tile with { Foreground = ABGR.SetA(tile.Foreground, (byte)(ABGR.A(tile.Foreground)/2)) };
		}
	}
}
