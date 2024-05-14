using System;
using System.Collections.Generic;
using System.Linq;
using RangeExtensions;
using Common;

namespace LibGamer;
public record Rect(int x, int y, int width, int height) {

	public Rect () : this(0, 0, 0, 0) {
	}
	public int left = x;
	public int top = y;
	public int right = width + x;
	public int bottom = height + y;
	public static Rect Sides (int left, int top, int right, int bottom) => new(left, top, right - left, bottom - top);
	public Rect Union (Rect r) =>
		Sides(Math.Min(x, r.x), Math.Min(y, r.y), Math.Max(right, r.right), Math.Max(bottom, r.bottom));
	public IEnumerable<(int x, int y)> Perimeter () => [
		..(left..right).Select(x => (x, top)),
		..(left..right).Select(x => (x, bottom)),
		..((top+1)..(bottom-1)).Select(y => (y, left)),
		..((top+1)..(bottom-1)).Select(y => (y, right))
	];
	public bool Contains (XY p) => ((p.x - left) * (right - 1 - p.x), (p.y - top) * (bottom - 1 - p.y)) is (>=0, >=0);
}
