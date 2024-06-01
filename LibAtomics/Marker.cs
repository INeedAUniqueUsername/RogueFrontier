using Common;
using LibGamer;
namespace LibAtomics;

public class Marker : IEntity, IActor {

	public XY _pos = (0, 0);
	public XYI pos => _pos.roundDownI;
	public Tile tile =>
		new Tile(ABGR.Black, ABGR.Yellow, GetGlyph());
	public char GetGlyph() {
		var re = Math.IEEERemainder(time, 0.5);
		return re < 0 ? '?' : ' ';
	}
	void IActor.UpdateReal(System.TimeSpan delta) {
		time += delta.TotalSeconds;
	}
	public Action Removed { get; set; }
	public double time = 0;
}
