using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace RogueFrontier;
public record ItemFilter([Opt(separator = ";")] HashSet<string> require, [Opt(separator = ";")] HashSet<string> reject) {
    public ItemFilter() : this(new(), new()) { }
    public ItemFilter(XElement e) : this(
        e.TryAtt("require").Split(";").ToHashSet(),
        e.TryAtt("reject").Split(";").ToHashSet()
        ) { }
    public static ItemFilter Parse(string s) {
        var require = new HashSet<string>();
        var reject = new HashSet<string>();
        foreach(Match m in Regex.Matches(s, "(?<pre>\\+|-)(?<att>[a-zA-Z0-9]+)")) {
            (m.Groups["pre"].Value switch {
                "+" => require,
                "-" => reject,
                _ => throw new Exception("This should not happen")
            }).Add(m.Groups["att"].Value);
        }
        return new(require, reject);
    }
    public bool Matches(Item i) {
        var f = (string att) => i.type.attributes.Contains(att);
        return require.All(f) && !reject.Any(f);
    }
}
public record TradeEntry(ItemFilter filter, double priceFactor, int priceInc) {
    public TradeEntry(XElement e) : this(
        new ItemFilter(e),
        e.TryAttDouble("priceAdj"),
        e.TryAttInt("priceInc")
        ) {
        if(e.TryAtt("price", out var strPrice)) {
            (priceFactor, priceInc) = (0, int.Parse(strPrice));
        }
    }
}
public record TradeDesc() : IDesignType {
    Dictionary<ItemType, int> priceTable;
    List<TradeEntry> buyAdj, sellAdj;
    public void Initialize(TypeCollection tc, XElement e) {
        priceTable = e.Element("Prices")?.Value.Trim().Split("\n")
            .Select(line => line.Split(":")).ToDictionary(
            parts => tc.Lookup<ItemType>(parts[0]),
            parts => int.Parse(parts[1])) ?? new();
        sellAdj = e.Element("Buy")?.Elements("Item").Select(e => new TradeEntry(e)).ToList();
        buyAdj = e.Element("Sell")?.Elements("Item").Select(e => new TradeEntry(e)).ToList();
    }
    public int GetBuyPrice(Item i) {
        int price = priceTable[i.type];
        foreach(var e in buyAdj) {
            if (e.filter.Matches(i)) {
                price = (int)(price * e.priceFactor) + e.priceInc;
            }
        }
        return -1;
    }
    public int GetSellPrice(Item i) {
        int price = priceTable[i.type];
        foreach (var e in sellAdj) {
            if (e.filter.Matches(i)) {
                price = (int)(price * e.priceFactor) + e.priceInc;
            }
        }
        return -1;
    }
}