using Common;
using LibGamer;
using SadConsole;
using SadConsole.Input;
using SFML.System;
namespace ExtSadConsole;
public static class SadGamer {
	public static Vector3f ToV3f (this XY xy, float z = 0) => new(xy.xf, xy.yf, z);
	public static Tile ToTile (this ColoredGlyphBase cg) => new Tile(cg.Foreground.PackedValue, cg.Background.PackedValue, cg.Glyph);
	public static ColoredGlyphBase ToCG (this Tile t) =>
		new ColoredGlyph(new SadRogue.Primitives.Color(t.Foreground), new SadRogue.Primitives.Color(t.Background), (int)t.Glyph);
	public static KB ToKB (this Keyboard info) => new(
		[.. info.KeysPressed.Select(k => (KC)k.Key)],
		[.. info.KeysDown.Select(k => (KC)k.Key)],
		[.. info.KeysReleased.Select(k => (KC)k.Key)]
		);
}