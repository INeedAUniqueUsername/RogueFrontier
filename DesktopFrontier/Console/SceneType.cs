using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using Console = SadConsole.Console;
using ArchConsole;
using ASECII;
using System.IO;
using SFML.Audio;
using LibGamer;
namespace RogueFrontier;


public static partial class SScene {
    public static Dictionary<(int, int), U> Normalize<U>(this Dictionary<(int, int), U> d) {
        int left = int.MaxValue;
        int top = int.MaxValue;
        foreach ((int x, int y) p in d.Keys) {
            left = Math.Min(left, p.x);
            top = Math.Min(top, p.y);
        }
        return d.Translate(new Point(-left, -top));
    }
    public static Dictionary<(int, int), ColoredGlyph> LoadImage(string file) {
        var img = ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(file));

        var result = new Dictionary<(int, int), ColoredGlyph>();
        foreach ((var p, var t) in img) {
            result[p] = t.cg;
        }
        return result;
    }
    public static Dictionary<(int, int), ColoredGlyph> ToImage(this string[] image, Color tint) {
        var result = new Dictionary<(int, int), ColoredGlyph>();
        for (int y = 0; y < image.Length; y++) {
            var line = image[y];
            for (int x = 0; x < line.Length; x++) {
                result[(x, y * 2)] = new ColoredGlyph(tint, Color.Black, line[x]);
                result[(x, y * 2 + 1)] = new ColoredGlyph(tint, Color.Black, line[x]);
            }
        }
        return result;
    }
    public static Dictionary<(int, int), U> Translate<U>(this Dictionary<(int, int), U> image, Point translate) {
        var result = new Dictionary<(int, int), U>();
        foreach (((var x, var y), var u) in image) {
            result[(x + translate.X, y + translate.Y)] = u;
        }
        return result;
    }
    public static Dictionary<(int, int), U> CenterVertical<U>(this Dictionary<(int, int), U> image, Console c, int deltaX = 0) {
        var result = new Dictionary<(int, int), U>();
        int deltaY = (c.Height - (image.Max(pair => pair.Key.Item2) - image.Min(pair => pair.Key.Item2))) / 2;
        foreach (((var x, var y), var u) in image) {
            result[(x + deltaX, y + deltaY)] = u;
        }
        return result;
    }
    public static Dictionary<(int, int), U> Flatten<U>(params Dictionary<(int, int), U>[] images) {
        var result = new Dictionary<(int x, int y), U>();
        foreach (var image in images) {
            foreach (((var x, var y), var u) in image) {
                result[(x, y)] = u;
            }
        }
        return result;
    }
    public static void ProcessMouseTree(this IScreenObject root, Mouse m) {
        List<IScreenObject> s = new List<IScreenObject>();
        AddChildren(root);
        void AddChildren(IScreenObject parent) {
            s.Add(parent);
            foreach (var c in parent.Children) {
                AddChildren(c);
            }
        }
        foreach (var c in s) {
            c.ProcessMouse(new MouseScreenObjectState(c, m));
        }
    }
}
public class HeroImageDisplay : Console {
    double time;
    string[] heroImage;
    Color tint;
    public HeroImageDisplay(Console prev, string[] heroImage, Color tint) : base(prev.Width, prev.Height) {
        this.heroImage = heroImage;
        this.tint = tint;
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    public override void Render(TimeSpan delta) {
        int width = heroImage.Max(line => line.Length);
        int height = heroImage.Length;
        int x = 8;
        int y = (Height - height * 2) / 2;
        byte GetAlpha(int x, int y) {
            return (byte)(Math.Sin(time * 1.5 + Math.Sin(x) * 5 + Math.Sin(y) * 5) * 25 + 230);
        }
        int lineY = 0;
        foreach (var line in heroImage) {
            void DrawLine() {
                for (int lineX = 0; lineX < line.Length; lineX++) {
                    var color = tint.SetAlpha(GetAlpha(lineX, lineY));
                    this.SetCellAppearance(x + lineX, y + lineY, new ColoredGlyph(color, Color.Black, line[lineX]));
                }
                lineY++;
            }
            DrawLine();
            DrawLine();
        }
        base.Render(delta);
    }
}