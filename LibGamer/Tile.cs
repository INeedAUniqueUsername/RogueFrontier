using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using TileTuple = (uint Foreground, uint Background, int Glyph);
namespace LibGamer;

public record ABGR(uint packed) {

	public static uint TryAttColor (XElement e, string key, uint fallback) => e.TryAtt(key, out var c) ? Parse(c) : fallback;
	public static uint? TryAttColor (XElement e, string key, uint? fallback) => e.TryAtt(key, out var c) ? Parse(c) : fallback;


	public static uint Parse (string name) =>
		(uint?)(typeof(ABGR).GetField(name)?.GetValue(null))
		?? FromRGBA(uint.Parse(name, System.Globalization.NumberStyles.HexNumber));
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
	public static uint BlendPremultiply (uint back, uint front, byte a = 0xff) {

		var alpha = A(front);
		var inv_alpha = (byte)(255 - alpha);
		return RGBA(
			r: (byte)((alpha * R(front) + inv_alpha * R(back) * A(back) / 255) >> 8),
			g: (byte)((alpha * G(front) + inv_alpha * G(back) * A(back) / 255) >> 8),
			b: (byte)((alpha * B(front) + inv_alpha * B(back) * A(back) / 255) >> 8),
			a: a
			);
	}
	//Essentially the same as blending this color over Color.Black
	public static uint Premultiply (uint abgr) {
		var (a, b, g, r) = Data(abgr);
		return RGBA((byte)(r * a / 255f), (byte)(g * a / 255f), (byte)(b * a / 255f), a);
	}
	//Premultiply and also set the alpha
	public static uint PremultiplySet (ABGR c, byte alpha) =>
		ABGR.RGBA((byte)(c.r * c.a / 255), (byte)(c.g * c.a / 255), (byte)(c.b * c.a / 255), alpha);
	public static uint Blend (uint back, uint front, byte setAlpha = 255) {
		//Background should be premultiplied because we ignore its alpha value
		var alpha = ABGR.A(front);
		var inv_alpha = (byte)(255 - ABGR.A(front));
		return ABGR.RGBA(
			r: (byte)((alpha * ABGR.R(front) + inv_alpha * ABGR.R(back)) >> 8),
			g: (byte)((alpha * ABGR.G(front) + inv_alpha * ABGR.G(back)) >> 8),
			b: (byte)((alpha * ABGR.B(front) + inv_alpha * ABGR.B(back)) >> 8),
			a: setAlpha
			);
	}

	//public static (byte R, byte G, byte B, byte A) RGBA(uint abgr) => (R(abgr), G(abgr), B(abgr), A(abgr));

	public static (byte R, byte G, byte B, byte A) Data (uint abgr) =>
		(A(abgr), G(abgr), B(abgr), R(abgr));

	public static Mut MA (uint abgr) => new(abgr, 0);
	public static byte A (uint abgr) => (byte)((abgr >> 24) & 255);
	public static byte B (uint abgr) => (byte)((abgr >> 16) & 255);
	public static byte G (uint abgr) => (byte)((abgr >> 08) & 255);
	public static byte R (uint abgr) => (byte)((abgr >> 00) & 255);
	public static uint RGB (byte r, byte g, byte b) => RGBA(r, g, b, 255);
	public static uint RGBA (byte r, byte g, byte b, byte a) => (uint)(
	(a << 24) +
	(b << 16) +
	(g << 08) +
	(r << 00)
	);
	public static uint FromRGBA (uint rgba) => (uint)(
		((rgba << 24) & 0xFF000000) +
		((rgba << 08) & 0x00FF0000) +
		((rgba >> 08) & 0x0000FF00) +
		((rgba >> 24) & 0x000000FF)
	);
	public static uint ToRGBA (uint abgr) => (uint)(
		((abgr & 0x000000FF) << 24) +
		((abgr & 0x0000FF00) << 08) +
		((abgr & 0x00FF0000) >> 08) +
		((abgr & 0xFF000000) >> 24)
	);


	public static float GetLightness (uint c) {
		int r = R(c);
		int g = G(c);
		int b = B(c);

		var min = Math.Min(Math.Min(r, g), b);
		var max = Math.Max(Math.Max(r, g), b);
		return (max + min) / 510f;
	}

	public static uint SetLightness (uint c, float brightness) =>
		FromHSL(H(c), S(c), brightness);

	public static float H (uint c) {
		int r = R(c);
		int g = G(c);
		int b = B(c);
		if(r == g && g == b) {
			return 0f;
		}

		var min = Enumerable.Min<int>([r, g, b]);
		var max = Enumerable.Max<int>([r, g, b]);

		float num = max - min;
		float num2 = ((r == max) ? ((float)(g - b) / num) : ((g != max) ? ((float)(r - g) / num + 4f) : ((float)(b - r) / num + 2f)));
		num2 *= 60f;
		if(num2 < 0f) {
			num2 += 360f;
		}

		return num2;
	}
	public static float S(uint c) {

		int r = R(c);
		int g = G(c);
		int b = B(c);
		if(r == g && g == b) {
			return 0f;
		}

		var min = Enumerable.Min<int>([r, g, b]);
		var max = Enumerable.Max<int>([r, g, b]);

		int num = max + min;
		if(num > 255) {
			num = 510 - max - min;
		}
		return (float)(max - min) / (float)num;
	}


	public static uint ToGray (uint c) => FromHSL(0, 0, GetLightness(c));
	public static uint FromHSL (float h, float s, float l) {
		if(h < 0f || h > 360f) {
			throw new ArgumentOutOfRangeException("h");
		}

		double grey = (1f - Math.Abs(2f * l - 1f)) * s;
		double sector = h / 60f;
		double intensity = grey * (1.0 - Math.Abs(sector % 2.0 - 1.0));
		double light = (double)l - 0.5 * grey;
		double red;
		double green;
		double blue;
		if(sector < 1.0) {
			red = grey;
			green = intensity;
			blue = 0.0;
		} else if(sector < 2.0) {
			red = intensity;
			green = grey;
			blue = 0.0;
		} else if(sector < 3.0) {
			red = 0.0;
			green = grey;
			blue = intensity;
		} else if(sector < 4.0) {
			red = 0.0;
			green = intensity;
			blue = grey;
		} else if(sector < 5.0) {
			red = intensity;
			green = 0.0;
			blue = grey;
		} else {
			red = grey;
			green = 0.0;
			blue = intensity;
		}

		byte r = (byte)(255.0 * (red + light));
		byte g = (byte)(255.0 * (green + light));
		byte b = (byte)(255.0 * (blue + light));
		byte alpha = 255;
		return ABGR.RGBA(r, g, b, alpha);
	}

	public static uint IncR (uint abgr, byte inc) => (abgr & 0xFFFFFF00) + (abgr + inc) & ~0xFFFFFF00;
	public static uint IncG (uint abgr, byte inc) => (abgr & 0xFFFF00FF) + (abgr + inc) & ~0xFFFF00FF;
	public static uint IncB (uint abgr, byte inc) => (abgr & 0xFF00FFFF) + (abgr + inc) & ~0xFF00FFFF;
	public static uint IncA (uint abgr, byte inc) => (abgr & 0x00FFFFFF) + (abgr + inc) & 0xFF000000;
	public static uint IncRGB (uint abgr, byte inc) => (byte)(
		(abgr & 0xFF000000) +
		(inc >> 24) +
		(inc >> 16) +
		(inc >> 08)
		);
	public static uint SetR (uint abgr, byte r) => (abgr & 0xFFFFFF00) + r >> 24;
	public static uint SetG (uint abgr, byte g) => (abgr & 0xFFFF00FF) + g >> 16;
	public static uint SetB (uint abgr, byte b) => (abgr & 0xFF00FFFF) + b >> 8;
	public static uint SetA (uint abgr, byte a) => (abgr & 0x00FFFFFF) + (uint)(a << 24);
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

	public string FrontStr => Foreground.ToString("X");
	public string BackStr => Foreground.ToString("X");

	public static implicit operator Tile (TileTuple t) => new(t.Foreground, t.Background, t.Glyph);
	public static Tile From(XElement e) {
		var f = ABGR.Parse(e.TryAtt(["f", "foreground"], "White"));
		var b = ABGR.Parse(e.TryAtt(["b", "background"], "Black"));
		var g = e.TryAtt(["g", "glyph"], out var _g) ? char.Parse(_g) : throw new Exception();
		return new Tile(f, b, g);
	}
	public static Tile[] ArrFrom(XElement element, uint df = ABGR.White, uint db = ABGR.Black) {
		var f = ABGR.TryAttColor(element, "f", df);
		var b = ABGR.TryAttColor(element, "b", db);
		return [.. GetParts().SelectMany(a => a)];
		IEnumerable<IEnumerable<Tile>> GetParts () {
			foreach(var node in element.Nodes()) {
				switch(node) {
					case XText t:
						yield return t.Value.Select(c => new Tile(f, b, c));
						break;
					case XElement e:
						yield return ArrFrom(e, f, b);
						break;

				}
			}
		}
	}



	public Tile Gray =>
		new(ABGR.ToGray(Foreground), ABGR.ToGray(Background), Glyph);







	public static Tile empty { get; } = new(0, 0, 0);
	public Tile () : this(0, 0, 0) { }
	public Tile (uint Foreground, uint Background, int Glyph) : this(Foreground, Background, (uint)Glyph) { }
	public static Tile[] Arr (string str, uint Foreground = ABGR.White, uint Background = ABGR.Black) => [.. str.Select(c => new Tile(Foreground, Background, c))];
	public static IEnumerable<Tile> WithA (IEnumerable<Tile> str, byte front, byte back) =>
		from t in str select t with {
			Foreground = ABGR.SetA(t.Foreground, front),
			Background = ABGR.SetA(t.Background, back)
		};
	public Tile PremultiplySet (byte alpha) {
		if(alpha == 255) {
			return this;
		}
		return new(ABGR.PremultiplySet(Foreground, alpha), ABGR.PremultiplySet(Background, alpha), Glyph);
	}
	public Tile SetA (byte alpha) {
		if(alpha == 255) {
			return this;
		}
		return new(ABGR.SetA(Foreground, alpha), ABGR.SetA(Background, alpha), Glyph);
	}
	public bool IsVisible => Glyph != ' ' || Background != ABGR.Transparent;
}
public record TileArr (Tile[] arr) {
	public static implicit operator TileArr ((string str, uint Foreground, uint Background) t) => new(Tile.Arr(t.str, t.Foreground, t.Background));

	public static implicit operator Tile[] (TileArr t) => t.arr;
}

public interface ITile {
	public Tile Original { get; }
	void Update () { }


}
public record StaticTile () : ITile {
	[JsonIgnore]
	public Tile Original => new(foreground, background, glyph);
	[JsonProperty]
	[Opt(parse = false)] public uint foreground;
	[JsonProperty]
	[Opt(parse =false)] public uint background;
	[JsonProperty]
	[Req(parse = false)] public uint glyph;
	public StaticTile (XElement e) : this() {
		e.Initialize(this, transform:new() {
			[nameof(foreground)] = (string s) => ABGR.Parse(s),
			[nameof(background)] = (string s) => ABGR.Parse(s),
			[nameof(glyph)] = (string s) => (uint)(s.Length == 1 ? s[0] : uint.Parse(s[1..])),
		});
	}
	public StaticTile (Tile cg) : this() =>
		(foreground, background, glyph) = (cg.Foreground, cg.Background, cg.Glyph);
	public StaticTile (char c) : this() =>
		(foreground, background, glyph) = (ABGR.White, ABGR.Black, c);

	public StaticTile (char c, uint foreground, uint background) : this() =>
		(this.foreground, this.background, glyph) = (foreground, background, c);
	public StaticTile (char c, string foreground, string background) : this() {
		this.foreground = ABGR.Parse(foreground);
		this.background = ABGR.Parse(background);
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
