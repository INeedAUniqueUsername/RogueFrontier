using System.Linq;
using System.Collections.Generic;
using System;
using LibGamer;

namespace LibGamer;

public class SfList {
	List<Sf> consoles;
	public Tile this[int x, int y] {
		get {

			//List<CellDecorator> d = new List<CellDecorator>();
			var f = ABGR.Transparent;
			var b = ABGR.Transparent;
			var g = 0;
			foreach (var c in consoles) {
				var cg = c.GetTile(x, y);
				if (cg.Glyph != 0 && cg.Glyph != ' ' && ABGR.A(cg.Foreground) != 0) {
					if (g != 0 && g != ' ' && ABGR.A(f) != 0) {
						//d.Add(new CellDecorator(f, g, Mirror.None));
					}
					f = cg.Foreground;
					g = (int)cg.Glyph;
				}
				b = ABGR.Blend(ABGR.Premultiply(b), cg.Background);
			}
			return new Tile(f, b, g);
		}
	}
	public SfList(params Sf[] consoles) => this.consoles = new List<Sf>(consoles);
	public SfList(IEnumerable<Sf> consoles) => this.consoles = new List<Sf>(consoles);
}
