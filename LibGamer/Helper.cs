global using static System.Math;
using RangeExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGamer;
public static class IntExtension {
	public static RangeEnumerable AsEnumerable (this int end) {
		return (..end);
	}
	public static RangeEnumerable.Enumerator GetEnumerator (this int end) {
		return (..end).GetEnumerator();
	}
	public static SelectRange<T> Select<T> (this int end, Func<int, T> selector) {
		return (..end).AsEnumerable().Select(selector);
	}

	public static IEnumerable<T> SelectMany<T>(this int end, Func<int, IEnumerable<T>> select) {
		return end.AsEnumerable().SelectMany(select);
	}

	public static IEnumerable<T> SelectMany<T> (this Range range, Func<int, IEnumerable<T>> select) {
		return range.AsEnumerable().SelectMany(select);
	}

	public static IEnumerable<(int x, int y)> Product (this Range a, Range b) {
		return a.SelectMany(x => b.Select(y => (x, y)));
	}

	public static IEnumerable<(int x, int y)> Product (this int a, int b) {
		return (..a).Product(..b);
	}
}