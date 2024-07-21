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
}