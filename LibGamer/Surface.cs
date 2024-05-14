using Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LibGamer;
/// <summary>
/// Surface of Tiles. Wrapper for array of tiles.
/// </summary>
public class Sf {
	public bool redraw = false;
	public XY pos = (0, 0);
	public Sf(int Width, int Height, int Scale = 1) {
		this.Width = Width;
		this.Height = Height;
		this.Scale = Scale;
		Data = (Tile[])Array.CreateInstance(typeof(Tile), Width * Height);
		Front = new(GetFront, SetFront);
		Back = new(GetBack, SetBack);
		Tile = new(GetTile, SetTile);
	}
	public static Sf From (Sf sf) => new Sf(sf.Width, sf.Height, sf.Scale);
	public int Width { get; }
	public int Height { get; }
	public int Scale { get; }
	public Tile[] Data;

	public HashSet<(int x, int y)> Active = new();
	public Grid<uint> Front { get; }
	public Grid<uint> Back { get; }
	public Grid<Tile> Tile { get; }
	public int GetIndex (int x, int y) => y * Width + x;
	public void Clear (uint front = 0, uint back = 0, uint glyph = 0) {
		redraw = true;

		Active.Clear();
		Array.Fill(Data, LibGamer.Tile.empty);
	}
	public uint GetFront (int x, int y) => Data[GetIndex(x, y)].Foreground;
	public void SetFront (int x, int y, uint color) {
		
		ref var t = ref Data[GetIndex(x, y)];
		if(t.Foreground == color) return;
		Active.Add((x, y));
		redraw = true;
		t = t with { Foreground = color };
	}
	public uint GetBack (int x, int y) => Data[GetIndex(x, y)].Background;
	public void SetBack (int x, int y, uint color) {
		ref var t = ref Data[GetIndex(x, y)];
		if(t.Background == color) return;
		Active.Add((x, y));
		redraw = true;
		t = t with { Background = color };
	}
	public uint GetGlyph (int x, int y) => Data[GetIndex(x, y)].Glyph;
	public void SetGlyph (int x, int y, uint g) {
		ref var t = ref Data[GetIndex(x, y)];
		if(t.Glyph == g) return;
		Active.Add((x, y));
		redraw = true;
		t = t with { Glyph = g };
	}
	public Tile GetTile (int x, int y) => Data[GetIndex(x, y)];
	public void SetTile (int x, int y, Tile g) {

		ref var t = ref Data[GetIndex(x, y)];
		if(t == g) return;
		Active.Add((x, y));
		redraw = true;
		Data[GetIndex(x, y)] = g;
	
	
	}
	public void Print (int x, int y, params Tile[] str) {
		var i = GetIndex(x, y);
		foreach(var t in str) {
			Data[i] = t;
			i++;
		}
	}
	public void Print (int x, int y, string str, uint Front = ABGR.White, uint Back = ABGR.Black) => Print(x, y, LibGamer.Tile.Arr(str,Front, Back));
	public record Grid<T>(Grid<T>.Get get, Grid<T>.Set set) {
		public delegate T Get(int x, int y);
		public delegate void Set (int x, int y, T t);
		public T this[int x, int y] {
			get => get(x, y);
			set => set(x, y, value);
		}
		public static implicit operator Grid<T> ((Get get, Set set) t) => new(t.get, t.set);
	}
}

public record Tf(string path, int GlyphWidth, int GlyphHeight) {
}