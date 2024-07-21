using Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
namespace LibGamer;
public interface SfContainer {
	Sf sf { get; set; }
	int Width => sf.Width;
	int Height => sf.Height;
}
/// <summary>
/// Surface of Tiles. Wrapper for array of tiles.
/// </summary>
public class Sf {
	public bool redraw = false;
	public XY pos = (0, 0);
	public double scale = 1;
	public Tf font;
	public int GlyphWidth => font.GlyphWidth;
	public int GlyphHeight => font.GlyphHeight;
	public Rect rect => new Rect(GlyphWidth * pos.xi, GlyphHeight * pos.yi, GlyphWidth * Width, GlyphHeight * Height);
	public Rect SubRect (int x, int y, int w, int h) => new Rect((pos.xi + x) * GlyphWidth, (y + pos.yi) * GlyphHeight, w * GlyphWidth, h * GlyphHeight);
	public Sf(int Width, int Height, Tf font) {
		this.Width = Width;
		this.Height = Height;
		this.font = font;
		Data = (Tile[])Array.CreateInstance(typeof(Tile), Width * Height);
		Array.Fill(Data, LibGamer.Tile.empty);
		Front = new(GetFront, SetFront);
		Back = new(GetBack, SetBack);
		Tile = new(GetTile, SetTile);
	}
	public static Sf Display(int w, int h, Tf font, Dictionary<(int x,int y), Tile> img) {
		Sf sf = new Sf(w, h, font);
		foreach(var(p, t) in img) {
			sf.Tile[p] = t;
		}
		return sf;
	}
	public static Sf From (Sf sf) => new Sf(sf.Width, sf.Height, sf.font);
	public Sf Clone => Sf.From(this);
	public int Width { get; }
	public int Height { get; }
	public int Count => Width * Height;
	public Tile[] Data;
	public HashSet<(int x,int y)> Active = new();
	public Grid<uint> Front { get; }
	public Grid<uint> Back { get; }
	public Grid<Tile> Tile { get; }
	public bool IsValid (int x, int y) => GetIndex(x, y) is > -1 and { } i && i < Count;
	public int GetIndex (int x, int y) => y * Width + x;
	public void Clear (Tile t = null) {
		redraw = true;
		Active.Clear();
		Array.Fill(Data, LibGamer.Tile.empty);
	}
	public uint GetFront (int x, int y) => Data[GetIndex(x, y)].Foreground;
	public void SetFront (int x, int y, uint color) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Foreground == color) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Foreground = color };
		Active.Add((x, y));
	}
	public uint GetBack (int x, int y) => Data[GetIndex(x, y)].Background;
	public void SetBack (int x, int y, uint color) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Background == color) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Background = color };
		Active.Add((x, y));
	}
	public uint GetGlyph (int x, int y) => Data[GetIndex(x, y)].Glyph;
	public void SetGlyph (int x, int y, uint g) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Glyph == g) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Glyph = g };
		Active.Add((x, y));
	}
	public Tile GetTile (int x, int y) => Data[GetIndex(x, y)];
	public void SetTile (int x, int y, Tile g) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t == g) return;
		redraw = true;
		t = g;
		Active.Add((x, y));
	}
	public void Print (int x, int y, params Tile[] str) {
		var i = GetIndex(x, y);
		foreach(var t in str) {
			Data[i] = t;
			Active.Add((i % Width, i / Width));
			i++;
		}
	}
	public void Print (int x, int y, string str, uint Front = ABGR.White, uint Back = ABGR.Black) => Print(x, y, LibGamer.Tile.Arr(str,Front, Back));
	public void Print (int x, int y, char c, uint Front = ABGR.White, uint Back = ABGR.Black) => Print(x, y, $"{c}", Front, Back);
	public record Grid<T>(Grid<T>.Get get, Grid<T>.Set set) {
		public delegate T Get(int x, int y);
		public delegate void Set (int x, int y, T t);
		public T this[int x, int y] {
			get => get(x, y);
			set => set(x, y, value);
		}
		public T this[(int x, int y) p] {
			get => this[p.x, p.y];
			set => this[p.x, p.y] = value;
		}
		public static implicit operator Grid<T> ((Get get, Set set) t) => new(t.get, t.set);
	}
	public static void DrawRect ( Sf sf, int xStart, int yStart, int dx, int dy, RectOptions op) {
		char Box (Line n = Line.None, Line e = Line.None, Line s = Line.None, Line w = Line.None) =>
			(char)BoxInfo.IBMCGA.glyphFromInfo[new(n, e, s, w)];
		var width = op.width;
		var aboveWidth = op.connectAbove ? width : Line.None;
		var belowWidth = op.connectBelow ? width : Line.None;
		var vert = Box(n: width, s: width);
		var hori = Box(e: width, w: width);
		void c (int x, int y, char c) =>
				sf.Print(x, y, new Tile(op.f, op.b, c));
		void l (int x, int y, string line) =>
				sf.Print(x, y, LibGamer.Tile.Arr(line, op.f, op.b));
		int y = yStart;
		void p (string line) =>
				sf.Print(xStart, y++, LibGamer.Tile.Arr(line, op.f, op.b));
		bool fill = ABGR.A(op.b) != 0;
		if(dx == 0 || dy == 0)
			return;
		if(dx == 1) {
			var n = Box(e: Line.Single, w: Line.Single, s: width, n: aboveWidth);
			var s = Box(e: Line.Single, w: Line.Single, n: width, s: belowWidth);
			p($"{n}");
			foreach(var i in Math.Max(0, dy - 1))
				p($"{vert}");
			p($"{s}");
		} else if(dy == 1) {
			var e = Box(n: Line.Single, s: Line.Single, w: width);
			var w = Box(n: Line.Single, s: Line.Single, e: width);
			p($"{w}{$"{hori}".Repeat(dx - 2)}{e}");
		} else {
			var nw = Box(e: width, s: width, n: aboveWidth);
			var ne = Box(w: width, s: width, n: aboveWidth);
			var sw = Box(e: width, n: width, s: belowWidth);
			var se = Box(w: width, n: width, s: belowWidth);
			if(fill) {
				p($"{nw}{$"{hori}".Repeat(dx - 2)}{ne}");
				foreach(var i in Math.Max(0, dy - 2))
					p($"{vert}{" ".Repeat(dx - 2)}{vert}");
				p($"{sw}{$"{hori}".Repeat(dx - 2)}{se}");
			} else {
				var x = xStart;
				l(x, y, $"{nw}{$"{hori}".Repeat(dx - 2)}{ne}"); y++;
				foreach(var i in Math.Max(0, dy - 1)) {
					c(x, y, vert); c(x + dx - 1, y, vert); y++;
				}
				l(x, y, $"{sw}{$"{hori}".Repeat(dx - 2)}{se}"); y++;
			}
		}
	}
}
public record Tf (byte[] data, string name, int GlyphWidth, int GlyphHeight, int cols, int rows, int solidGlyphIndex) {
	public (int x, int y) GlyphSize => (GlyphWidth, GlyphHeight);
	public int Width => GlyphWidth * cols;
	public int Height=>GlyphHeight * rows;
}
public class RectOptions {
	public bool connectBelow, connectAbove;
	public Line width = Line.Single;
	public uint f = ABGR.White, b = ABGR.Black;
}