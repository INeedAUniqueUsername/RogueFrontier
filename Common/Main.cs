using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using static SadConsole.ColoredString;
using SadConsole.UI;
using SadConsole.Input;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Data;
using NCalc;
using Con = SadConsole.Console;
using static SFML.Window.Keyboard;
using ASECII;
using Namotion.Reflection;
using ArchConsole;
using Col = SadRogue.Primitives.Color;
using System.Numerics;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using RogueFrontier;
using SadRogue.Primitives.SpatialMaps;
using SFML.Audio;
using SadConsole.SplashScreens;
using SadConsole.EasingFunctions;
using System.Collections.ObjectModel;
using GamerLib;

namespace Common;
public static class Main {
    public static ColoredString LerpString(this ColoredString str, double x, double fromMin, double fromMax, double gamma) =>
        str.SubString(0, (int)Lerp(x, fromMin, fromMax, 0, str.Length, gamma));
    public static ColoredString ConcatColored(ColoredString[] parts) {
        var r = new List<ColoredGlyph>();
        foreach(var cs in parts) {
            r.AddRange(cs);
        }
        return new(r.ToArray());
    }
    public static string Repeat(this string str, int times) =>
        string.Join("", Enumerable.Range(0, times).Select(i => str));
    public static ColoredString Concat(params (string str, Col foreground, Col background)[] parts) =>
        new(parts.SelectMany(part => new ColoredString(part.str, part.foreground, part.background)).ToArray());
    public static void Replace(this ScreenSurface c, ScreenSurface next) {
        var p = c.Parent;
        p.Children.Remove(c);
        p.Children.Add(next);
        p.IsFocused = true;
    }
    public static string ExpectFile(string path) =>
         (File.Exists(path)) ? path :
            throw new Exception($"File {path} does not exist");
    public static bool TryFile(string path, out string file) =>
        (file = File.Exists(path) ? path : null) != null;
    public static T GetRandom<T>(this IEnumerable<T> e, Rand r) =>
        e.ElementAt(r.NextInteger(e.Count()));
    public static T GetRandomOrDefault<T>(this IEnumerable<T> e, Rand r) =>
        e.Any() ? e.ElementAt(r.NextInteger(e.Count())) : default(T);
    

	public static bool AreKeysPressed(this Keyboard keyboard, params Keys[] keys) =>
        keys.All(keyboard.IsKeyPressed);
    
	public static Col TryAttColor (this XElement e, string attribute, Col fallback) {
		if(e.TryAtt(attribute, out string s)) {
			if(int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
				return new Col((packed >> 24) & 0xFF, (packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
			} else try {
					var f = typeof(Col).GetField(s);
					return (Col)(f?.GetValue(null) ?? throw e.Invalid<Col>(attribute));
				} catch {
					throw e.Invalid<Col>(attribute);
				}
		} else {
			return fallback;
		}
	}
	public static Col ExpectAttColor (this XElement e, string key) {
		if(e.TryAtt(key, out string s)) {
			if(int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
				return new Col((packed >> 24) & 0xFF, (packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
			} else try {
					return (Col)typeof(Col).GetField(s).GetValue(null);
				} catch {
					throw e.Invalid<Col>(key);
				}
		} else {
			throw e.Missing<Col>(key);
		}
	}

	public static IDice ExpectAttDice (this XElement e, string key) =>
		e.Attribute(key) is XAttribute a ?
			ExpectAttributeDice(a) :
			throw new Exception($"<{e.Name}> requires dice range attribute: {key} ### {e} ### {e.Parent}");

	public static IDice ExpectAttributeDice (this XAttribute a) =>
		IDice.Parse(a.Value) ??
			throw new Exception($"int value / equation expected: {a.Name} = \"{a.Value}\"");

	public static Col NextGray(this Random r, int range) {
        var value = r.Next(range);
        return new (value, value, value);
    }
    public static Col Noise(this Col c, Random r, double range) {
        double increaseFactor = r.NextDouble() * range;
        double multiplier = 1 + increaseFactor;
        return new ((int)Math.Min(255, c.R * multiplier), (int)Math.Min(255, c.G * multiplier), (int)Math.Min(255, c.B * multiplier));
    }
    public static Col NextColor(this Random r, int range) => 
        new (r.Next(range), r.Next(range), r.Next(range));
    public static Col Round(this Col c, int factor) => 
        new (factor * (c.R / factor), factor * (c.G / factor), factor * (c.B / factor));
    //public static Color Add(this Color c, int value) => c.Add(new Color(value, value, value));
    public static Col Add(this Col c1, int r = 0, int g = 0, int b = 0) => 
        new (Math.Min(255, c1.R + r), Math.Min(255, c1.G + g), Math.Min(255, c1.B + b));
    public static Col Add(this Col c1, Col c2) => 
        new (Math.Min(255, c1.R + c2.R), Math.Min(255, c1.G + c2.G), Math.Min(255, c1.B + c2.B));
    public static Col Subtract(this Col c, int value) => 
        c.Subtract(new Col(value, value, value));
    public static Col Subtract(this Col c1, Col c2) =>
        new (Math.Max(0, c1.R - c2.R), Math.Max(0, c1.G - c2.G), Math.Max(0, c1.B - c2.B));
    public static Col Divide(this Col c, int scale) =>
        new (c.R / scale, c.G / scale, c.B / scale);
    public static Col Multiply(this Col c, double r = 1, double g = 1, double b = 1, double a = 1) =>
        new ((int)(c.R * r), (int)(c.G * g), (int)(c.B * b), (int)(c.A * a));
    public static Col Divide(this Col c, double scale) =>
        new ((int)(c.R / scale), (int)(c.G / scale), (int)(c.B / scale));
    public static Col Clamp(this Col c, int max) =>
        new (Math.Min(c.R, max), Math.Min(c.G, max), Math.Min(c.B, max));
    public static Col Gray(int value) => 
        new (value, value, value, 255);
    public static Col Gray(this Col c) =>
        SadRogue.Primitives.Color.FromHSL(0, 0, c.GetBrightness());
    public static ColoredGlyph Gray(this ColoredGlyph cg) =>
        new (cg.Foreground.Gray(), cg.Background.Gray(), cg.Glyph);
    public static Col WithValues(this Col c, int? red = null, int? green = null, int? blue = null, int? alpha = null) =>
        new(red ?? c.R, green ?? c.G, blue ?? c.B, alpha ?? c.A);
    
    public static Col SetBrightness(this Col c, float brightness) =>
        SadRogue.Primitives.Color.FromHSL(c.GetHue(), c.GetSaturation(), brightness);
    public static double CalcFireAngle(XY posDiff, XY velDiff, double missileSpeed, out double timeToHit) {
        /*
        var timeToHit = posDiff.Magnitude / missileSpeed;
        var posFuture = posDiff + velDiff * timeToHit;

        var posDiffPrev = posDiff;
        posDiff = posFuture;
        */
        timeToHit = posDiff.magnitude / missileSpeed;
        XY posDiffNext;
        double timeToHitPrev;
        int i = 10;
        do {
            posDiffNext = posDiff + velDiff * timeToHit;
            timeToHitPrev = timeToHit;
            timeToHit = posDiffNext.magnitude / missileSpeed;
        } while (Math.Abs(timeToHit - timeToHitPrev) > 0.1 && i-- > 0);

        return posDiffNext.angleRad;
        /*
        var a = velDiff.Dot(velDiff) - missileSpeed * missileSpeed;
        var b = 2 * velDiff.Dot(posDiff);
        var c = posDiff.Dot(posDiff);

        var p = -b / (2 * a);
        var q = Math.Sqrt(b * b - 4 * a * c) / (2 * a);
        var t1 = -p - q;
        var t2 = p + q;

        timeToHit = t1;
        if(t1 > t2 && t2 > 0) {
            timeToHit = t2;
        }
        var posFuture = posDiff + velDiff * timeToHit;
        return posFuture.Angle;
        */
    }

    /*
    public static bool InRange(double n, double min, double max) {
        return n > min && n < max;
    }
    */
    public static int LineLength(this string lines) =>
        lines.IndexOf('\n');
    public static int LineCount(this string lines) =>
        lines.Split('\n').Length;


    public class RectOptions {
        public bool connectBelow, connectAbove;
        public Line width = Line.Single;
        public Col f = Col.White, b = Col.Black;
    }

    public static void DrawRect(this ICellSurface surf, int xStart, int yStart, int dx, int dy, RectOptions op) {
        char Box(Line n = Line.None, Line e = Line.None, Line s = Line.None, Line w = Line.None) =>
            (char)BoxInfo.IBMCGA.glyphFromInfo[new(n, e, s, w)];

        var width = op.width;
        var aboveWidth = op.connectAbove ? width : Line.None;
        var belowWidth = op.connectBelow ? width : Line.None;
        IEnumerable<string> GetLines() {

            var vert = Box(n: width, s: width);
            var hori = Box(e: width, w: width);

            if (dx == 1) {
                var n = Box(e: Line.Single, w: Line.Single, s: width, n: aboveWidth);
                var s = Box(e: Line.Single, w: Line.Single, n: width, s: belowWidth);

                yield return $"{n}";
                for (int i = 0; i < dy - 2; i++) {
                    yield return $"{vert}";
                }
                yield return $"{s}";
                yield break;
            } else if(dy == 1) {
                var e = Box(n: aboveWidth, s: belowWidth, w: width);
                var w = Box(n: aboveWidth, s: belowWidth, e: width);

                yield return $"{w}{new string(hori, dx - 2)}{e}";
                yield break;
            } else {
                var nw = Box(e: width, s: width, n: aboveWidth);
                var ne = Box(w: width, s: width, n: aboveWidth);
                var sw = Box(e: width, n: width, s: belowWidth);
                var se = Box(w: width, n: width, s: belowWidth);
                yield return $"{nw}{new string(hori, dx - 2)}{ne}";
                for (int i = 0; i < dy - 2; i++) {
                    yield return $"{vert}{new string(' ', dx - 2)}{vert}";
                }
                yield return $"{sw}{new string(hori, dx - 2)}{se}";
            }
        }
        int y = yStart;
        foreach(var line in GetLines()) {
            surf.Print(xStart, y++, new ColoredString(line, op.f, op.b));
        }
    }
    public static T LastItem<T>(this List<T> list) => list[list.Count - 1];
    public static T FirstItem<T>(this List<T> list) => list[0];
    public static string FlipLines(this string s) {
        var lines = new List<string>(s.Split('\n'));
        lines.Reverse();
        var result = new StringBuilder(s.Length - s.LineCount());
        for (int i = 0; i < lines.Count - 1; i++) {
            result.AppendLine(lines[i]);
        }
        result.Append(lines.LastItem());
        return result.ToString();
    }
    public static void PrintLines(this Con console, int x, int y, string lines, Col? foreground = null, Col? background = null, Mirror mirror = Mirror.None) {
        foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
            console.Print(x, y, line, foreground ?? SadRogue.Primitives.Color.White, background ?? SadRogue.Primitives.Color.Black, mirror);
            y++;
        }
    }

	public static void PaintCentered (this Window w, string s, int x, int y) =>
	w.Print(x - s.Length / 2, y, s);

	public static List<XYZ> GetWithin(int radius) {
        var result = new List<XYZ>();
        for (int i = 0; i < radius; i++) {
            result.AddRange(GetSurrounding(i));
        }
        result = new(result.Distinct(new XYZGridComparer()).Where(p => p.Magnitude2 < radius * radius));
        return result;
    }
    //This function calculates all the points on a hollow cube of given radius around an origin of (0, 0, 0)
    public static List<XYZ> GetSurrounding(int radius) {
        //Cover all the corners
        var result = new List<XYZ>() {
                new XYZ( radius,  radius,  radius),	//NE Upper
				new XYZ( radius,  radius, -radius),	//NE Lower
				new XYZ( radius, -radius,  radius),	//SE Upper
				new XYZ( radius, -radius, -radius),	//SE Lower
				new XYZ(-radius,  radius,  radius),	//NW Uper
				new XYZ(-radius,  radius, -radius),	//NW Lower
				new XYZ(-radius, -radius,  radius),	//SW Upper
				new XYZ(-radius, -radius, -radius),	//SW Lower
			};
        //Fill in the sides of the cube
        for (int y = -radius + 1; y < radius; y++) {
            for (int z = -radius + 1; z < radius; z++) {
                result.Add(new XYZ(radius, y, z));  //East side
                result.Add(new XYZ(-radius, y, z)); //West side
            }
            //Add the top/bottom edges of each side
            result.Add(new XYZ(radius, y, radius));
            result.Add(new XYZ(radius, y, -radius));
            result.Add(new XYZ(-radius, y, radius));
            result.Add(new XYZ(-radius, y, -radius));
        }
        for (int x = -radius + 1; x < radius; x++) {
            for (int z = -radius + 1; z < radius; z++) {
                result.Add(new XYZ(x, radius, z));   //North side
                result.Add(new XYZ(x, -radius, z));   //South side
            }

            result.Add(new XYZ(x, radius, radius)); //North upper
            result.Add(new XYZ(x, radius, -radius));    //North lower
            result.Add(new XYZ(x, -radius, radius));    //South upper
            result.Add(new XYZ(x, -radius, -radius));//South lower
        }
        for (int x = -radius + 1; x < radius; x++) {
            for (int y = -radius + 1; y < radius; y++) {
                result.Add(new XYZ(x, y, radius));   //Top side
                result.Add(new XYZ(x, y, -radius));   //Bottom side
            }
        }
        //Vertical
        for (int z = -radius + 1; z < radius; z++) {
            result.Add(new XYZ(radius, radius, z));
            result.Add(new XYZ(radius, -radius, z));
            result.Add(new XYZ(-radius, radius, z));
            result.Add(new XYZ(-radius, -radius, z));
        }
        //Sort them based on distance from center
        result.Sort((p1, p2) => {
            double d1 = (p1).Magnitude;
            double d2 = (p2).Magnitude;
            if (d1 < d2) {
                return -1;
            } else if (d1 > d2) {
                return 1;
            } else {
                return 0;
            }
        });
        return result;
    }
    public static int Amplitude(this Random random, int amplitude) => random.Next(-amplitude, amplitude);

    public static List<ColoredString> SplitLine(this ColoredString s, int width) {
        var result = new List<ColoredString>();
        var column = 0;
        var line = new List<ColoredGlyph>();
        var word = new List<ColoredGlyph>();

        void AddWord() {
            line.AddRange(word);
            word.Clear();
        }
        void AddLine() {
            result.Add(new(line.ToArray()));
            line.Clear();
            column = 0;
        }
        foreach (var c in s) {
            if (column < width) {
                var g = c.Glyph;
                if (g == ' ') {
                    AddWord();
                    line.Append(c);
                    column++;
                } else if (g == '\n') {
                    AddWord();
                    column = 0;
                } else if (g == '-') {
                    word.Append(c);
                    AddWord();
                } else {
                    word.Append(c);
                    column++;
                }
            } else {
                word.Append(c);
                AddLine();
            }
        }
        line.AddRange(word);
        if (line.Count > 0) {
            AddLine();
        }

        return result;
    }
    public static void InheritAttributes(this XElement sub, XElement source) {
        foreach (var att in source.Attributes()) {
            if (sub.Attribute(att.Name) == null) {
                sub.SetAttributeValue(att.Name, att.Value);
            }
        }
    }
    public static void PrintCenter(this ScreenSurface c, int y, string s) =>
        c.Surface.Print(c.Surface.Width / 2 - s.Length / 2, y, s);
    public static void PrintCenter(this ScreenSurface c, int y, ColoredString s) =>
        c.Surface.Print(c.Surface.Width / 2 - s.Length / 2, y, s);
    
    /// <summary>Provides intermediate format for <see cref="SoundBuffer"/> which requires IntPtr at runtime</summary>
	public class SoundBufferPort : XPort<SoundBuffer> {
        private record Data(short[] Samples, uint ChannelCount, uint SampleRate);
        /// <inheritdoc/>
		public static SoundBuffer LoadObject(XLoad ctx, XElement source) =>
			ctx.DeserializeValue<Data>(source).CrossConstruct<SoundBuffer>();
        /// <inheritdoc/>
		public static XElement SaveObject(XSave ctx, SoundBuffer source) =>
            ctx.Serialize(source.CrossConstruct<Data>());
	}
	
    public static int CalcAccuracy(int difficulty, int skill, Random karma) {
        if (skill > difficulty) {
            return 100;
        } else {
            var miss = difficulty - skill;
            return 100 - karma.Next(miss);
        }
    }
    //Chance that the shot is blocked by an obstacle
    public static bool CalcBlocked(int coverage, int accuracy, Random karma) =>
        karma.Next(coverage) > karma.Next(accuracy);
    public static ColoredGlyph Colored(char c, Col? f = null, Col? b = null) =>
        new(f ?? SadRogue.Primitives.Color.White, b?? SadRogue.Primitives.Color.Black, c);
    public static ColoredString WithBackground(this ColoredString c, Col? Background = null) {
        var result = c.SubString(0, c.Count());
        result.SetBackground(Background ?? SadRogue.Primitives.Color.Black);
        return result;
    }
    public static ColoredString Adjust(this ColoredString c, Col foregroundInc) {
        var result = c.SubString(0, c.Count());
        foreach (var g in result) {
            g.Foreground = Sum(g.Foreground, foregroundInc);
        }
        return result;
    }
    public static ColoredString WithOpacity(this ColoredString s, byte front, byte back = 255) {
        s = s.Clone();
        foreach (var c in s) {
            c.Foreground = c.Foreground.SetAlpha(front);
            c.Background = c.Background.SetAlpha(back);
        }
        return s;
    }
    public static ColoredString Brighten(this ColoredString s, int intensity) {
        return s.Adjust(new(intensity, intensity, intensity, 0));
    }
    public static ColoredGlyphAndEffect ToEffect(this ColoredGlyph cg) {
        return new() {
            Foreground = cg.Foreground,
            Background = cg.Background,
            Glyph = cg.Glyph
        };
    }
    public static ColoredString ToColoredString(this string s) =>
        s.Color();
    public static ColoredString Color(this string s, Col? f = null, Col? b = null) =>
        new(s, f ?? SadRogue.Primitives.Color.White, b ?? SadRogue.Primitives.Color.Black);
    public static ColoredString ToColoredString(this ColoredGlyph c) =>
        new(c.ToEffect());


    public static ColoredGlyph Brighten(this ColoredGlyph c, int intensity) {
        var result = new ColoredGlyph(c.Foreground, c.Background, c.Glyph);
        result.Foreground = Sum(result.Foreground, new Col(intensity, intensity, intensity, 0));
        return result;
    }
    public static ColoredString Adjust(this ColoredString c, Col foregroundInc, Col backgroundInc) {
        var result = c.SubString(0, c.Count());
        foreach (var g in result) {
            g.Foreground = Sum(g.Foreground, foregroundInc);
            g.Background = Sum(g.Background, backgroundInc);
        }
        return result;
    }
    public static ColoredGlyph Adjust(this ColoredGlyph c, Col foregroundInc) {
        var result = new ColoredGlyph(c.Foreground, c.Background, c.Glyph);
        result.Foreground = Sum(result.Foreground, foregroundInc);
        return result;
    }
    public static Col Sum(Col c, Col c2) =>
        new(Range(0, 255, c.R + c2.R), Range(0, 255, c.G + c2.G), Range(0, 255, c.B + c2.B), Range(0, 255, c.A + c2.A));
    
    //Essentially the same as blending this color over Color.Black
    public static Col Premultiply(this Col c) => new((c.R * c.A) / 255, (c.G * c.A) / 255, (c.B * c.A) / 255, c.A);
    //Premultiply and also set the alpha
    public static Col PremultiplySet(this Col c, int alpha) => new((c.R * c.A) / 255, (c.G * c.A) / 255, (c.B * c.A) / 255, alpha);

    //Premultiplies this color and the blends another color over it
    public static Col BlendPremultiply(this Col background, Col foreground, byte setAlpha = 0xff) {

        var alpha = (byte)(foreground.A);
        var inv_alpha = (byte)(255 - foreground.A);
        return new(
            r: (byte)((alpha * foreground.R + inv_alpha * background.R * background.A / 255) >> 8),
            g: (byte)((alpha * foreground.G + inv_alpha * background.G * background.A / 255) >> 8),
            b: (byte)((alpha * foreground.B + inv_alpha * background.B * background.A / 255) >> 8),
            alpha: setAlpha
            );
    }

    //https://stackoverflow.com/a/12016968
    //Blend another color over this color
    public static Col Blend(this Col background, Col foreground, byte setAlpha = 0xff) {
        //Background should be premultiplied because we ignore its alpha value
        var alpha = (byte)(foreground.A);
        var inv_alpha = (byte)(255 - foreground.A);
        return new(
            r: (byte)((alpha * foreground.R + inv_alpha * background.R) >> 8),
            g: (byte)((alpha * foreground.G + inv_alpha * background.G) >> 8),
            b: (byte)((alpha * foreground.B + inv_alpha * background.B) >> 8),
            alpha: setAlpha
            );
    }
    public static ColoredGlyph Blend(this ColoredGlyph back, ColoredGlyph front) {
        var d = new List<CellDecorator>();
        var f = back.Foreground;
        var b = back.Background;
        int g = back.Glyph;

        if (front.Glyph != 0 && front.Glyph != ' ' && front.Foreground.A != 0) {
            d.Add(new(f, g, Mirror.None));

            f = front.Foreground;
            g = front.Glyph;
        }
        b = b.Premultiply().Blend(front.Background);

        return new(f, b, g) { Decorators = d.ToList() };
    }

    public static ColoredGlyph PremultiplySet(this ColoredGlyph cg, int alpha) {
        if (alpha == 255) {
            return cg;
        }
        return new(cg.Foreground.PremultiplySet(alpha), cg.Background.PremultiplySet(alpha), cg.Glyph);
    }
}
/// <summary>
/// Helpers for ColoredString
/// </summary>
public static class ColorCommand {
    /*
    public static string Substring(string s, int start, int count) {
        int index = start;
        int remaining = count;
        StringBuilder result = new();
        if (Regex.Match(s.Substring(index), "(?<command>\\[c:.+\\])") is Match {Success:true }m) {
            var c = m.Groups["command"].Value;
            result.Append(c);
            index += c.Length;
        }
    }
    */
    public static string Unparse(Col c) =>
        $"{c.R},{c.G},{c.B},{c.A}";
    public static string Front(Col f) => $"[c:r f:{Unparse(f)}]";
    public static string Front(Col f, string str) => $"{Front(f)}{str}[c:u]";
    public static string Back(Col b) => $"[c:r b:{Unparse(b)}]";
    public static string Back(Col b, string str) => $"{Back(b)}{str}[c:u]";
    public static string Recolor(Col? f, Col? b) {
        var result = new StringBuilder();
        if(f.HasValue)
            result.Append(Front(f.Value));
        if (b.HasValue)
            result.Append(Back(b.Value));
        return result.ToString();
    }
    public static string Recolor(Col? f, Col? b, string str) {
        var result = new StringBuilder();
        result.Append(Recolor(f, b));
        result.Append(str);
        if (f.HasValue)
            result.Append(Undo());
        if (b.HasValue)
            result.Append(Undo());
        return result.ToString();
    }
    public static string Undo() => "[c:u]";
    public static string Repeat(string s, int n) => string.Join("", Enumerable.Range(0, n).Select(i => s));
}