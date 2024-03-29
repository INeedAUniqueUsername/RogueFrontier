using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RogueFrontier;
public record Modifier() {
    [Opt] public bool
        curse = false;

    public IntMod damageHP, missileSpeed, lifetime, maxHP;
    public Modifier(XElement e) : this() {
        e.Initialize(this);
        damageHP = new(e, nameof(damageHP));
        missileSpeed = new(e, nameof(missileSpeed));
        lifetime = new(e, nameof(lifetime));
        maxHP = new(e, nameof(maxHP));
    }
    public static Modifier Sum(params Modifier[] mods) {
        Modifier result = new();
        foreach (var m in mods) result += m;
        return result;
    }
    public static Modifier operator +(Modifier x, Modifier y) =>
        y == null ? x : 
        new() {
            curse = y.curse,
            damageHP = x.damageHP + y.damageHP,
            missileSpeed=x.missileSpeed + y.missileSpeed,
            lifetime = x.lifetime + y.lifetime,
            maxHP=x.maxHP + y.maxHP
        };
    public static FragmentDesc operator *(Modifier x, FragmentDesc y) =>
        y with {
            damageHP = x.damageHP?.Modify(y.damageHP) ?? y.damageHP,
            missileSpeed = x.missileSpeed?.Modify(y.missileSpeed) ?? y.missileSpeed,
            lifetime = x.lifetime?.Modify(y.lifetime) ?? y.lifetime,
        };
    public static ArmorDesc operator *(Modifier x, ArmorDesc y) =>
        y with {
            maxHP = x.maxHP.Modify(y.maxHP),
        };
    public bool empty => this is Modifier {
        curse: false,
        damageHP: { factor:1, inc: 0},
        missileSpeed: { factor:1, inc: 0},
        lifetime: { factor:1, inc: 0},
        maxHP: { factor:1, inc: 0},
    };
    public void ModifyRemoval(ref bool removable) {
        if (curse) {
            removable = false;
        }
    }
}
public record IntMod(int inc = 0, double factor = 1) {
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