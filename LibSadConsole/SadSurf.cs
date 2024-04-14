using Common;
using LibGamer;
using SadConsole;
using SadRogue.Primitives;
using Console = SadConsole.Console;
namespace LibSadConsole;
public class SadSurf: ISurf {
	Console console;
	public ISurf.Grid<uint> Front { get; set; }
	public ISurf.Grid<uint> Back { get; set; }
	public ISurf.Grid<uint> Glyph { get; set; }
	public ISurf.Grid<Tile> Tile { get; set; }
	public SadSurf (Console console) {
		this.console = console;
		Front = (GetFront, SetFront);
		Back = (GetBack, SetBack);
		Glyph = (GetGlyph, SetGlyph);
		Tile = (GetTile, SetTile);
	}
	public uint GetFront (int x, int y) => console.GetForeground(x, y).PackedValue;
	public void SetFront (int x, int y, uint c) => console.SetForeground(x, y, new Color(c));
	public uint GetBack (int x, int y) => console.GetBackground(x, y).PackedValue;
	public void SetBack (int x, int y, uint c) => console.SetBackground(x, y, new Color(c));
	public uint GetGlyph (int x, int y) => (uint)console.GetGlyph(x, y);
	public void SetGlyph (int x, int y, uint c) => console.SetGlyph(x, y, (int)c);
	public Tile GetTile (int x, int y) {
		var g = console.GetCellAppearance(x, y);
		return new Tile(g.Foreground.PackedValue, g.Background.PackedValue, g.Glyph); 
	}
	public void SetTile (int x, int y, Tile t) => console.SetCellAppearance(x, y, new ColoredGlyph(
		new Color(t.Foreground),
		new Color(t.Background),
		(int)t.Glyph
		));

}