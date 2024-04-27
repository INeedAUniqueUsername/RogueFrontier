using SadConsole;
using Color = SadRogue.Primitives.Color;
using System.Drawing;
using System.Runtime.Serialization;

namespace ASECII;

public interface TileRef {
	Color Foreground { get; }
	Color Background { get; }
	int Glyph { get; }
	ColoredGlyph cg { get; }
}
[DataContract]
public class TileValue : TileRef {
	[DataMember]
	public Color Foreground { get; set; }
	[DataMember]
	public Color Background { get; set; }
	[DataMember]
	public int Glyph { get; set; }
	[IgnoreDataMember]
	public ColoredGlyph cg => new ColoredGlyph(Foreground, Background, Glyph);
	public TileValue (Color Foreground, Color Background, int Glyph) {
		this.Foreground = Foreground;
		this.Background = Background;
		this.Glyph = Glyph;
	}
	public static implicit operator ColoredGlyph (TileValue tv) => new ColoredGlyph(tv.Foreground, tv.Background, tv.Glyph);
}

