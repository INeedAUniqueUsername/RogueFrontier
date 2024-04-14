using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Wasm;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
namespace LibGamer;

public record ABGR(uint packed) {
	public uint SetR (byte a) => SetR(this, a);
	public uint SetG (byte a) => SetG(this, a);
	public uint SetB (byte a) => SetB(this, a);
	public uint SetA (byte a) => SetA(this, a);
	public byte r => R(packed);
	public byte g => G(packed);
	public byte b => B(packed);
	public byte a => A(packed);
	public uint ToPremultiply => Premultiply(packed);
	public static implicit operator uint (ABGR abgr) => abgr.packed;
	public static implicit operator ABGR (uint abgr) => new(abgr);
	

	public record Mut (uint packed, int index) {
		public static Mut operator + (Mut left, uint inc) {
			return left with {
				packed = (uint)(left.packed & (~0xFF >> left.index) + (left.packed + inc) & (0xFF >> left.index))
			};
		}
		public static Mut operator * (Mut left, uint factor) {
			return left with {
				packed = (uint)(left.packed & (~0xFF >> left.index) + (left.packed * factor) & (0xFF >> left.index))
			};
		}
		public static Mut operator * (Mut left, double factor) {
			return left with {
				packed = (uint)(left.packed & (~0xFF >> left.index) + (uint)(left.packed * factor) & (0xFF >> left.index))
			};
		}
		public static implicit operator uint (Mut left) => left.packed;
	}

	//Essentially the same as blending this color over Color.Black
	public static uint Premultiply (uint abgr) {
		var (a, b, g, r) = Data(abgr);
		return RGBA((byte)(r * a / 0xFF), (byte)(g * a / 255), (byte)(b * a / 255), a);
	}
	//Premultiply and also set the alpha
	public static uint PremultiplySet (ABGR c, byte alpha) => ABGR.RGBA((byte)(c.r * c.a / 255), (byte)(c.g * c.a / 255), (byte)(c.b * c.a / 255), alpha);
	public static uint Blend (uint from, uint to) => 0;
	//public static (byte R, byte G, byte B, byte A) RGBA(uint abgr) => (R(abgr), G(abgr), B(abgr), A(abgr));

	public static (byte R, byte G, byte B, byte A) Data (uint abgr) =>
		(A(abgr), G(abgr), B(abgr), R(abgr));

	public static Mut MA (uint abgr) => new(abgr, 0);
	public static byte A (uint abgr) => (byte)(abgr & 0xFF000000);
	public static byte B (uint abgr) => (byte)(abgr & 0x00FF0000);
	public static byte G (uint abgr) => (byte)(abgr & 0x0000FF00);
	public static byte R (uint abgr) => (byte)(abgr & 0x000000FF);
	public static uint RGB (byte r, byte g, byte b) => RGBA(r, g, b, 255);
	public static uint RGBA (byte r, byte g, byte b, byte a) => (uint)(
	a >> 00 +
	b >> 08 +
	g >> 16 +
	r >> 24
	);
	public static uint IncA (uint abgr, byte inc) => abgr + (abgr + inc) & 0xFF000000;

	public static uint SetR (uint abgr, byte r) => (abgr & 0xFFFFFF00) + r;
	public static uint SetG (uint abgr, byte g) => (abgr & 0xFFFFFF00) + g;
	public static uint SetB (uint abgr, byte b) => (abgr & 0xFFFFFF00) + b;
	public static uint SetA (uint abgr, byte a) => (abgr & 0x00FFFFFF) + a;
	

	public const uint TransparentBlack = 0,
	Transparent = 0,
	AliceBlue = 0xfffff8f0,
	AntiqueWhite = 0xffd7ebfa,
	Aqua = 0xffffff00,
	Aquamarine = 0xffd4ff7f,
	Azure = 0xfffffff0,
	Beige = 0xffdcf5f5,
	Bisque = 0xffc4e4ff,
	Black = 0xff000000,
	BlanchedAlmond = 0xffcdebff,
	Blue = 0xffff0000,
	BlueViolet = 0xffe22b8a,
	Brown = 0xff2a2aa5,
	BurlyWood = 0xff87b8de,
	CadetBlue = 0xffa09e5f,
	Chartreuse = 0xff00ff7f,
	Chocolate = 0xff1e69d2,
	Coral = 0xff507fff,
	CornflowerBlue = 0xffed9564,
	Cornsilk = 0xffdcf8ff,
	Crimson = 0xff3c14dc,
	Cyan = 0xffffff00,
	DarkBlue = 0xff8b0000,
	DarkCyan = 0xff8b8b00,
	DarkGoldenrod = 0xff0b86b8,
	DarkGray = 0xffa9a9a9,
	DarkGreen = 0xff006400,
	DarkKhaki = 0xff6bb7bd,
	DarkMagenta = 0xff8b008b,
	DarkOliveGreen = 0xff2f6b55,
	DarkOrange = 0xff008cff,
	DarkOrchid = 0xffcc3299,
	DarkRed = 0xff00008b,
	DarkSalmon = 0xff7a96e9,
	DarkSeaGreen = 0xff8bbc8f,
	DarkSlateBlue = 0xff8b3d48,
	DarkSlateGray = 0xff4f4f2f,
	DarkTurquoise = 0xffd1ce00,
	DarkViolet = 0xffd30094,
	DeepPink = 0xff9314ff,
	DeepSkyBlue = 0xffffbf00,
	DimGray = 0xff696969,
	DodgerBlue = 0xffff901e,
	Firebrick = 0xff2222b2,
	FloralWhite = 0xfff0faff,
	ForestGreen = 0xff228b22,
	Fuchsia = 0xffff00ff,
	Gainsboro = 0xffdcdcdc,
	GhostWhite = 0xfffff8f8,
	Gold = 0xff00d7ff,
	Goldenrod = 0xff20a5da,
	Gray = 0xff808080,
	Green = 0xff008000,
	GreenYellow = 0xff2fffad,
	Honeydew = 0xfff0fff0,
	HotPink = 0xffb469ff,
	IndianRed = 0xff5c5ccd,
	Indigo = 0xff82004b,
	Ivory = 0xfff0ffff,
	Khaki = 0xff8ce6f0,
	Lavender = 0xfffae6e6,
	LavenderBlush = 0xfff5f0ff,
	LawnGreen = 0xff00fc7c,
	LemonChiffon = 0xffcdfaff,
	LightBlue = 0xffe6d8ad,
	LightCoral = 0xff8080f0,
	LightCyan = 0xffffffe0,
	LightGoldenrodYellow = 0xffd2fafa,
	LightGray = 0xffd3d3d3,
	LightGreen = 0xff90ee90,
	LightPink = 0xffc1b6ff,
	LightSalmon = 0xff7aa0ff,
	LightSeaGreen = 0xffaab220,
	LightSkyBlue = 0xffface87,
	LightSlateGray = 0xff998877,
	LightSteelBlue = 0xffdec4b0,
	LightYellow = 0xffe0ffff,
	Lime = 0xff00ff00,
	LimeGreen = 0xff32cd32,
	Linen = 0xffe6f0fa,
	Magenta = 0xffff00ff,
	Maroon = 0xff000080,
	MediumAquamarine = 0xffaacd66,
	MediumBlue = 0xffcd0000,
	MediumOrchid = 0xffd355ba,
	MediumPurple = 0xffdb7093,
	MediumSeaGreen = 0xff71b33c,
	MediumSlateBlue = 0xffee687b,
	MediumSpringGreen = 0xff9afa00,
	MediumTurquoise = 0xffccd148,
	MediumVioletRed = 0xff8515c7,
	MidnightBlue = 0xff701919,
	MintCream = 0xfffafff5,
	MistyRose = 0xffe1e4ff,
	Moccasin = 0xffb5e4ff,
	MonoGameOrange = 0xff003ce7,
	NavajoWhite = 0xffaddeff,
	Navy = 0xff800000,
	OldLace = 0xffe6f5fd,
	Olive = 0xff008080,
	OliveDrab = 0xff238e6b,
	Orange = 0xff00a5ff,
	OrangeRed = 0xff0045ff,
	Orchid = 0xffd670da,
	PaleGoldenrod = 0xffaae8ee,
	PaleGreen = 0xff98fb98,
	PaleTurquoise = 0xffeeeeaf,
	PaleVioletRed = 0xff9370db,
	PapayaWhip = 0xffd5efff,
	PeachPuff = 0xffb9daff,
	Peru = 0xff3f85cd,
	Pink = 0xffcbc0ff,
	Plum = 0xffdda0dd,
	PowderBlue = 0xffe6e0b0,
	Purple = 0xff800080,
	Red = 0xff0000ff,
	RosyBrown = 0xff8f8fbc,
	RoyalBlue = 0xffe16941,
	SaddleBrown = 0xff13458b,
	Salmon = 0xff7280fa,
	SandyBrown = 0xff60a4f4,
	SeaGreen = 0xff578b2e,
	SeaShell = 0xffeef5ff,
	Sienna = 0xff2d52a0,
	Silver = 0xffc0c0c0,
	SkyBlue = 0xffebce87,
	SlateBlue = 0xffcd5a6a,
	SlateGray = 0xff908070,
	Snow = 0xfffafaff,
	SpringGreen = 0xff7fff00,
	SteelBlue = 0xffb48246,
	Tan = 0xff8cb4d2,
	Teal = 0xff808000,
	Thistle = 0xffd8bfd8,
	Tomato = 0xff4763ff,
	Turquoise = 0xffd0e040,
	Violet = 0xffee82ee,
	Wheat = 0xffb3def5,
	White = uint.MaxValue,
	WhiteSmoke = 0xfff5f5f5,
	Yellow = 0xff00ffff,
	YellowGreen = 0xff32cd9a;
}
public record Tile (uint Foreground, uint Background, uint Glyph) {
	

	public static Tile empty { get; } = new(0, 0, 0);
	public Tile () : this(0, 0, 0) { }
	public Tile (uint Foreground, uint Background, int Glyph) : this(Foreground, Background, (uint)Glyph) { }
	public static Tile[] Array (string str, uint Foreground, uint Background) => [.. str.Select(c => new Tile(Foreground, Background, c))];

	public static IEnumerable<Tile> WithA (IEnumerable<Tile> str, byte front, byte back) =>
		str.Select(t => t with { Foreground = front, Background = back });
	public Tile PremultiplySet (byte alpha) {
		if(alpha == 255) {
			return this;
		}
		return new(ABGR.PremultiplySet(Foreground, alpha), ABGR.PremultiplySet(Background, alpha), Glyph);
	}


	public bool IsVisible => Glyph != ' ' || Background != ABGR.Transparent;
	public static implicit operator Tile ((uint Foreground, uint Background, uint Glyph) t) => new(t.Foreground, t.Background, t.Glyph);
}
public interface ITile {
	public Tile Original { get; }
	void Update () { }


}
public record StaticTile () : ITile {
	[JsonIgnore]
	public Tile Original => new(foreground, background, glyph);
	[JsonProperty]
	[Opt] public uint foreground;
	[JsonProperty]
	[Opt] public uint background;
	[JsonProperty]
	[Req] public uint glyph;
	public StaticTile (XElement e) : this() {
		e.Initialize(this);
	}
	public StaticTile (Tile cg) : this() =>
		(foreground, background, glyph) = (cg.Foreground, cg.Background, cg.Glyph);
	public StaticTile (char c) : this() =>
		(foreground, background, glyph) = (ABGR.White, ABGR.Black, c);

	public StaticTile (char c, uint foreground, uint background) : this() =>
		(this.foreground, this.background, glyph) = (foreground, background, c);
	public StaticTile (char c, string foreground, string background) : this() {
		this.foreground = (uint)typeof(ABGR).GetField(foreground).GetValue(null);
		this.background = (uint)typeof(ABGR).GetField(background).GetValue(null);
		this.glyph = c;
	}
	
	public static implicit operator Tile (StaticTile t) => t.Original;
	public static implicit operator StaticTile (Tile cg) => new StaticTile(cg);
}
public record AlphaTile () : ITile {
	[JsonIgnore]
	public Tile Original => new(foreground, background, glyph);
	[JsonProperty]
	[Opt] public uint foreground;
	[JsonProperty]
	[Opt] public uint background;
	[JsonProperty]
	[Req] public uint glyph;
	[JsonIgnore]
	private byte alpha => (byte)(alphaRange * Math.Sin(ticks * 2 * Math.PI / cycle));
	[JsonIgnore]
	public Tile Glyph => new Tile(
		ABGR.IncA(foreground, alpha),
		ABGR.IncA(background, alpha),
		glyph);
	int cycle;
	int alphaRange;
	int ticks = 0;
	public AlphaTile (Tile cg) : this() =>
		(foreground, background, glyph) = (cg.Foreground, cg.Background, cg.Glyph);
	public void Update () =>
		ticks++;
	public static implicit operator Tile (AlphaTile t) => t.Original;
}