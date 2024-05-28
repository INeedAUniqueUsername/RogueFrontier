using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RogueFrontier;
public record FragmentMod {
    public static FragmentMod EMPTY = new FragmentMod();
    [Opt] public bool curse = false;
    public IntMod
        damageHP = IntMod.EMPTY,
        missileSpeed = IntMod.EMPTY,
        lifetime = IntMod.EMPTY,
        maxHP = IntMod.EMPTY;
    public FragmentMod () { }
    public FragmentMod(XElement e) {
        e.Initialize(this);
        damageHP = new(e, nameof(damageHP));
        missileSpeed = new(e, nameof(missileSpeed));
        lifetime = new(e, nameof(lifetime));
        maxHP = new(e, nameof(maxHP));
    }
    public static FragmentMod Sum(params FragmentMod[] mods) {
        FragmentMod result = new();
        foreach (var m in mods) result += m;
        return result;
    }
    public static FragmentMod operator +(FragmentMod x, FragmentMod y) =>
        y == null ? x : 
        new() {
            curse = y.curse,
            damageHP = x.damageHP + y.damageHP,
            missileSpeed=x.missileSpeed + y.missileSpeed,
            lifetime = x.lifetime + y.lifetime,
            maxHP=x.maxHP + y.maxHP
        };
    public static FragmentDesc operator *(FragmentMod x, FragmentDesc y) =>
        y with {
            damageHP = x.damageHP.Modify(y.damageHP),
            missileSpeed = x.missileSpeed.Modify(y.missileSpeed),
            lifetime = x.lifetime.Modify(y.lifetime),
        };
    public void ModifyRemoval(ref bool removable) {
        if (curse) {
            removable = false;
        }
    }
}
public record IntMod(int inc = 0, double factor = 1) {
    public static IntMod EMPTY = new IntMod();
    public IntMod(XElement e, string name) : this(
        e.TryAttInt($"{name}Inc", 0),
        e.TryAttDouble($"{name}Factor", 1)
        ) {
    }
    public int Modify(int n) => (int)((n * factor) + inc);
    public IDice Modify(IDice n) => IDice.Apply(n, factor, inc);
    public static IntMod operator +(IntMod x, IntMod y) =>
        x == null ? y :
        new() {
            inc = x.inc + y.inc,
            factor = x.factor * y.factor
        };
}