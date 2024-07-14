using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibTerminator;
public class Reticle : IEntity, IActor {
	public XY _pos { get; set; } = (0, 0);
	public XYI pos => _pos.roundDownI;
	public bool visible = false;
	public Tile tile =>
		visible ?
			new Tile(Math.IEEERemainder(time, 0.2) < 0 ? ABGR.Yellow : ABGR.SetA(ABGR.Yellow, 128), ABGR.Black, '+') :
			Tile.empty;
	public Action Removed { get; set; }
	double time = 0;
	void IActor.UpdateReal(System.TimeSpan delta) {
		time += delta.TotalSeconds;
	}
}