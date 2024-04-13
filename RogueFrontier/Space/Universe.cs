using Common;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RogueFrontier;

public class UniverseDesc {
    public List<SystemDesc> systems=new();
    public List<LinkDesc> links=new();
    public UniverseDesc(TypeCollection tc, XElement e) {
        if (e.HasElement("Topology", out var xmlTopology)) {
            foreach (var element in xmlTopology.Elements()) {
                switch (element.Name.LocalName) {
                    case "System":
                        systems.Add(new(tc, element));
                        break;
                    case "Link":
                        links.Add(new(element));
                        break;
                }
            }
        }
        if (e.HasElement("Links", out var xmlLinks)) {
            links.AddRange(xmlLinks.Elements().Select(e => new LinkDesc(e)));
        }
    }

    public record SystemDesc {
        [Req] public string id;
        [Req] public string name;
        [Req] public string codename;
        [Opt] public int x, y;
        public SystemGroup systemGroup;

        public List<GlobalStargateDesc> globalStargates;

        public SystemDesc() { }
        public SystemDesc(TypeCollection tc, XElement e) {
            e.Initialize(this);
            if (e.HasElement("SystemGroup", out var xmlSystemGroup)) {
                systemGroup = new(xmlSystemGroup, SGenerator.ParseFrom(tc, SSystemElement.Create));
            }
            globalStargates = new(e.Elements("GlobalStargate").Select(g => new GlobalStargateDesc(g)));
        }
    }
    public record GlobalStargateDesc {
        [Req] public string globalId;
        [Req] public string gateId;
        public GlobalStargateDesc() { }
        public GlobalStargateDesc(XElement e) {
            e.Initialize(this);
        }
    }
    public record LinkDesc {
        [Req] public string fromGateId;
        [Req] public string toGateId;
        public LinkDesc() { }
        public LinkDesc(XElement e) {
            e.Initialize(this);
        }
    }
}
public class Universe : Ob<EntityAdded> {
    public void Observe(EntityAdded ev) =>
        onEntityAdded.Observe(ev);
    public Rand karma;
    public TypeCollection types;

    public Dictionary<string, Entity> identifiedObjects=new();
    public Dictionary<string, System> systems=new();
    public Dictionary<string, Stargate> stargates=new();
    public Dictionary<string, HashSet<Stargate>> systemGates=new();
    public Dictionary<string, (int, int)> grid = new();
    public Vi<EntityAdded> onEntityAdded = new();
    public Universe(TypeCollection types = null, Rand karma = null) {
        this.types = types ?? new TypeCollection();
        this.karma = karma ?? new Rand();
    }
    public Universe(UniverseDesc desc, TypeCollection types = null, Rand karma = null) : this(types, karma) {
        foreach (var entry in desc.systems) {
            var s = new System(this) { id = entry.id, name = entry.name };
            s.onEntityAdded += this;
            systems[entry.id] = s;
            grid[s.id] = (entry.x, entry.y);

            types.Lookup<SystemType>(entry.codename).Generate(s);
            s.UpdatePresent();

            //Record all the system stargates
            var gates = s.entities.all.OfType<Stargate>();
            systemGates[s.id] = new(gates);
            foreach (var g in gates) {
                stargates[$"{entry.id}:{g.gateId}"] = g;
            }
        }
        //Record any global stargates
        foreach (var s in desc.systems) {
            var sys = systems[s.id];
            Dictionary<string, Stargate> gateLookup = new();
            foreach (var g in sys.entities.all.OfType<Stargate>()) {
                gateLookup[g.gateId] = g;
            }

            foreach (var g in s.globalStargates) {
                stargates[g.globalId] = gateLookup[g.gateId];
            }
        }
        //Build all local links
        foreach (var s in desc.systems) {
            var sys = systems[s.id];
            foreach (var g in sys.entities.all.OfType<Stargate>()) {
                if (g.destGateId.Any()) {
                    g.destGate = stargates[g.destGateId];
                }
            }
        }
        //Build links
        foreach (var l in desc.links) {
            var fromGate = stargates[l.fromGateId];
            var toGate = stargates[l.toGateId];
            fromGate.destGate = toGate;
            toGate.destGate = fromGate;
        }

        var _ = FindGateTo(systems.Values.First(), systems.Values.Last());
    }
    public Stargate FindGateTo(System from, System to) {
        Dictionary<System, Stargate> gateTo = new();
        HashSet<System> visited = new();
        visited.Add(from);
        Queue<System> q = new();
        q.Enqueue(from);
        while (q.Any()) {
            var top = q.Dequeue();

            foreach (var g in systemGates[top.id].Where(g => g.destGate != null)) {
                if (visited.Add(g.destGate.world)) {
                    gateTo[g.destGate.world] = g;
                    q.Enqueue(g.destGate.world);
                }
            }
            if (top == to) {
                var g = gateTo[to];
                while (g.world != from) {
                    g = gateTo[g.world];
                }
                return g;
            }
        }
        return null;
    }
    public IEnumerable<Entity> GetAllEntities() =>
        systems.Values.SelectMany(s => s.entities.all);
}
