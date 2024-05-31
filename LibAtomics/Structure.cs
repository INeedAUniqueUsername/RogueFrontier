using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using LibAtomics;
using LibGamer;
namespace LibAtomics;
public class Wall : IEntity {
	public XYI pos { get; set; } = (0, 0);
	public Tile tile { get; set; } = new Tile(ABGR.White, ABGR.Black, '#');
	public Action Removed { get; set; }
}
public class Floor : IEntity {
	public XYI pos { get; set; }
	public Tile tile { get; set; }
	public Action Removed { get; set; }
	public Floor (XYI pos, Rand r) {
		this.pos = pos;
		tile = new Tile(ABGR.RGBA(25, 25, 36, (byte)(204 + (r.NextFloat() * 51))), ABGR.Black, (char)254);
	}
}
public class Door : IEntity {
	public XYI pos { get; set; }
	public Tile tile => new Tile(ABGR.White, ABGR.Black, closed ? '!' : '.');
	public Action Removed { get; set; }
	public bool closed;
}