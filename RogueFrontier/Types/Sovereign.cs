using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using static RogueFrontier.BasicAlignment;
using static RogueFrontier.Disposition;
namespace RogueFrontier;

public enum BasicAlignment {
    ConstructiveOrder, ConstructiveChaos, Neutral, DestructiveOrder, DestructiveChaos
}

public record ComplexDisposition {
    bool canAttack;
    bool waitUntilAttacked;
}
public class Sovereign : IDesignType {
    public class AutoEnemySelf : Lis<AutoSovereign> {
        public Sovereign self;
        public AutoEnemySelf(Sovereign self) {
            this.self = self;
        }
        [JsonIgnore]
        public AutoSovereign Value => s => s == self ? Disposition.Friend : Disposition.Enemy;
    }
    public class AutoEnemy : Lis<AutoSovereign> {
        [JsonIgnore]
        public AutoSovereign Value => s => Disposition.Enemy;
    }
    public class AutoNeutral : Lis<AutoSovereign> {
        [JsonIgnore]
        public AutoSovereign Value => s => Disposition.Neutral;
    }

    public static readonly Sovereign Inanimate = new Sovereign() {
        codename = "sovereign_inanimate",
        AutoSovereignDisposition = new AutoNeutral()
    };
    public static readonly Sovereign Gladiator = new Sovereign() {
        codename = "sovereign_gladiator",
        AutoSovereignDisposition = new AutoEnemy(),
    };
    public static Sovereign SelfOnly {
        get {
            Sovereign s = new Sovereign() {
                codename = "sovereign_self",
            };
            s.AutoSovereignDisposition = new AutoEnemySelf(s);
            return s;
        }
    }



    public string codename;
    public BasicAlignment alignment;
    //private Sovereign parent;

    public Dict<string, Disposition> sovDispositions;
    public Dict<ulong, Disposition> entityDispositions;
    public Lis<AutoSovereign> AutoSovereignDisposition;

    public static readonly Dictionary<BasicAlignment, Dictionary<BasicAlignment, Disposition>> dispositionTable = new Dictionary<BasicAlignment, Dictionary<BasicAlignment, Disposition>> {
            { ConstructiveOrder, new Dictionary<BasicAlignment, Disposition>{
                {ConstructiveOrder, Friend },
                {ConstructiveChaos, Disposition.Neutral },
                {BasicAlignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy },
            }}, {ConstructiveChaos, new Dictionary<BasicAlignment, Disposition>{
                {ConstructiveOrder, Disposition.Neutral },
                {ConstructiveChaos, Disposition.Friend },
                {BasicAlignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy }
            }}, {BasicAlignment.Neutral, new Dictionary<BasicAlignment, Disposition> {
                {ConstructiveOrder, Disposition.Neutral },
                {ConstructiveChaos, Disposition.Neutral },
                {BasicAlignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Disposition.Neutral },
                {DestructiveChaos, Disposition.Enemy }
            }}, {DestructiveOrder, new Dictionary<BasicAlignment, Disposition>{
                {ConstructiveOrder, Enemy },
                {ConstructiveChaos, Enemy },
                {BasicAlignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Disposition.Neutral },
                {DestructiveChaos, Enemy },
            }}, {DestructiveChaos, new Dictionary<BasicAlignment, Disposition>{
                {ConstructiveOrder, Enemy },
                {ConstructiveChaos, Enemy },
                {BasicAlignment.Neutral, Enemy },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy },
            }},
        };

    public delegate Disposition AutoSovereign(Sovereign other);
    public delegate Disposition AutoSpaceObject(ActiveObject other);

    public Sovereign() {
        sovDispositions = new();
        entityDispositions = new();
    }
    public void Initialize(TypeCollection tc, XElement e) {
        codename = e.ExpectAtt("codename");
        if (Enum.TryParse<BasicAlignment>(e.ExpectAtt("alignment"), out BasicAlignment alignment)) {
            this.alignment = alignment;
        } else {
            throw new Exception($"Invalid alignment value {e.ExpectAtt("alignment")}");
        }

        if (e.HasElement("Relations", out var xmlRelations)) {
            foreach (var xmlRel in xmlRelations.Elements()) {
                var other = xmlRel.ExpectAtt("codename");
                var disposition = xmlRel.ExpectAttEnum<Disposition>("disposition");
                var mutual = xmlRel.ExpectAttBool("mutual");

                var sov = tc.Lookup<Sovereign>(other);
                sovDispositions[sov.codename] = disposition;
                if (mutual) {
                    sov.sovDispositions[codename] = disposition;
                }
            }
        }
    }
    public void SetDisposition(Sovereign other, Disposition d) => sovDispositions[other.codename] = d;
    public void SetDisposition(ActiveObject other, Disposition d) => entityDispositions[other.id] = d;
    public Disposition GetDisposition(Sovereign other) {
        if (sovDispositions.TryGetValue(other.codename, out Disposition d)
            //|| (parent?.sovDispositions.TryGetValue(other, out d) == true)
            ) {
            return d;
        } else {
            sovDispositions[other.codename] = GetAutoDisposition(other);
            return sovDispositions[other.codename];
        }
    }
    public bool IsEnemy(Sovereign other) => GetDisposition(other) == Disposition.Enemy;
    public Disposition GetDisposition(ActiveObject other) {
        if (entityDispositions.TryGetValue(other.id, out Disposition d)
            //|| (parent?.entityDispositions.TryGetValue(other, out d) == true)
            ) {
            return d;
        }
        if (other.sovereign != null) {
            return GetDisposition(other.sovereign);
        }

        throw new Exception("Unknown sovereign");
        //entityDispositions[other] = GetAutoDisposition(other.sovereign);
        //return entityDispositions[other];
    }
    public bool IsFriend(ActiveObject other) => GetDisposition(other) == Friend;
    public bool IsEnemy(ActiveObject other) => GetDisposition(other) == Enemy;
    public Disposition GetAutoDisposition(Sovereign other) {
        var d = AutoSovereignDisposition?.Value?.Invoke(other);
        if (d.HasValue) {
            return d.Value;
        } else {
            //Initialize from default values given our alignments
            return dispositionTable[alignment][other.alignment];
        }
    }
}
public enum Disposition {
    Neutral,
    Friend,
    Enemy
}
