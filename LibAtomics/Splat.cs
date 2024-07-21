using Common;
using LibGamer;

namespace LibAtomics;

public class Splat : IEntity, IActor {
	public XYI pos { get; set; }
	public Tile tile { get; set; }

	public Tile original;

	public Action Removed { get; set; }
	public Splat(XYI pos, Tile tile) {
		this.pos = pos;
		this.tile = tile;
		this.original = tile;
	}
	Action[] IActor.UpdateTick () {
		Removed?.Invoke();
		return [];
	}
	public double time = 0.2;
	void IActor.UpdateReal(System.TimeSpan delta) {
		if(time < 0) {
			return;
		}
		time -= delta.TotalSeconds;

		tile = original with {
			Foreground = ABGR.Blend(ABGR.Black, ABGR.SetA(original.Foreground, (byte)Main.Lerp(time, 0, 0.2, 51, 255, 1)))
		};
	}
}
