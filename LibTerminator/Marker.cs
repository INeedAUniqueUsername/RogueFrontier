using Common;
using LibGamer;
namespace LibTerminator;

public class Marker : IEntity, IActor {

	public XY _pos = (0, 0);
	public XYI pos => _pos.roundDownI;
	public Tile tile =>
		new Tile(Math.IEEERemainder(time, 0.4) < 0 ? ABGR.Yellow : ABGR.SetA(ABGR.Yellow, 128), ABGR.Transparent, GetGlyph());
	public char GetGlyph() {
		var re = Math.IEEERemainder(time, 0.2);
		return re < 0 ? '?' : '?';
	}
	void IActor.UpdateReal(System.TimeSpan delta) {
		time += delta.TotalSeconds;
	}
	public Action Removed { get; set; }
	public double time = 0;
}
