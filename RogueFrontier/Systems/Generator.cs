using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public delegate T Parse<T>(XElement e);
public interface ShipGenerator {
    IEnumerable<AIShip> Generate(TypeCollection tc, ActiveObject owner);
    public IEnumerable<AIShip> GenerateAndPlace(TypeCollection tc, ActiveObject owner) {
        var w = owner.world;
        var result = Generate(tc, owner);
        foreach(var s in result) {
            w.AddEntity(s);
            w.AddEffect(new Heading(s));
        }
        return result;
    }
}
public class ShipGroup : ShipGenerator {
    public List<ShipGenerator> generators;
    public ShipGroup() {
        generators = new List<ShipGenerator>();
    }
    public ShipGroup(XElement e, Parse<ShipGenerator> parse) {
        generators = new();
        foreach (var element in e.Elements()) {
            generators.Add(parse(element));
        }
    }
    public IEnumerable<AIShip> Generate(TypeCollection tc, ActiveObject owner) =>
        generators.SelectMany(g => g.Generate(tc, owner));
}
public enum ShipOrder {
    attack, escort, guard, patrol, patrolCircuit, 
}
public class ShipEntry : ShipGenerator {
    [Opt] public IDice count = new Constant(1);
    [Opt] public string id = "";
    [Opt(alias = "Ships", parse = false)] public ShipGroup subordinates = new();
    [Req(alias = "codename", parse = false)] public ShipClass shipClass;
    [Opt(parse = false)] public Sovereign sovereign;
    [Par(construct = false)] public IShipOrderDesc orderDesc;
    [Opt] public EShipBehavior behavior;
    public ShipEntry() { }
    public ShipEntry(TypeCollection tc, XElement e) {
        e.Initialize(this, transform: new() {
            [nameof(sovereign)] = (string s) => tc.Lookup<Sovereign>(s),
            [nameof(shipClass)] = (string s) => tc.Lookup<ShipClass>(s),
            [nameof(subordinates)] = (XElement xmlSub) => new ShipGroup(xmlSub, SGenerator.ParseFrom(tc, SGenerator.ShipFrom)),
            [nameof(orderDesc)] = (XElement e) => IShipOrderDesc.Get(e)
        });
    }
    IShipBehavior GetBehavior() =>
        behavior switch {
            EShipBehavior.none => null,
            EShipBehavior.trader => new Merchant()
        };
    public IEnumerable<AIShip> Generate(TypeCollection tc, ActiveObject owner) {
        Sovereign s = sovereign ?? owner.sovereign ?? throw new Exception("Sovereign expected");
        var count = this.count.Roll();
        Func<int, XY> GetPos = orderDesc switch {
            PatrolOrbitDesc pod => i => owner.position + XY.Polar(
                                        Math.PI * 2 * i / count,
                                        pod.patrolRadius),
            _ => i => owner.position
        };
        var ships = Enumerable.Range(0, count).Select(
            i => new AIShip(new(owner.world, shipClass, GetPos(i)), s, GetBehavior(), orderDesc.Value(owner))
            ).ToList();
        if (id.Any()) {
            if(count != 1) {
                throw new Exception("Ship entry with id must have exactly one ship");
            }
            owner.world.universe.identifiedObjects[id] = ships.First();
        }
        var subShips = ships.SelectMany(ship => subordinates.Generate(tc, ship));
        return ships.Concat(subShips);
    }
    public interface IShipOrderDesc : Lis<IShipOrder.Create> {
        public static IShipOrderDesc Get(XElement e) => e.TryAttEnum("order", ShipOrder.guard) switch {
            ShipOrder.attack => new AttackDesc(e),
            ShipOrder.escort => new EscortDesc(e),
            ShipOrder.guard => new GuardDesc(e),
            ShipOrder.patrol => new PatrolOrbitDesc(e),
            ShipOrder.patrolCircuit => new PatrolCircuitDesc(e),
            _ => new GuardDesc(e)
        };
    }
    public record AttackDesc() : IShipOrderDesc {
        [Opt] public string targetId = "";
        public AttackDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore] public IShipOrder.Create Value => target => new AttackTarget(
            targetId.Any() ? (ActiveObject)target.world.universe.identifiedObjects[targetId] : target
            );
    }
    public record GuardDesc : IShipOrderDesc {
        public GuardDesc(XElement e) {
            e.Initialize(this);
        }
        [JsonIgnore] public IShipOrder.Create Value => target => new GuardAt(target);
    }
    public record PatrolOrbitDesc() : IShipOrderDesc {
        [Req] public int patrolRadius;
        public PatrolOrbitDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore] public IShipOrder.Create Value => target => new PatrolAt(target, patrolRadius);
    }
    //Patrol an entire cluster of stations (moving out to 50 ls + radius of nearest station)
    public record PatrolCircuitDesc() : IShipOrderDesc {
        [Req] public int patrolRadius;
        public PatrolCircuitDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore] public IShipOrder.Create Value => target => new PatrolAround(target, patrolRadius);
    }
    public record EscortDesc() : IShipOrderDesc {
        public EscortDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore] public IShipOrder.Create Value => target => new EscortShip((IShip)target, XY.Polar(0, 2));
    }
}
public record ModRoll() {
    [Opt] public double modifierChance = 1;
    [Par] public Modifier modifier;
    public ModRoll(XElement e) : this() {
        e.Initialize(this);
        if (modifier.empty) {
            modifier = null;
        }
    }
    public Modifier Generate() {
        if (modifier == null) {
            return null;
        }
        if(new Rand().NextDouble() <= modifierChance) {
            return modifier;
        }
        return null;
    }
}
public interface IGenerator<T> {
    List<T> Generate(TypeCollection t);

}
public record None<T>() : IGenerator<T> {
    public None(XElement e) : this() { }
    public List<T> Generate(TypeCollection tc) => new();
}
public static class SGenerator {
    public static Parse<T> ParseFrom<T>(TypeCollection tc, Func<TypeCollection, XElement, T> f) =>
        (XElement e) => f(tc, e);
    public static ShipGenerator ShipFrom(TypeCollection tc, XElement element) {
        var f = ParseFrom(tc, ShipFrom);
        return element.Name.LocalName switch {
            "Ship" => new ShipEntry(tc, element),
            "Ships" => new ShipGroup(element, f),
            _ => throw new Exception($"Unknown <Ships> subelement {element.Name}")
        };
    }
    public static IGenerator<Item> ItemFrom(TypeCollection tc, XElement element) {
        var f = ParseFrom(tc, ItemFrom);
        return element.Name.LocalName switch {
            "Item" => new ItemEntry(tc, element),
            "Items" => new Group<Item>(element, f),
            "ItemGroup" => new Group<Item>(element, f),
            "ItemTable" => new Table<Item>(element, f),
            "None" => new None<Item>(),
            _ => throw new Exception($"Unknown ItemGenerator subelement {element.Name}")
        };
    }
    public static IGenerator<Device> DeviceFrom(XElement element) {
        var f = (Parse<IGenerator<Device>>)DeviceFrom;
        return element.Name.LocalName switch {
            "Weapon" => new WeaponEntry(element),
            "Shield" => new ShieldEntry(element),
            "Reactor" => new ReactorEntry(element),
            "Solar" => new SolarEntry(element),
            "Service" => new ServiceEntry(element),
            "Devices" => new Group<Device>(element, f),
            "DeviceGroup" => new Group<Device>(element, f),
            "DeviceTable" => new Table<Device>(element, f),
            "None" => new None<Device>(),
            _ => throw new Exception($"Unknown DeviceGenerator subelement {element.Name}")
        };
    }
    public static IGenerator<Weapon> WeaponFrom(XElement element) {
        var f = (Parse<IGenerator<Weapon>>)WeaponFrom;
        return element.Name.LocalName switch {
            "Weapon" => new WeaponEntry(element),
            "Weapons" => new Group<Weapon>(element, f),
            "WeaponGroup" => new Group<Weapon>(element, f),
            "WeaponTable" => new Table<Weapon>(element, f),
            "None" => new None<Weapon>(),
            _ => throw new Exception($"Unknown WeaponGenerator subelement {element.Name}")
        };
    }
    public static IGenerator<Armor> ArmorFrom(XElement element) {
        var f = (Parse<IGenerator<Armor>>)ArmorFrom;
        return element.Name.LocalName switch {
            "Armor" => new ArmorEntry(element),
            "Armors" => new Group<Armor>(element, f),
            "ArmorGroup" => new Group<Armor>(element, f),
            "ArmorTable" => new Table<Armor>(element, f),
            "None" => new None<Armor>(),
            _ => throw new Exception($"Unknown ArmorGenerator subelement {element.Name}")
        };
    }
}
public record Group<T>() : IGenerator<T> {
    public List<IGenerator<T>> generators=new();
    public static List<T> From(TypeCollection tc, Parse<IGenerator<T>> parse, string str) => new Group<T>(XElement.Parse(str), parse).Generate(tc);
    public Group(XElement e, Parse<IGenerator<T>> parse) : this() {
        generators = new();
        foreach (var element in e.Elements()) {
            generators.Add(parse(element));
        }
    }
    public List<T> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}
public record Table<T>() : IGenerator<T> {
    [Opt] public IDice count = new Constant(1);
    [Opt] public bool replacement = true;
    public List<(double chance, IGenerator<T>)> generators;
    private double totalChance;
    public static List<T> From(TypeCollection tc, Parse<IGenerator<T>> parse, string str) => new Table<T>(XElement.Parse(str), parse).Generate(tc);
    public Table(XElement e, Parse<IGenerator<T>> parse) : this() {
        e.Initialize(this);
        generators = new();
        foreach (var element in e.Elements()) {
            var chance = element.ExpectAttDouble("chance");
            generators.Add((chance, parse(element)));
            totalChance += chance;
        }
    }
    public List<T> Generate(TypeCollection tc) {
        if (replacement) {
            return new(Enumerable.Range(0, count.Roll()).SelectMany(i => {
                var c = new Random().NextDouble() * totalChance;
                foreach ((var chance, var g) in generators) {
                    if (c < chance) {
                        return g.Generate(tc);
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            }));
        } else {
            List<(double chance, IGenerator<T>)> choicesLeft;
            double totalChanceLeft;
            ResetTable();
            return new(Enumerable.Range(0, count.Roll()).SelectMany(i => {
                if(totalChanceLeft > 0) {
                    ResetTable();
                }
                var c = new Random().NextDouble() * totalChanceLeft;
                for(int j = 0; j < choicesLeft.Count; j++) {
                    (var chance, var g) = generators[j];
                    if (c < chance) {
                        generators.RemoveAt(j);
                        totalChanceLeft -= chance;
                        return g.Generate(tc);
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            }));
            void ResetTable() {
                choicesLeft = new(generators);
                totalChanceLeft = totalChance;
            }

        }
    }
}
public record ItemEntry() : IGenerator<Item> {
    
    [Opt] public IDice count = new Constant(1);
    [Req(alias = "codename", parse = false)]
          public ItemType type;
    [Par]public ModRoll mod;
    public ItemEntry(TypeCollection tc, XElement e) : this() {
        e.Initialize(this, transform: new() {
            [nameof(type)] = (string s) => tc.Lookup<ItemType>(s)
        });
    }
    public List<Item> Generate(TypeCollection tc) =>
        new(Enumerable.Range(0, count.Roll()).Select(_ => new Item(type, mod.Generate())));
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) =>
        tc.Lookup<ItemType>(type.codename);
}
public record ArmorEntry() : IGenerator<Armor> {
    [Req] public string codename;
    [Par] public ModRoll mod;
    public ArmorEntry(XElement e) : this() {
        e.Initialize(this);
    }
    List<Armor> IGenerator<Armor>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    public Armor Generate(TypeCollection tc) =>
        SDevice.Generate<Armor>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) =>
        Generate(tc);

}
public static class SDevice {
    private static T Install<T>(TypeCollection tc, string codename, ModRoll mod) where T : class, Device =>
        new Item(tc.Lookup<ItemType>(codename), mod.Generate()).Get<T>();
    public static T Generate<T>(TypeCollection tc, string codename, ModRoll mod) where T : class, Device =>
        Install<T>(tc, codename, mod) ??
            throw new Exception($"Expected <ItemType> type with <{typeof(T).Name}> desc: {codename}");
}
public record ReactorEntry() : IGenerator<Device> {
    [Req] public string codename;
    [Par] public ModRoll mod;
    public ReactorEntry(XElement e) : this() {
        e.Initialize(this);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Reactor Generate(TypeCollection tc) =>
        SDevice.Generate<Reactor>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}

public record SolarEntry() : IGenerator<Device> {
    [Req] public string codename;
    [Par]public ModRoll mod;
    public SolarEntry(XElement e) : this() {
        e.Initialize(this);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Solar Generate(TypeCollection tc) =>
        SDevice.Generate<Solar>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}
public record ServiceEntry() : IGenerator<Device> {
    [Req] public string codename;
    [Par]public ModRoll mod;
    public ServiceEntry(XElement e) : this() {
        e.Initialize(this);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Service Generate(TypeCollection tc) => SDevice.Generate<Service>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}

public record ShieldEntry() : IGenerator<Device>, IGenerator<Shield> {
    [Req] public string codename;
    [Par]public ModRoll mod;
    public ShieldEntry(XElement e) : this() {
        e.Initialize(this);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    List<Shield> IGenerator<Shield>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Shield Generate(TypeCollection tc) =>
        SDevice.Generate<Shield>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}
public record WeaponList() : IGenerator<Weapon> {
    public List<IGenerator<Weapon>> generators;
    public WeaponList(XElement e) : this() {
        generators = new List<IGenerator<Weapon>>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Weapon":
                    generators.Add(new WeaponEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Weapons> subelement {element.Name}");
            }
        }
    }
    public List<Weapon> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}
public record WeaponEntry() : IGenerator<Device>, IGenerator<Weapon> {
    [Req] public string codename;
    [Opt] public bool omnidirectional;
    [Opt] public bool? structural = null;
    [Opt] public double angle, leftRange, rightRange;
    [Par(construct = false)] public XY offset;
    [Par] public ModRoll mod;
    public WeaponEntry(XElement e) : this() {
        var toRad = (double d) => d * Math.PI / 180;
        e.Initialize(this, transform: new() {
            [nameof(angle)] = toRad,
            [nameof(leftRange)] = toRad,
            [nameof(rightRange)] = toRad,
            [nameof(offset)] = (XElement e) => XY.TryParse(e, new(0, 0))
        });
    }
    List<Weapon> IGenerator<Weapon>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Weapon Generate(TypeCollection tc) {
        
        
        var w = SDevice.Generate<Weapon>(tc, codename, mod);
        w.aiming =
            omnidirectional ?
                new Omnidirectional() :
            leftRange + rightRange > 0 ?
                new Swivel(leftRange, rightRange) :
            w.aiming;
        w.targeting = w.targeting ?? (w.aiming switch {
            Omnidirectional => new Targeting(false),
            Swivel => new Targeting(true),
            _ => null
        });
    w.structural = structural ?? w.structural;
        w.angle = angle;
        w.offset = offset;
        return w;
    }
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}