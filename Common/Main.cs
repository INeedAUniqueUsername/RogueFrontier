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

namespace Common;
public static class Main {
    public static double Lerp(double x, double fromMin, double fromMax, double toMin, double toMax, double pow) {
        var fromRange = fromMax - fromMin;
        var toRange = toMax - toMin;
        
        var toDist = (Math.Clamp(x, fromMin, fromMax) - fromMin) * toRange / fromRange;
        toDist = Math.Pow(toDist / toRange, pow);
        return toMin + toRange * toDist;
    }
    public static string LerpString(this string str, double x, double fromMin, double fromMax, double pow) =>
        str.Substring(0, (int)Main.Lerp(x, fromMin, fromMax, 0, str.Length, pow));
    public static ColoredString LerpString(this ColoredString str, double x, double fromMin, double fromMax, double gamma) =>
        str.SubString(0, (int)Main.Lerp(x, fromMin, fromMax, 0, str.Length, gamma));
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
    public static SetDict<(int, int), T> Downsample<T>(this Dictionary<(int, int), T> from, double scale, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var i) in from) {
            result.Add(((offset + p) / scale).roundDown, i);
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            result.AddRange(((offset + p) / scale).roundDown, items);
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, Func<T, bool> filter, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            var i = items.Where(filter);
            if (i.Any()) {
                result.AddRange(((offset + p) / scale).roundDown, i);
            }
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, Func<T, bool> filter, XY offset = null, Predicate<(int, int)> posFilter = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            var scaled = (((XY)p + offset) / scale).roundDown;
            if (posFilter(scaled)) {
                var i = items.Where(filter);
                if (i.Any()) {
                    result.AddRange(scaled, i);
                }
            }
            
        }
        return result;
    }
    public static double step(double from, double to) {
        double difference = to - from;
        return (Math.Abs(difference) > 1) ?
            Math.Sign(difference) :
            difference;
    }
    public static bool CalcAim(XYZ difference, double speed, out double lower, out double higher) {
        double horizontal = difference.xy.magnitude;
        double vertical = difference.z;
        const double g = 9.8;
        double part1 = speed * speed;
        double part2 = Math.Sqrt(Math.Pow(speed, 4) - g * ((g * horizontal * horizontal) + (2 * vertical * speed * speed)));

        if (double.IsNaN(part2)) {
            lower = higher = 0;
            return false;
        } else {
            lower = Math.Atan2(part1 - part2, (g * horizontal));
            higher = Math.Atan2(part1 + part2, (g * horizontal));
            return true;
        }
    }
    public static bool CalcAim2(XYZ difference, double speed, out XYZ lower, out XYZ higher) {
        if (CalcAim(difference, speed, out var lowerAltitude, out var upperAltitude)) {
            double azimuth = difference.xy.angleRad;
            lower = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(lowerAltitude));
            higher = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(upperAltitude));
            return true;
        } else {
            lower = higher = null;
            return false;
        }
    }
    public static bool InRange(double n, double center, double maxDistance) =>
        n > center - maxDistance && n < center + maxDistance;
    public static bool AreKeysPressed(this Keyboard keyboard, params Keys[] keys) =>
        keys.All(keyboard.IsKeyPressed);
    public static double Round(this double d, double interval) {
        int times = (int)(d / interval);
        var roundedDown = times * interval;
        if (d - roundedDown < interval / 2) {
            return roundedDown;
        } else {
            var roundedUp = roundedDown + interval;
            return roundedUp;
        }
    }
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
    public static bool HasElement(this XElement e, string key, out XElement result) =>
        (result = e.Element(key)) != null;
    public static bool HasElements(this XElement e, string key, out IEnumerable<XElement> result) =>
        (result = e.Elements(key)) != null;
    public static string Att(this XElement e, string key) =>
        e.Attribute(key)?.Value;
    public static string ExpectAtt(this XElement e, string key) =>
        e.Att(key)
            ?? throw e.Missing<string>(key);
    public static XElement ExpectElement(this XElement e, string name) =>
        e.Element(name)
            ?? throw new Exception($"Element <{e.Name}> requires subelement {name} ### {e.Name}");
    
    public static bool TryAtt(this XElement e, string key, out string result) =>
        (result = e.Att(key)) != null;
    public static string TryAtt(this XElement e, string key, string fallback = "") =>
        e.Att(key) ?? fallback;
    public static string TryAttNullable(this XElement e, string key) =>
        e.Att(key);
    public static char TryAttChar(this XElement e, string attribute, char fallback) =>
        e.TryAtt(attribute, out string s) ?
            (s.Length == 1 ?
                s.First() :
            s.StartsWith("\\") && int.TryParse(s.Substring(1), out var result) ?
                (char)result :
            throw e.Invalid<char>(attribute)
            ) : fallback;
    public static char ExpectAttChar(this XElement e, string attribute) =>
        e.TryAtt(attribute, out string s) ?
            (s.Length == 1 ?
                s.First() :
            s.StartsWith("\\") && int.TryParse(s.Substring(1), out var result) ?
                (char)result :
            throw e.Invalid<char>(attribute)
            ) : throw e.Invalid<char>(attribute);
    public static Col TryAttColor(this XElement e, string attribute, Col fallback) {
        if (e.TryAtt(attribute, out string s)) {
            if (int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
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
    public static Exception Missing<T>(this XElement e, string key) =>
        new Exception($"{typeof(T).Name} requires ${typeof(T).Name} attribute: {key} ### {e.Name}");
    public static Exception Invalid<T>(this XElement e, string key) =>
        new Exception($"{typeof(T).Name} value expected: {key}=\"{e.Attribute(key).Value}\" ### {e.Name}");
    public static int TryAttInt(this XElement e, string key, int fallback = 0) => 
        e.TryAtt(key, out var value) ?
            (int.TryParse(value, out int result) ?
                result :
            value.Any() ?
                Convert.ToInt32(new Expression(value).Evaluate()) :
            throw e.Invalid<int>(key)) :
        fallback;

    public static int? TryAttIntNullable(this XElement e, string key, int? fallback=null) =>
        e.TryAtt(key, out var value) ?
            (value == "null" ? null : 
            int.TryParse(value, out int result) ? result :
            value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) :
            throw e.Invalid<int?>(key)) :
        fallback;
    public static int? ExpectAttIntNullable(this XElement e, string key, int? fallback = null) =>
    e.TryAtt(key, out var value) ?
        (value == "null" ? null :
        int.TryParse(value, out int result) ? result :
        value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) :
        throw e.Invalid<int?>(key)) :
    throw e.Missing<int?>(key);
    public static Col ExpectAttColor(this XElement e, string key) {
        if (e.TryAtt(key, out string s)) {
            if (int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
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
    public static int ExpectAttInt(this XElement e, string key) =>
        e.Attribute(key) is XAttribute a ?
            ExpectAttributeInt(a) :
            throw e.Missing<int>(key);

    public static IDice ExpectAttDice(this XElement e, string key) =>
        e.Attribute(key) is XAttribute a ?
            ExpectAttributeDice(a) :
            throw new Exception($"<{e.Name}> requires dice range attribute: {key} ### {e} ### {e.Parent}");
    public static int ExpectAttributeInt(this XAttribute a) =>
        int.TryParse(a.Value, out int result) ? result :
            a.Value.Any() ? Convert.ToInt32(new Expression(a.Value).Evaluate()) :
            throw new Exception($"int value / equation expected: {a.Name} = \"{a.Value}\"");

    public static IDice ExpectAttributeDice(this XAttribute a) =>
        IDice.Parse(a.Value) ?? 
            throw new Exception($"int value / equation expected: {a.Name} = \"{a.Value}\"");

    public static double ExpectAttDouble(this XElement e, string key) =>
        e.TryAtt(key, out var value) ? 
            (double.TryParse(value, out double result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Missing<double>(key)) :
        throw e.Missing<double>(key);
    public static bool ExpectAttBool(this XElement e, string key) =>
        e.TryAtt(key, out var value) ?  
            (bool.TryParse(value, out bool result) ? result :
            throw e.Invalid<bool>(key)) :
        throw e.Missing<bool>(key);
    public static double TryAttDouble(this XElement e, string attribute, double fallback = 0) =>
        e.TryAtt(attribute, out var value) ?
            (double.TryParse(value, out double result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Invalid<double>(attribute)) :
        fallback;
    public static bool TryAttDouble2(this XElement e, string attribute, out double result, double fallback = 0) {
        var b = e.TryAtt(attribute, out var value);
        result = b ?
            (double.TryParse(value, out result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Invalid<double>(attribute)) :
        fallback;
        return b;
    }
    public static IEnumerable<string> GetKeys(this object o) {
        var pr = o.GetType().GetProperties();
        return pr.Select(p => p.Name);
    }
    public static Dictionary<string, T> ToDict<T>(this object o) {
        var pr = o.GetType().GetProperties();
        return pr.ToDictionary(p => p.Name, p => (T)p.GetValue(o, null));
    }
    public static Dictionary<U, T> ToDict<U, T>(this object o, Func<string, U> keyMap) {
        var pr = o.GetType().GetProperties();
        return pr.ToDictionary(p => keyMap(p.Name), p => (T)p.GetValue(o, null));
    }
    public static List<string> SplitLine(this string s, int width) {
        var result = new List<string>();
        var column = 0;
        var line = new StringBuilder();
        var word = new StringBuilder();

        void AddWord() {
            line.Append(word.ToString());
            word.Clear();
        }
        void AddLine() {
            result.Add(line.ToString());
            line.Clear();
            column = 0;
        }
        foreach (var c in s) {
            if (line.Length + column < width) {
                if (c == ' ') {
                    AddWord();
                    line.Append(c);
                    column++;
                } else if (c == '\n') {
                    AddWord();
                    column = 0;
                } else if (c == '-') {
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
        line.Append(word.ToString());
        if (line.Length > 0) {
            AddLine();
        }

        return result;
    }

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
    public static XY GetBoundaryPoint(XY dimensions, double angleRad) {
        while (angleRad < 0) {
            angleRad += 2 * Math.PI;
        }
        while (angleRad > 2 * Math.PI) {
            angleRad -= 2 * Math.PI;
        }
        var center = dimensions / 2;
        var halfWidth = dimensions.x / 2;
        var halfHeight = dimensions.y / 2;
        var diagonalAngle = dimensions.angleRad;


        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);

        bool horizontal = (angleRad < diagonalAngle || angleRad > Math.PI * 2 - diagonalAngle)
            || (angleRad < Math.PI + diagonalAngle && angleRad > Math.PI - diagonalAngle);

        double factor =
            horizontal ?
                Math.Abs(halfWidth / cos) :
                Math.Abs(halfHeight / sin);
        var offset = new XY(cos * factor, sin * factor);
        var result = center + offset;
        return result;
    }

    public static List<string> Wrap(this string s, int width) {
        var lines = new List<string> { "" };
        foreach (var word in Regex.Split(s, $"({Regex.Escape(" ")})")) {
            if (lines.Last().Length + word.Length < width) {
                lines[lines.Count - 1] += word;
            } else {
                if (word == " ") {
                    lines.Add("");
                } else {
                    lines.Add(word);
                }

            }
        }
        return lines;
    }
    public static void PaintCentered(this Window w, string s, int x, int y)=>
        w.Print(x - s.Length / 2, y, s);
    public static int ParseInt(this string s, int fallback = 0) =>
        int.TryParse(s, out int result) ? result : fallback;
    public static int ParseIntMin(this string s, int min, int fallback = 0) =>
        Math.Max(s.ParseInt(fallback), min);
    public static int ParseIntMax(this string s, int max, int fallback = 0) =>
        Math.Min(s.ParseInt(fallback), max);
    public static int ParseIntBounded(this string s, int min, int max, int fallback = 0) =>
        Range(min, s.ParseInt(fallback), max);
    public static int Range(int min, int max, int n) =>
        Math.Min(max, Math.Max(min, n));
    public static bool ParseBool(this string s, bool fallback = false) {
        return s == "true" ?
            true : (s == "false" ?
            false : fallback);
    }
    //We expect either no value or a valid value; an invalid value gets an exception
    public static bool TryAttBool(this XElement e, string attribute, bool fallback = false) =>
        e.TryAtt(attribute).ParseBool(fallback);
    public static bool TryAttributeBool(XAttribute a, bool fallback = false) {
        if (a == null) {
            return fallback;
        } else if (bool.TryParse(a.Value, out bool result)) {
            return result;
        } else {
            throw new Exception($"Bool value expected: {a.Name}=\"{a.Value}\"");
        }
    }
    public static bool? TryAttBoolNullable(this XElement e, string name, bool? fallback = null) =>
        e.Attribute(name)?.TryAttributeBoolOptional(fallback);
    
    public static bool? TryAttributeBoolOptional(this XAttribute a, bool? fallback = null) {
        if (a == null) {
            return fallback;
        } else if (bool.TryParse(a.Value, out bool result)) {
            return result;
        } else {
            throw new Exception($"int value expected: {a.Name}=\"{a.Value}\" ### {a.Parent.Name}");
        }
    }
    /*
    public static bool? ParseBool(this string s, bool? fallback = null) {
        switch(s) {
            case "true":
                return true;
            case "false":
                return false;
            default:
                return null;
        }
    }
    */
    /*
    public static Func<int> ParseIntGenerator(string s) {

    }
    */
    public static TEnum TryAttEnum<TEnum>(this XElement e, string attribute, TEnum fallback = default) where TEnum : struct =>
        e.Attribute(attribute)?.ParseEnum(fallback) ?? fallback;

    public static bool TryAttEnum<TEnum>(this XElement e, string attribute, out TEnum result) where TEnum : struct {
        bool b = e.TryAtt(attribute, out var s);
        result = b ? Enum.Parse<TEnum>(s) : default;
        return b;
    }
    public static object TryAttEnum(this XElement e, Type t, string attribute, object fallback) =>
        Convert.ChangeType(e.Attribute(attribute)?.ParseEnum(t, fallback), t) ?? fallback;
    public static TEnum ExpectAttEnum<TEnum>(this XElement e, string attribute) where TEnum : struct {
        string value = e.ExpectAtt(attribute);
        if (Enum.TryParse(value, out TEnum result)) {
            return result;
        } else {
            throw new Exception($"Enum value of {typeof(TEnum).Name} expected: {attribute}=\"{value}\"");
        }
    }
    //We expect either no value or a valid value; an invalid value gets an exception
    public static TEnum ParseEnum<TEnum>(this XAttribute a, TEnum fallback = default) where TEnum : struct {
        if (a == null) {
            return fallback;
        } else if (Enum.TryParse(a.Value, out TEnum result)) {
            return result;
        } else {
            throw new Exception($"Enum value of {fallback.GetType().Name} expected: {a.Name}=\"{a.Value}\"");
        }
    }
    public static object ParseEnum(this XAttribute a, Type t, object fallback) {
        if (a == null) {
            return fallback;
        } else if (Enum.TryParse(t, a.Value, out var result)) {
            return Convert.ChangeType(result, t);
        } else {
            throw new Exception($"Enum value of {fallback.GetType().Name} expected: {a.Name}=\"{a.Value}\"");
        }
    }
    static Dictionary<Type, string> TransgenesisTypes = new() {
        [typeof(int)] = "INTEGER",
        [typeof(string)] = "STRING",
        [typeof(double)] = "DOUBLE",
        [typeof(char)] = "CHAR",
        [typeof(bool)] = "BOOLEAN",
        [typeof(bool?)] = "BOOLEAN",
        [typeof(IDice)] = "DICE_RANGE",
        [typeof(Col)] = "COLOR"
    };
    public static void WriteSchema(Type type, Dictionary<Type, XElement> dict) {

        var inst = Activator.CreateInstance(type);

        void GetItemType(ref Type t) {
            var g = t.GetGenericTypeDefinition();
            if (g == typeof(HashSet<>) || g == typeof(List<>)) {
                t = t.GetGenericArguments()[0];
            }
        }
        if (type.IsEnum) {
            var root = new XElement("Enum") { Value = $"\n{string.Join('\n', type.GetEnumNames())}\n" };
            root.SetAttributeValue("name", type.Name);
            dict[type] = root;
        } else {
            var root = new XElement(type.Name);
            dict[type] = root;
            foreach (var f in type.GetFields()) {
                foreach (var a in f.GetCustomAttributes(true).OfType<IXml>()) {
                    if (a is IAtt att) {
                        var el = new XElement("A");
                        el.SetAttributeValue("name", att.alias ?? f.Name);
                        var t = att.parse ? (att.type ?? f.FieldType) : typeof(string);
                        if (att.separator != null) {
                            GetItemType(ref t);
                            el.SetAttributeValue("type", $"{TransgenesisTypes[t]}_ARRAY");
                        } else {
                            if (t.IsEnum) {
                                if (!dict.ContainsKey(t)) {
                                    WriteSchema(t, dict);
                                }
                                el.SetAttributeValue("type", t.Name);
                            } else {
                                el.SetAttributeValue("type", TransgenesisTypes[t]);
                            }

                        }
                        if (att is Req) {
                            el.SetAttributeValue("required", true);
                        } else if (att is Opt o) {
                            el.SetAttributeValue("default", f.GetValue(inst));
                        }
                        if (f.GetXmlDocsSummary() is { Length: > 0 } str) {
                            el.Add(new XElement("D") { Value = str });
                        }
                        root.Add(el);
                    } else if (a is Sub sub) {
                        var el = new XElement("E");
                        var t = sub.type ?? f.FieldType;
                        el.SetAttributeValue("name", sub.alias ?? f.Name);
                        el.SetAttributeValue("count", (sub.required, sub.multiple) switch {
                            (false, false) => "?",
                            (false, true) => "*",
                            (true, false) => "1",
                            (true, true) => "+"
                        });

                        if (sub.construct) {
                            if (sub.multiple) {
                                GetItemType(ref t);
                            }
                            if (!dict.ContainsKey(t)) {
                                WriteSchema(t, dict);
                            }
                            el.SetAttributeValue("inherit", t.Name);
                        }
                        root.Add(el);
                    }
                }
            }
        }
    }

    public class XMap {
		Dictionary<Type, Type> map = new();
		public void Add<K, V>() where V:XForm<K> {
            map[typeof(K)] = typeof(V);
        }
        public XForm<T> Get<T>() => (XForm<T>)map[typeof(T)];
		public bool Load(Type type, XLoad ctx, XElement source, out object dest) {
			bool b = map.TryGetValue(type, out Type t);
			dest = b ? x.LoadObject(source, ctx) : default;
			return b;
		}
        public bool Save(Type type, object source, XSave ctx, out XElement dest) {
            bool b = map.TryGetValue(type, out Type t);
            dest = b ? x.SaveObject(source, ctx) : default;
            return b;
        }
	}
	public interface XForm<T>{
		static abstract int SaveObject(T source, XSave d);
		static abstract T LoadObject(XLoad ctx, XElement source);
	}
	public class XFormSoundBuffer : XForm<SoundBuffer> {
        record Data(short[] Samples, uint ChannelCount, uint SampleRate) {
            public static explicit operator SoundBuffer(Data d) =>
                new(d.Samples, d.ChannelCount, d.SampleRate);
            public static explicit operator Data(SoundBuffer s) =>
                new(s.Samples, s.ChannelCount, s.SampleRate);
        }
		public static SoundBuffer LoadObject(XLoad ctx, XElement source) =>
			(SoundBuffer)ctx.Deserialize<Data>(source);
		public static int SaveObject(SoundBuffer source, XSave ctx) =>
            ctx.Save((Data)source);
	}
	public static bool IsCollection(this Type t) {
        typeof(XForm<int>).GetMethod("SaveObject");
        HashSet<Type> tt = [typeof(List<>), typeof(HashSet<>)];
        return t.IsGenericType ?
            tt.Contains(t.GetGenericTypeDefinition()) :
            t.IsArray;
    }
    public record XSave {
		public XMap map = new();
		public readonly ConcurrentDictionary<object, int> table = new();
		public XElement root = new("R");
		public int Save(object source) {
			var found = true;
			var i = table.GetOrAdd(source, o => { found = false; return table.Count; });
			if (found) return i;
			string SaveItem(object source) => source switch {
				null => "null",
				_ when source.GetType().IsPrimitive => JsonSerializer.Serialize(source),
				_ => $"{Save(source)}"
			};
            var t = source.GetType();
			var e = new XElement("O", t.AssemblyQualifiedName);
			root.Add(e);

            if(map.Save(t, source, this, out var dest)){
                e.Add(dest);
            } else {
				var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				e.ReplaceWith((XElement)(source switch {
					XElement ox => new("X", ox),
					string os => new("S", os),
					Type { AssemblyQualifiedName: { } aqn } => new("T", aqn),
					_ when t.AssemblyQualifiedName is { }aqn => source switch {
						IDictionary id => new("D", aqn, id
							.Keys.Cast<object>().Zip(id.Values.Cast<object>())
							.Select(((object key, object val) pair) => new XElement("P",
								new XElement("K", SaveItem(pair.key)),
								new XElement("V", SaveItem(pair.val)))
							)),
						IEnumerable ie when t.IsCollection() => new("C", aqn, ie.Cast<object>()
								.Select(item => new XElement("I", SaveItem(item))
							)),
						_ => new("O", aqn, t.GetFields(flags)
							.Where(f => !f.GetCustomAttributes<CompilerGeneratedAttribute>().Any())
							.Select(f => new XAttribute(f.Name, SaveItem(f.GetValue(source)))),
							t.GetProperties(flags)
							.Select(p => p.DeclaringType.GetProperty(p.Name, flags))
							.Where(p => p.GetSetMethod(true) is { } m)
							.Select(p => new XAttribute(p.Name, SaveItem(p.GetValue(source))))
							)
					}
				}));
			}
			return i;
		}
	}
	public class XLoad {
		XMap form = new();

		public readonly List<XElement> children;
		public readonly object[] table;
        public XLoad(XElement root) {
            children = root.Elements().ToList();
            table = new object[children.Count];
        }
        public T Load<T>(int index) => (T)Load(index);
        public T Load<T>(XElement e, string key) => (T)Load(e, key);
        public object Load(XElement e, string key) => Load(e.ExpectAttInt(key));
        /* Clone
        public T PopulateObject<T>(T source) {
            var dest = Activator.CreateInstance<T>();
            PopulateObject(typeof(T), dest, source);
            return dest;
		}
        */
        public T Deserialize<T>(XElement source) {
            var dest = Activator.CreateInstance<T>();
            PopulateObject(typeof(T), dest, source);
            return dest;
		}
		public void PopulateObject<T>(T dest, T source) => PopulateObject(typeof(T), dest, source);
		public void PopulateObject<T>(T dest, XElement source) => PopulateObject(typeof(T), dest, source);
		public void PopulateObject(Type t, object dest, XElement source){
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var fields = t
						.GetFields(flags)
						.Where(f => !f.GetCustomAttributes<CompilerGeneratedAttribute>().Any());
			var properties = t
				.GetProperties(flags)
				.Select(f => f.DeclaringType.GetProperty(f.Name, flags));
            foreach(var f in fields) {
				if (source.TryAtt(f.Name, out var data))
					f.SetValue(dest, LoadItem(data, f.FieldType));
			}
            foreach(var p in properties) {
				if (source.TryAtt(p.Name, out var data))
					p.GetSetMethod(true)?.Invoke(dest, [LoadItem(data, p.PropertyType)]);
			}
		}
		public void PopulateObject(Type t, object dest, object source) {
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var fields = t.GetFields(flags)
				.Where(f => !f.GetCustomAttributes<CompilerGeneratedAttribute>().Any());
			var properties = t.GetProperties(flags)
				.Select(f => f.DeclaringType.GetProperty(f.Name, flags));
			foreach (var f in fields)
                f.SetValue(dest, f.GetValue(source));
			foreach (var p in properties)
				p.SetValue(dest, p.GetValue(source));
		}
		public object LoadItem(string source, Type type) {
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				type = type.GetGenericArguments()[0];
			}
			return source is "null" ?
				null :
			type.IsPrimitive ?
				JsonSerializer.Deserialize(source, type) :
				Load(int.Parse(source));
		}
		public object Load(int index) {
			if (index < table.Length)
				return table[index];
			var source = children[index];
			return table[index] = source.Name.LocalName switch {
				"X" => source.Elements().First(),
				"S" => source.Value,
				"T" => Type.GetType(source.Value),
				"D" => new Lazy<dynamic>(() => {
					var tn = source.FirstNode.ToString();
					var t = Type.GetType(tn);
					var elements = source.Elements().ToArray();
					var o = (IDictionary)(table[index] = Activator.CreateInstance(t, elements.Length));
					if (t.GetGenericArguments() is [var pk, var pv]) {
						foreach (var pair in elements) {
							var key = LoadItem(pair.Element("K").Value, pk);
							var val = LoadItem(pair.Element("V").Value, pv);
							o.Add(key, val);
						}
					}
					return o;
				}).Value,
				"C" => new Lazy<dynamic>(() => {
					var t = Type.GetType(source.FirstNode.ToString());
					var elements = source.Elements().ToArray();
					var pt = t.IsArray ? t.GetElementType() : t.GetGenericArguments()[0];
					var items = source.Elements().Select(sub => LoadItem(sub.Value, pt));
					dynamic o = table[index] =
						t.IsArray ?
							Array.CreateInstance(pt, elements.Length) :
							Activator.CreateInstance(t, elements.Length);
					if (t.IsArray) {
						Array.Copy(items.ToArray(), (Array)o, elements.Length);
					} else {
						foreach (dynamic i in items)
							o.Add(i);
					}
					return o;
				}).Value,
				"O" => new Lazy<dynamic>(() => {
					var t = Type.GetType(source.FirstNode.ToString());
					var dest = table[index] = RuntimeHelpers.GetUninitializedObject(t);
					if (form.Load(t, this, source, out object data)){
                        PopulateObject(t, dest, data);
					} else {
                        PopulateObject(t, dest, source);
                    }
					return dest;
				}).Value
			};
		}
	}
	public static void Load<T>(this XElement root, out T obj) => obj = (T)root.Load();
    public static object Load(this XElement root) => new XLoad(root).Load(0);
	/// <summary>
	/// Read data from XML to populate the object fields. For attributes, mark fields with <c>Req</c> and <c>Opt</c>. For elements, mark fields with <c>Self</c> and <c>Sub</c>
	/// </summary>
	/// <param name="ele">The XML element to read data from</param>
	/// <param name="obj">The object to be populated</param>
	/// <param name="inherit">If a field is missing from </param>
	/// <param name="transform">Functions to convert values after reading.</param>
	/// <seealso cref="Req"/>
	/// <seealso cref="Opt"/>
	/// <seealso cref="Par"/>
	/// <seealso cref="Sub"/>
	/// <exception cref="Exception"></exception>
	public static void Initialize(this XElement ele, object obj, object inherit = null, Dictionary<string, object> transform = null, Dictionary<string, object> fallback = null) {
        var props = obj.GetType().GetFields();
        foreach (var p in props) {
            foreach(var a in p.GetCustomAttributes(true).OfType<IXml>()) {
                void Transform(ref object value, object f) {
                    var t = f.GetType();
                    if (typeof(Action).IsAssignableFrom(t)) {
                        (f as dynamic)();
                    } else if(typeof(Action<>).IsAssignableFrom(t)) {
                        (f as dynamic)(value as dynamic);
                    } else {
                        value = (f as dynamic)(value as dynamic);
                    }
                }
                bool Fallback(dynamic f, out dynamic value) {
                    var t = (Type)f.GetType();
                    if (typeof(Action).IsAssignableFrom(t)) {
                        f();
                        value = default;
                        return false;
                    } else {
                        value = f();
                        return true;
                    }
                }
                void Set(object parsed) =>
                        p.SetValue(obj, parsed);
                void Inherit() =>
                    Set(p.GetValue(inherit));
                Type GetItemType() =>
                    p.FieldType.GetGenericTypeDefinition() == typeof(List<>) || p.FieldType.GetGenericTypeDefinition() == typeof(HashSet<>) ?
                        p.FieldType.GetGenericArguments()[0] :
                    p.FieldType.IsArray ?
                        p.FieldType.GetElementType() :
                        throw new Exception("Unsupported subelement collection type");
                var key = p.Name;

                if (a is Err err) {
                    if (p.GetValue(obj) != null) {
                        continue;
                    }
                    throw new Exception($"{err.msg}: {ele} ## {ele.Parent.Name}");
                } else if (a is Par self) {
                    if (self.fallback && p.GetValue(obj) != null) {
                        continue;
                    }
                    Set(Create(ele));
                    object Create(XElement element) {
                        object value = element;
                        if (self.construct) {
                            value = (self.type ?? p.FieldType).GetConstructor(new[] { typeof(XElement) }).Invoke(new[] { element });
                        }
                        if (transform?.TryGetValue(p.Name, out object f) == true) {
                            Transform(ref value, f);
                        }
                        return value;
                    }
                } else if (a is Sub sub) {
                    key = sub.alias ?? key;
                    if (sub.multiple) {
                        var elements = ele.Elements(key).ToList();
                        if (!elements.Any()) {
                            if (inherit != null) {
                                Inherit();
                            } else if (sub.required) {
                                throw new Exception($"<{ele.Name}> requires at least one <{key}> subelement: {ele} ### {ele.Parent.Name}");
                            } else if(fallback?.TryGetValue(key, out dynamic f) ?? false) {
                                if(Fallback(f, out dynamic value)) {
                                    Set(value);
                                }
                            }
                            continue;
                        }
                        Set(CreateCollectionFrom(elements));
                        object CreateCollectionFrom(List<XElement> elements) {

                            /*
                            IEnumerable<object> CreateValues(Type type) {
                                var con = type.GetConstructor(new[] { typeof(XElement) });
                                return elements.Select(element => con.Invoke(new[] { element })).ToList();
                            }
                            */

                            var type = GetItemType();
                            var col = (sub.type ?? p.FieldType).GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(type) });
                            var con = type.GetConstructor(new[] { typeof(XElement) });
                            var items = elements.Select(element => con.Invoke(new[] { element })).ToList();

                            //this works
                            var i = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(type).Invoke(null, new[] { items });
                            return col.Invoke(new[] { i });

                            //return Activator.CreateInstance(p.FieldType, (dynamic) items);

                        }
                    } else {
                        if (!ele.HasElement(key, out var element)) {
                            if (inherit != null) {
                                Inherit();
                            } else if (sub.required) {
                                throw new Exception($"<{ele.Name}> requires one <{key}> subelement: {ele} ### {ele.Parent.Name}");
                            } else if (fallback?.TryGetValue(key, out dynamic f) ?? false) {
                                f();
                            }
                            continue;
                        }
                        Set(Create(element));
                        object Create(XElement element) {
                            object value = element;
                            if (sub.construct) {
                                value = (sub.type ?? p.FieldType).GetConstructor(new[] { typeof(XElement) }).Invoke(new[] { element });
                            }
                            if (transform?.TryGetValue(p.Name, out object f) == true) {
                                Transform(ref value, f);
                            }
                            return value;
                        }
                    }

                } else if (a is IAtt ia) {
                    key = ia.alias ?? key;
                  

                    object CreateCollectionFrom(IEnumerable<string> elements, Type type) {
                        var col = p.FieldType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(type) });
                        if (type != typeof(string)) {

                            var con = type.GetConstructor(new[] { typeof(XElement) });
                            var items = elements.Select(element => con.Invoke(new[] { element })).ToList();
                            return col.Invoke(new[] { items });
                        } else {
                            return col.Invoke(new[] { elements });
                        }
                    }
                    var value = ele.Att(key);
                    if (value == null) {
                        if (inherit != null) {
                            Set(p.GetValue(inherit));
                        } else if(a is Req) {
                            throw new Exception($"<{ele.Name}> requires {key} attribute: {ele} ### {ele.Parent.Name}");
                        } else if (fallback?.TryGetValue(key, out var f) ?? false) {
                            if (Fallback(f, out dynamic result)) {
                                Set(result);
                            }
                        }
                        continue;
                    }
                    var parseDict = new Dictionary<Type, Func<object>>() {
                        [typeof(string)] = () => value,

                        [typeof(bool)] = ParseBool,
                        [typeof(int)] = ParseInt,
                        [typeof(char)] = ParseChar,
                        [typeof(double)] = ParseDouble,
                        [typeof(double?)] = ParseDoubleNullable,


                        [typeof(bool?)] = ParseBoolNullable,
                        [typeof(int?)] = () => ele.ExpectAttIntNullable(key),

                        [typeof(IDice)] = () => ele.ExpectAttDice(key),
                        [typeof(Col)] = () => ele.ExpectAttColor(key),
                        [typeof(Col?)] = () => ele.ExpectAttColor(key),
                    };
                    if (ia.separator?.Any() == true) {
                        Set(ParseCollection());
                    } else {
                        dynamic result = value;
                        if (ia.parse) {
                            var type = ia.type ?? p.FieldType;

                            if (type.IsEnum) {
                                result = Enum.Parse(type, result);
                            } else {
                                result = parseDict[type]();
                            }
                        }
                        //dynamic result = parseDict[p.FieldType]();
                        if (transform?.TryGetValue(p.Name, out object f) == true) {
                            Transform(ref result, f);
                        }
                        Set(result);
                    }
                    object ParseBool() =>
                        bool.TryParse(value, out var result) ? result : throw Error<bool>();
                    object ParseBoolNullable() =>
                        value == "null" ? null : ParseBool();
                    object ParseInt() =>
                        value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) : throw Error<int>();
                    object ParseChar() =>
                        (value.Length == 1 ?
                            value.First() :
                        value.StartsWith("\\") && int.TryParse(value.Substring(1), out var result) ?
                            (char)result :
                            throw Error<char>());
                    object ParseDouble() =>
                        value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) : throw Error<double>();
                    object ParseDoubleNullable() =>
                        value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) : null;
                    object ParseCollection() =>
                        CreateCollectionFrom(value.Split(ia.separator), GetItemType());

                    Exception Error<T>() =>
                        ele.Invalid<T>(key);
                }
            }

        }
    }

    public static Action Bind<T>(this Action<T> f, T arg0) => () => f(arg0);
    public static void Switch(params (bool, Action)[] actions) {

    }
    public static Func<T, Action> PreBind<T>(Action<T> a) => (T t) => () => a(t);
    public static Func<U, Action> PreBind<T, U>(Action<T> a, Func<U, T> tr) => (U u) => () => a(tr(u));
    public static TValue TryLookup<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue fallback = default) =>
        d.ContainsKey(key) ? d[key] : fallback;
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

    //https://stackoverflow.com/a/28037434
    public static double AngleDiffDeg(double from, double to) {
        void mod(ref double a) {
            while (a < 0)
                a += 360;
            while (a >= 360)
                a -= 360;
        }

        mod(ref from);
        mod(ref to);
        
        double diff = (to - from + 180) % 360 - 180;
        return diff < -180 ? diff + 360 : diff;
    }
    /// <summary>
    /// Calculates the minimum delta needed
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns>The signed difference of the quickest turn</returns>
    public static double AngleDiffRad(double from, double to) {
        var pi = Math.PI;
        var tau = pi * 2;
        void mod(ref double a) {
            while (a < 0)
                a += tau;
            while (a >= tau)
                a -= tau;
        }

        mod(ref from);
        mod(ref to);

        double diff = (to - from + pi) % tau - pi;
        return diff < -pi ? diff + tau : diff;
    }
    public static bool IsRight(double from, double to) =>
        (XY.Polar(to)-XY.Polar(from)).magnitude2 > (XY.Polar(to)-XY.Polar(from - 0.1)).magnitude2;
    public static Func<T, bool> Or<T>(params Func<T, bool>[] f) {
        Func<T, bool> result = e => true;
        foreach (Func<T, bool> condition in f) {
            if (condition == null)
                continue;
            Func<T, bool> previous = result;
            result = e => (previous(e) || condition(e));
        }
        return result;
    }
    public static Func<T, bool> And<T>(params Func<T, bool>[] f) {
        Func<T, bool> result = e => true;
        foreach (Func<T, bool> condition in f) {
            if (condition == null)
                continue;
            Func<T, bool> previous = result;
            result = e => (previous(e) && condition(e));
        }
        return result;
    }
    public static T Elvis<T>(this object o, T result) =>
        o == null ? default(T) : result;
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