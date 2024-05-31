using Common;
using LibGamer;
namespace LibAtomics;

public class Marker : IEntity {
	public XYI pos { get; set; } = (0, 0);
	public Tile tile => 
		new Tile(ABGR.Black, ABGR.Yellow, GetGlyph());
	public char GetGlyph() {
		var re = Math.IEEERemainder(time, 0.5);
		return re < 0 ? '?' : ' ';
	}
	public Action Removed { get; set; }
	public double time = 0;
}
