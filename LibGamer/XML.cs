using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Data;
using NCalc;
using System.Numerics;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Collections;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using LibGamer;
using Namotion.Reflection;

namespace Common;
public static class Main {

	//https://stackoverflow.com/a/28037434
	public static double AngleDiffDeg (double from, double to) {
		void mod (ref double a) {
			while(a < 0)
				a += 360;
			while(a >= 360)
				a -= 360;
		}

		mod(ref from);
		mod(ref to);

		double diff = (to - from + 180) % 360 - 180;
		return diff < -180 ? diff + 360 : diff;
	}
	public static double Lerp(double x, double fromMin, double fromMax, double toMin, double toMax, double pow) {
		var fromRange = fromMax - fromMin;
		var toRange = toMax - toMin;
		
		var toDist = (Math.Clamp(x, fromMin, fromMax) - fromMin) * toRange / fromRange;
		toDist = Math.Pow(toDist / toRange, pow);
		return toMin + toRange * toDist;
	}
	public static string LerpString(this string str, double x, double fromMin, double fromMax, double pow) =>
		str.Substring(0, (int)Main.Lerp(x, fromMin, fromMax, 0, str.Length, pow));
	public static Tile[] LerpString(this Tile[] str, double x, double fromMin, double fromMax, double gamma) =>
		str[..(int)Lerp(x, fromMin, fromMax, 0, str.Length, gamma)];
	public static Tile[] ConcatColored(Tile[][] parts) {
		return [..parts.SelectMany(cs => cs)];
	}
	public static string Repeat(this string str, int times) =>
		string.Join("", from i in times select str);
	public static string ExpectFile(string path) =>
		 (File.Exists(path = Path.GetFullPath(path))) ? path :
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
	public static string Att(this XElement e, params string[] key) =>
		key.Select(k => e.Attribute(k)?.Value).Except([null]).FirstOrDefault();


	public static string ExpectAtt(this XElement e, string key) =>
		e.Att(key)
			?? throw e.Missing<string>(key);
	public static XElement ExpectElement(this XElement e, string name) =>
		e.Element(name)
			?? throw new Exception($"Element <{e.Name}> requires subelement {name} ### {e.Name}");

	public static bool TryAtt (this XElement e, string key, out string result) =>
		(result = e.Att(key)) != null;

	public static bool TryAtt (this XElement e, string[] key, out string result) =>
		(result = e.Att(key)) != null;
	public static string TryAtt (this XElement e, string key, string fallback = "") =>
		e.Att(key) ?? fallback;

	public static string TryAtt (this XElement e, string[] key, string fallback = "") =>
		e.Att(key) ?? fallback; public static string TryAttNullable(this XElement e, string key) =>
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
	public static uint TryAttUint (this XElement e, string key, uint fallback = 0) =>
		e.TryAtt(key, out var value) ?
			(uint.TryParse(value, out var result) ?
				result :
			value.Any() ?
				Convert.ToUInt32(new Expression(value).Evaluate()) :
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

	public static List<Tile[]> SplitLine(this Tile[] s, int width) {
		if(s.Length == 0)
			return [[new Tile(0, 0, ' ')]];

		var result = new List<Tile[]>();
		var column = 0;
		var line = new List<Tile>();
		var word = new List<Tile>();

		void AddWord() {
			line.AddRange(word);
			word.Clear();
		}
		void AddLine() {
			result.Add([..line]);
			line.Clear();
			column = 0;
		}
		foreach (var c in s) {
			if (column < width) {
				var g = c.Glyph;
				if (g == ' ') {
					AddWord();
					line.Add(c);
					column++;
				} else if (g == '\n') {
					AddWord();
					column = 0;
				} else if (g == '-') {
					word.Add(c);
					AddWord();
				} else {
					word.Add(c);
					column++;
				}
			} else {
				word.Add(c);
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
	public static int ParseInt(this string s, int fallback = 0) =>
		int.TryParse(s, out int result) ? result : fallback;
	public static int ParseIntMin(this string s, int min, int fallback = 0) =>
		Math.Max(s.ParseInt(fallback), min);
	public static int ParseIntMax(this string s, int max, int fallback = 0) =>
		Math.Min(s.ParseInt(fallback), max);
	public static int ParseIntBounded(this string s, int min, int max, int fallback = 0) =>
		Math.Clamp(s.ParseInt(fallback), min, max);
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
		[typeof(uint)] = "COLOR"
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
		readonly Dictionary<Type, LoadObject> load = new();
		readonly Dictionary<Type, SaveObject> save = new();
		private delegate XElement SaveObject(XSave ctx, object source);
		private delegate object LoadObject(XLoad ctx, XElement source);
		public XMap() { }
		public XMap(params Type[] ports) {
			foreach(var u in ports) {
				var t = u.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(XPort<>)).GetGenericArguments()[0];
				var tl = u.GetMethod(nameof(LoadObject));
				var ts = u.GetMethod(nameof(SaveObject));
				load[t] = (ctx, source) => tl.Invoke(null, [ctx, source]);
				save[t] = (ctx, source) => (XElement)ts.Invoke(null, [ctx, source]);
			}
		}
		public void Add<K, V>() where V:XPort<K> {
			//map[typeof(K)] = typeof(V);
			load[typeof(K)] = (ctx, source) => typeof(V).GetMethod(nameof(LoadObject)).Invoke(null, [ctx, source]);
			save[typeof(K)] = (ctx, source) => (XElement)typeof(V).GetMethod(nameof(SaveObject)).Invoke(null, [ctx, source]);
		}
		//public XForm<T> Get<T>() => (XForm<T>)map[typeof(T)];
		public bool Load(Type type, XLoad ctx, XElement source, out object dest) {
			bool b = load.TryGetValue(type, out var loadF);
			dest = b ? loadF(ctx, source.Elements().Single()) : default;
			return b;
		}
		public bool Save(Type type, XSave ctx, object source, out XElement dest) {
			bool b = save.TryGetValue(type, out var saveF);
			dest = b ? new XElement("O", type.AssemblyQualifiedName, saveF(ctx, source)) : default;
			return b;
		}
	}
	public static U CrossConstruct<U>(this object source) {
		var t = source.GetType();
		var tc = t.GetConstructors().Where(c => c.GetParameters().Any());
		var uc = typeof(U).GetConstructors().Where(c => c.GetParameters().Any());
		var c = uc.First(tcon => uc.Any(ucon =>
			tcon.GetParameters().Select(p => (p.Name, p.ParameterType)).SequenceEqual(
				ucon.GetParameters().Select(p => (p.Name, p.ParameterType)
				))));
		return (U)c.Invoke(c.GetParameters().Select(p => t.GetProperty(p.Name).GetValue(source)).ToArray());
	}
	/// <summary>
	/// Maps <typeparamref name="T"/> to intermediate data format.
	/// </summary>
	/// <typeparam name="T">The type to handle</typeparam>
	public interface XPort<T> {
		/// <summary>
		/// Save <typeparamref name="T"/> <paramref name="source"/> into <paramref name="ctx"/>. Convert <paramref name="source"/> to intermediate format and call <see cref="XSave.SavePointer(object)"/> to save the intermediate.
		/// </summary>
		/// <param name="ctx">Contains the object data</param>
		/// <param name="source">The object to save</param>
		/// <returns>Index of the object data</returns>
		static abstract XElement SaveObject(XSave ctx, T source);
		/// <summary>
		/// Load <typeparamref name="T"/> object from <paramref name="source"/> data. Call <see cref="XLoad.DeserializeValue{U}(XElement)"/> to convert <paramref name="source"/> to intermediate.
		/// </summary>
		/// <param name="ctx">Contains the object data</param>
		/// <param name="source">The data to load from. Fields are stored as indices.</param>
		/// <returns>The object loaded from data</returns>
		static abstract T LoadObject(XLoad ctx, XElement source);
	}
	/*
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
	*/
	public static bool IsCollection(this Type t) {
		//typeof(XPort<int>).GetMethod("SaveObject");
		HashSet<Type> tt = [typeof(List<>), typeof(HashSet<>)];
		return t.IsGenericType ?
			tt.Contains(t.GetGenericTypeDefinition()) :
			t.IsArray;
	}


	public class AdaptiveEqualityComparer : EqualityComparer<object>{
		public readonly static AdaptiveEqualityComparer Instance = new();

		private bool E(object? x, object? y){
			Type tx = x.GetType(), ty = y.GetType();
			

			if (tx.IsValueType != ty.IsValueType){
				return false;
			} else if(x is string s && y is string sy){
				var b = s.Equals(sy);
				return b;
			} else if(tx.IsValueType) {
				return x.GetHashCode() == y.GetHashCode();
			} else {
				return object.ReferenceEquals(x, y);
			}
		}
		public override bool Equals(object x, object y) => E(x, y);
		public override int GetHashCode(object obj) => obj is string s ? s.GetHashCode() : RuntimeHelpers.GetHashCode(obj);
	}

	/// <summary>
	/// Saves objects to XML data.
	/// </summary>
	public record XSave {
		/// <summary>Contains ports for unsafe types</summary>
		public XMap map = new();
		/// <summary>Lazy map from object to index</summary>
		public readonly ConcurrentDictionary<object, int> table = new(AdaptiveEqualityComparer.Instance);
		/// <summary>Contains all saved data</summary>
		public XElement root = new("R");

		public XElement Serialize<T>(T source) {
			var t = typeof(T);
			XElement dest = new("D", t.AssemblyQualifiedName);
			ToXml(t, dest, source);
			return dest;
		}
		public XElement SerializeValue(object source, Type t) {
			XElement dest = new("D", t.AssemblyQualifiedName);
			ToXml(t, dest, source);
			return dest;
		}

		public void ToXml(Type t, XElement dest, object source) {
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			/*
			t.GetFields(flags)
								.Select(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() is null ?
									new XAttribute(f.Name, SaveItem(f.GetValue(source))) : null),
							t.GetProperties(flags).Select(
								p => p.DeclaringType.GetProperty(p.Name, flags) is { SetMethod:{ } } pp && pp.GetIndexParameters() is [] ?
									new XAttribute(pp.Name, SaveItem(pp.GetValue(source))) : null)
			 */
		}
		private string SaveItem(object source, Type type) {
			if (type.GenericTypeArguments is [{ } ta] && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				type = ta;
			}
			return
				source == null ?
					"null" :
				type.IsPrimitive ?
					JsonSerializer.Serialize(source) :
				/*
				type.IsValueType ?
					$"{SerializeValue(source, type)}" :
				*/
				$"{SavePointer(source)}";
		}
		/// <summary>
		/// Saves <paramref name="source"/> to XML.
		/// </summary>
		/// <param name="source">The object to save</param>
		/// <returns>Index of the XML data</returns>
		public int SavePointer(object source) {
			var found = true;

			if((source as string)?.Contains("ModRoll") == true)
			{
				int a = 0;
			}
			var i = table.GetOrAdd(source, o => { found = false; return table.Count; });
			if (found) return i;
			var t = source.GetType();
			XElement e = new("A");
			root.Add(e);
			if (map.Save(t, this, source, out var dest)){
				e.ReplaceWith(dest);
			} else {

				var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				e.ReplaceWith((XElement)(source switch {
					XElement ox => new("X", ox),
					string os => new("S", os),
					Type { AssemblyQualifiedName: { } aqn } => new("T", aqn),
					_ when t.AssemblyQualifiedName is { }aqn => source switch {
						IDictionary id when t.GenericTypeArguments is [{}k,{}v] =>
							new("D", SavePointer(aqn), id.Keys.Cast<object>().Select(key =>
								new XElement("P",
									new XElement("K", SaveItem(key, k)),
									new XElement("V", SaveItem(id[key], v))))),
						IEnumerable ie when t.IsCollection() =>
							new("C", SavePointer(aqn), ie.Cast<object>().Select(item =>
								new XElement("I", SaveItem(item, t.IsArray?t.GetElementType():t.GenericTypeArguments[0])))),
						_ => new("O", SavePointer(aqn), t.GetFields(flags)
								.Select(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() is null ?
									new XAttribute(f.Name, SaveItem(f.GetValue(source), f.FieldType)) : null),
							t.GetProperties(flags).Select(
								p => p.DeclaringType.GetProperty(p.Name, flags) is { SetMethod:{ } } pp && pp.GetIndexParameters() is [] ?
									new XAttribute(pp.Name, SaveItem(pp.GetValue(source), pp.PropertyType)) : null)
							)
					}
				}));
			}
			return i;
		}
	}
	/// <summary>Loads objects from XML data</summary>
	public class XLoad{
		/// <summary>Contains ports for unsafe types</summary>
		XMap form = new();
		public readonly List<XElement> data;
		/// <summary>Lazy map from index to object</summary>
		public readonly Dictionary<int, object> table;
		public XLoad(XElement root) {
			data = root.Elements().ToList();
			table = new (data.Count);
		}
		public T Load<T>(int index) => (T)LoadPointer(index);
		public T Load<T>(XElement e, string key) => (T)Load(e, key);
		public object Load(XElement e, string key) => LoadPointer(e.ExpectAttInt(key));
		/* Clone
		public T PopulateObject<T>(T source) {
			var dest = Activator.CreateInstance<T>();
			PopulateObject(typeof(T), dest, source);
			return dest;
		}
		*/
		public T DeserializeValue<T>(XElement source) {
			var dest = Activator.CreateInstance<T>();
			FromXml(typeof(T), dest, source);
			return dest;
		}
		public object DeserializeValue(XElement source, Type t) {
			var dest = Activator.CreateInstance(t);
			FromXml(t, dest, source);
			return dest;
		}
		//public void PopulateObject<T>(T dest, T source) => LoadObject(typeof(T), dest, source);
		//public void PopulateObject<T>(T dest, XElement source) => LoadObject(typeof(T), dest, source);
		public void FromXml(Type t, object dest, XElement source){
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var fields = t.GetFields(flags).Where(
				f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null);
			var properties = t.GetProperties(flags).Select(
				f => f.DeclaringType.GetProperty(f.Name, flags));
			foreach(var f in fields) {
				if (source.TryAtt(f.Name, out var data))
					f.SetValue(dest, LoadItem(data, f.FieldType));
			}
			foreach(var p in properties) {
				if (source.TryAtt(p.Name, out var data) && p.SetMethod is {}m)
					//https://stackoverflow.com/questions/48792034/set-private-setter-on-property-via-reflection
					m.Invoke(dest, [LoadItem(data, p.PropertyType)]);
			}
		}
		public void FromObject(Type t, object dest, object source) {
			var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var fields = t.GetFields(flags).Where(
				f => f.GetCustomAttribute<CompilerGeneratedAttribute>() is null);
			foreach (var f in fields)
				f.SetValue(dest, f.GetValue(source));
			var properties = t.GetProperties(flags).Select(
				f => f.DeclaringType.GetProperty(f.Name, flags));
			foreach (var p in properties)
				p.SetValue(dest, p.GetValue(source));
		}
		public object LoadItem(string source, Type type) {
			//typeof(List<>).GetGenericTypeDefinition();
			if (type.GenericTypeArguments is [{} ta] && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				type = ta;
			}
			return source is "null" ?
				null :
			type.IsPrimitive ?
				JsonSerializer.Deserialize(source, type) :
			/*
			type.IsValueType ?
				DeserializeValue(XElement.Parse(source), type) :
			*/
				LoadPointer(int.Parse(source));
		}

		static string GetContent(XElement e) => string.Join("", e.Nodes().Select(n => n.ToString())).Replace("&lt;", "<").Replace("&gt;", ">");

		public string LoadString(string index) => (string)LoadPointer(int.Parse(index));
		//Support serializing methods? https://stackoverflow.com/a/11193717
		/// <summary>
		/// Loads object from XML data.
		/// </summary>
		/// <param name="index">Index of the XML data to load from</param>
		/// <returns>The object loaded from the XML element at <paramref name="index"/></returns>
		public object LoadPointer(int index) {
			if (table.TryGetValue(index, out dynamic dest))
				return dest;
			var source = data[index];
			return table[index] = source.Name.LocalName switch {
				"X" => source.Elements().First(),
				"S" => source.Value,
				"T" => Type.GetType(source.Value),
				"D" => new Lazy<dynamic>(() => {
					var t = Type.GetType(LoadString(source.FirstNode.ToString()));
					var elements = source.Elements().ToArray();
					var o = (IDictionary)(table[index] = Activator.CreateInstance(t, elements.Length));
					if (t.GenericTypeArguments is [{}k, {}v]) {
						foreach (var p in elements) {
							if(p.Elements().Select(GetContent).ToArray() is [{}sk, {}sv]){
								o.Add(LoadItem(sk, k), LoadItem(sv, v));
							}
						}
					}
					return o;
				}).Value,
				"C" => new Lazy<dynamic>(() => {
					var elements = source.Elements().ToArray();
					var n = elements.Length;
					Type t = Type.GetType(LoadString(source.FirstNode.ToString())), pt = null;
					if (t.IsArray) {
						pt = t.GetElementType();
						dest = table[index] = Array.CreateInstance(pt, n);
					} else {
						pt = t.GenericTypeArguments.Single();
						dest = table[index] = Activator.CreateInstance(t, n);
					}
					var items = elements.Select(sub => LoadItem(GetContent(sub), pt)).ToArray();
					if (t.IsArray) {
						Array.Copy(items, dest, n);
					} else {
						foreach (dynamic i in items) dest.Add(i);
					}
					return dest;
				}).Value,
				"O" => new Lazy<dynamic>(() => {
					var t = Type.GetType(LoadString(source.FirstNode.ToString()));
					var dest = table[index] = RuntimeHelpers.GetUninitializedObject(t);
					if (form.Load(t, this, source, out object data))
						FromObject(t, dest, data);
					else
						FromXml(t, dest, source);
					return dest;
				}).Value
			};
		}
	}
	//public static void Load<T>(this XElement root, out T obj) => obj = (T)root.Load();
	public static object Load(this XElement root) => new XLoad(root).LoadPointer(0);
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
		var props = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
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

								var c = (sub.type ?? p.FieldType).GetConstructor([ typeof(XElement) ]);

								value = c.Invoke([ element ]);
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
						//uint
						//[typeof(Col)] = () => ele.ExpectAttColor(key),
						//[typeof(Col?)] = () => ele.ExpectAttColor(key),
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
