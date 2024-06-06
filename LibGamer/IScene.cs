using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace LibGamer;
using TileTuple = (uint Foreground, uint Background, int Glyph);

using XYI = (int X, int Y);
public static class SScene {
	public static Dictionary<XYI, U> Normalize<U> (this Dictionary<XYI, U> d) {
		int left = int.MaxValue;
		int top = int.MaxValue;
		foreach(XYI p in d.Keys) {
			left = Math.Min(left, p.X);
			top = Math.Min(top, p.Y);
		}
		return d.Translate((-left, -top));
	}
	public static Dictionary<XYI, TileTuple> LoadImage (string file) =>
		ImageLoader.DeserializeObject<Dictionary<XYI, TileTuple>>(File.ReadAllText(file));
	public static Dictionary<XYI, Tile> ToImage (this string[] image, uint tint) {
		var result = new Dictionary<XYI, Tile>();
		for(int y = 0; y < image.Length; y++) {
			var line = image[y];
			for(int x = 0; x < line.Length; x++) {
				result[(x, y * 2)] = new(tint, ABGR.Black, line[x]);
				result[(x, y * 2 + 1)] = new(tint, ABGR.Black, line[x]);
			}
		}
		return result;
	}
	public static Dictionary<XYI, U> Translate<U> (this Dictionary<XYI, U> image, (int X, int Y) translate) {
		var result = new Dictionary<XYI, U>();
		foreach(((var x, var y), var u) in image) {
			result[(x + translate.X, y + translate.Y)] = u;
		}
		return result;
	}
	public static Dictionary<XYI, U> CenterVertical<U> (this Dictionary<XYI, U> image, Sf c, int deltaX = 0) {
		var result = new Dictionary<XYI, U>();
		int deltaY = (c.Height - (image.Max(pair => pair.Key.Y) - image.Min(pair => pair.Key.Y))) / 2;
		foreach(((var x, var y), var u) in image) {
			result[(x + deltaX, y + deltaY)] = u;
		}
		return result;
	}
	public static Dictionary<XYI, U> Flatten<U> (params Dictionary<XYI, U>[] images) {
		var result = new Dictionary<XYI, U>();
		foreach(var image in images) {
			foreach(((var x, var y), var u) in image) {
				result[(x, y)] = u;
			}
		}
		return result;
	}
}
public interface IScene {
	//IProgram Program { get; set; }
	Action<IScene> Go { get; set; }
	Action<Sf> Draw { get; set; }
	Action<SoundCtx> PlaySound { get; set; }
	public void Show () => Go(this);
	public void Close () => Go(null);
	public void Update (TimeSpan delta) { }
	public void Render (TimeSpan delta) { }
	public void HandleKey (KB kb) { }
	public void HandleMouse (HandState mouse) { }
}
