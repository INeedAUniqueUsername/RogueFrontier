using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
namespace Common;

public interface IDice {
    public static IDice Apply(IDice original, double factor, int inc) {
        var result = original;
        if (factor != 1)
            result = new DiceFactor(result, factor);
        if(inc != 0)
            result = new DiceInc(result, inc);
        return result;
    }
    public static IDice Parse(string s) {
        Match m;
        IDice result = null;
        if ((m = Regex.Match(s, "^(?<value>\\-?[0-9]+$)")).Success) {
            return new Constant(int.Parse(m.Groups["value"].Value));
        }
        if ((m = Regex.Match(s, "^(?<min>\\-?[0-9]+)-(?<max>\\-?[0-9]+)$")).Success) {
            return new IntRange(int.Parse(m.Groups["min"].Value), int.Parse(m.Groups["max"].Value));
        }
        if ((m = Regex.Match(s, "^(?<n>[0-9]+)d(?<m>\\-?[0-9]+)((\\+(?<bonus>[0-9]+))|(?<bonus>\\-[0-9]+))?$")).Success) {
            return new DiceRange(int.Parse(m.Groups["n"].Value), int.Parse(m.Groups["m"].Value), m.Groups["bonus"].Value is string { Length:>0} b ? int.Parse(b) : 0);
        }
        if((m = Regex.Match(s, "(,?([0-9]+))+")).Success) {
            return new Distribution(Regex.Matches(s, "[0-9]+").Select(m => int.Parse(m.Value)).ToArray());
        }
        return result;
    }
    public static bool TryParse(string s, out IDice result) => (result = Parse(s)) != null;
    public static string strBonus(int bonus) => bonus > 0 ? $"+{bonus}" : bonus < 0 ? $"-{bonus}" : "";
    int Roll();
    string str { get; }
    public int min { get; }
}

public record DiceInc(IDice sub, int bonus) : IDice {
    public int Roll() => sub.Roll() + bonus;
    public string str => $"({sub.str}){IDice.strBonus(bonus)}";

    public int min => sub.min + bonus;
}
public record DiceFactor(IDice sub, double factor) : IDice {

    public int Roll() => (int)(sub.Roll() * factor);
    public string str => $"({sub.str})*{factor}";
    public int min => (int)(sub.min * factor);
}
public record Constant(int Value) : IDice {
    public int Roll() => Value;
    public string str => $"{Value}";
    public int min => Value;
}
public record IntRange(int min, int max) :IDice{
    public Rand r = new();
    public int range => max - min;
    public int Value => r.NextInteger(min, max);
    public int Roll() => Value;
    public string str => $"{min}-{max}";
    int IDice.min => this.min;
}
public record DiceRange(int n, int m, int bonus) : IDice {
    public Rand r=new();
    public int Value => Enumerable.Range(0, n).Select(i => (int)Math.Ceiling(r.NextDouble()*m)).Sum() + bonus;
    public int Roll() => Value;
    public string str => $"{n}d{m}{IDice.strBonus(bonus)}";
    public int min => n + bonus;
}
public record Distribution(int[] choices) : IDice {

    public Rand r = new();
    public int Roll() => choices.GetRandom(r);
    public string str => $"[{string.Join(",", choices)}]";
    public int min => choices.Min();
}