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
	public int scale = 1;
	public Tf font;
	public int GlyphWidth => font.GlyphWidth;
	public int GlyphHeight => font.GlyphHeight;
	public Rect rect => new Rect(GlyphWidth * pos.xi, GlyphHeight * pos.yi, GlyphWidth * Width, GlyphHeight * Height);
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
	public static Sf From (Sf sf) => new Sf(sf.Width, sf.Height, sf.font);
	public Sf Clone => Sf.From(this);
	public int Width { get; }
	public int Height { get; }
	public int Count => Width * Height;
	public Tile[] Data;
	public Dictionary<(int x,int y), Tile> Active = new();
	public Grid<uint> Front { get; }
	public Grid<uint> Back { get; }
	public Grid<Tile> Tile { get; }
	public bool IsValid (int x, int y) => GetIndex(x, y) is > -1 and { } i && i < Count;
	public int GetIndex (int x, int y) => y * Width + x;
	public void Clear (Tile t = null) {
		redraw = true;
		Active.Clear();
		Array.Fill(Data, t ?? LibGamer.Tile.empty);
	}
	public uint GetFront (int x, int y) => Data[GetIndex(x, y)].Foreground;
	public void SetFront (int x, int y, uint color) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Foreground == color) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Foreground = color };
		Active[(x, y)] = t;
	}
	public uint GetBack (int x, int y) => Data[GetIndex(x, y)].Background;
	public void SetBack (int x, int y, uint color) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Background == color) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Background = color };
		Active[(x, y)] = t;
	}
	public uint GetGlyph (int x, int y) => Data[GetIndex(x, y)].Glyph;
	public void SetGlyph (int x, int y, uint g) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t?.Glyph == g) return;
		redraw = true;
		t = (t ?? LibGamer.Tile.empty) with { Glyph = g };
		Active[(x, y)] = t;
	}
	public Tile GetTile (int x, int y) => Data[GetIndex(x, y)];
	public void SetTile (int x, int y, Tile g) {
		var i = GetIndex(x, y);
		ref var t = ref Data[i];
		if(t == g) return;
		redraw = true;
		t = g;
		Active[(x, y)] = t;
	}
	public void Print (int x, int y, params Tile[] str) {
		var i = GetIndex(x, y);
		foreach(var t in str) {
			Data[i] = t;
			Active[(i % Width, i / Width)] = t;
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
}
public record Tf (byte[] data, string name, int GlyphWidth, int GlyphHeight, int cols, int rows, int solidGlyphIndex) {
	public (int x, int y) GlyphSize => (GlyphWidth, GlyphHeight);
	public int Width => GlyphWidth * cols;
	public int Height=>GlyphHeight * rows;
}