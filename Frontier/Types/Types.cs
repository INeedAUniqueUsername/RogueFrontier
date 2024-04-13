using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
namespace RogueFrontier;
public class TypeCollection {
    [JsonProperty]
    private Dictionary<string, XElement> sources=new();
    [JsonProperty]
    public Dictionary<string, IDesignType> all=new();
    public HashSet<string> initialized = new();
    [JsonProperty]
    private Dictionary<Type, object> dicts = new() {
        [typeof(GenomeType)] = new Dictionary<string, GenomeType>(),
        [typeof(ImageType)] = new Dictionary<string, ImageType>(),
        [typeof(ItemType)] = new Dictionary<string, ItemType>(),
        [typeof(PowerType)] = new Dictionary<string, PowerType>(),
        [typeof(ShipClass)] = new Dictionary<string, ShipClass>(),
        [typeof(Sovereign)] = new Dictionary<string, Sovereign>(),
        [typeof(StationType)] = new Dictionary<string, StationType>(),
        [typeof(SystemType)] = new Dictionary<string, SystemType>(),
        [typeof(TradeDesc)] = new Dictionary<string, TradeDesc>()
    };
    enum InitState {
        InitializePending,
        Initializing,
        Initialized
    }
    InitState state;
    //After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
    public TypeCollection() {
        state = InitState.InitializePending;
        Debug.Print("TypeCollection created");
    }
    public TypeCollection(params string[] modules) : this() {
        LoadFile(modules);
    }
    public TypeCollection(params XElement[] modules) : this() {
        //We do two passes
        //The first pass creates DesignType references for each type and stores the source code
        foreach (var m in modules) {
            ProcessRoot("", m);
        }
        //The second pass initializes each type from the source code
        Initialize();
    }
    void Initialize() {
        state = InitState.Initializing;
        //We don't evaluate all sources; just the ones that are used by DesignTypes
        foreach (string key in all.Keys.ToList()) {
            if (initialized.Contains(key)) {
                continue;
            }
            InitializeType(key);
        }
        state = InitState.Initialized;
    }
    public void InitializeType(string key) {
        IDesignType type = all[key];
        XElement source = sources[key];
        type.Initialize(this, source);
        initialized.Add(key);
    }
    public void LoadFile(params string[] modules) {
        foreach (var m in modules) {
            ProcessRoot(m, XElement.Parse(File.ReadAllText(m)));
        }
        if (state == InitState.InitializePending) {
            //We do two passes
            //The first pass creates DesignType references for each type and stores the source code

            //The second pass initializes each type from the source code
            Initialize();
        }
    }
    void ProcessRoot(string file, XElement root) {
        foreach (var element in root.Elements()) {
            ProcessElement(file, element);
        }
    }
    public void ProcessElement(string file, XElement element) {
        void ProcessSection(XElement e) => ProcessRoot(file, e);
        Action<XElement> a = element.Name.LocalName switch {
            "Module" => e => {
                var subfile = Path.Combine(Directory.GetParent(file).FullName, e.ExpectAtt("file"));
                XElement module = XDocument.Load(subfile).Root;
                ProcessRoot(file, module);
            },
            "Source" => AddSource,
            "Content" => ProcessSection,
            "Unused" => e => { },
#if DEBUG
            "Debug" => ProcessSection,
#else
            "Debug" => e => { },       
#endif
            "GenomeType" => AddType<GenomeType>,
            "ImageType" => AddType<ImageType>,
            "ItemType" => AddType<ItemType>,
            "PowerType" => AddType<PowerType>,
            "ShipClass" => AddType<ShipClass>,
            "StationType" => AddType<StationType>,
            "Sovereign" => AddType<Sovereign>,
            "SystemType" => AddType<SystemType>,
            "TradeDesc" => AddType<TradeDesc>,
            _ => throw new Exception($"Unknown element <{element.Name}>")
        };
        a(element);
    }
    void AddSource(XElement element) {
        if (!element.TryAtt("codename", out string type)) {
            throw new Exception("DesignType requires codename attribute");
        } else if (sources.ContainsKey(type)) {
            throw new Exception($"DesignType type conflict: {type}");
        }
        Debug.Print($"Created Source <{element.Name}> of type {type}");
        sources[type] = element;
    }
    public Dictionary<string, T> GetDict<T>() where T: IDesignType =>
        (Dictionary<string, T>) dicts[typeof(T)];
    public Dictionary<string, T>.ValueCollection Get<T>() where T : IDesignType =>
        GetDict<T>().Values;
    void AddType<T>(XElement element) where T : IDesignType, new() {
        if (!element.TryAtt("codename", out string type)) {
            throw new Exception("DesignType requires codename attribute");
        } else if (sources.ContainsKey(type)) {
            throw new Exception($"DesignType type conflict: {type}");
        }
        Debug.Print($"Created <{element.Name}> of type {type}");
        sources[type] = element;
        T t = new();
        all[type] = t;
        ((Dictionary<string, T>)dicts[typeof(T)])[type] = t;
        Init:
        //If we're uninitialized, then we will definitely initialize later
        if (state != InitState.InitializePending) {
            //Otherwise, initialize now
            all[type].Initialize(this, sources[type]);
        }
    }
    public bool TryLookup(string codename, out IDesignType result) =>
        all.TryGetValue(codename, out result);
    public bool TryLookup<T>(string type, out T result) where T : class, IDesignType =>
        (result = Lookup<T>(type)) != null;
    public IDesignType Lookup(string codename) {
        if (codename == null || codename.Trim().Length == 0) {
            throw new Exception($"Must specify a codename");
        }
        if(all.TryGetValue(codename, out var result)) {
            return result;
        }
        throw new Exception($"Unknown type {codename}");
    }


    public T Lookup<T>(string codename) where T : class, IDesignType {
        var result = Lookup(codename);
        if (!initialized.Contains(codename)) {
            result.Initialize(this, sources[codename]);
        }
        return result as T ??
            throw new Exception($"Type {codename} is <{result.GetType().Name}>, not <{nameof(T)}>");
    }
}
public interface IDesignType {
    void Initialize(TypeCollection collection, XElement e);
}
public interface ITile {
    public ColoredGlyph Original { get; }
    void Update() { }
}
public record StaticTile() : ITile {
    [JsonIgnore]
    public ColoredGlyph Original => new (foreground, background, glyph);
    [JsonProperty]
    [Opt] public Color foreground;
    [JsonProperty]
    [Opt] public Color background;
    [JsonProperty]
    [Req] public char glyph;
    public StaticTile(XElement e) : this() {
        e.Initialize(this);
    }
    public StaticTile(ColoredGlyph cg) : this() =>
        (foreground, background, glyph) = (cg.Foreground, cg.Background, cg.GlyphCharacter);
    public StaticTile(char c) : this() =>
        (foreground, background, glyph) = (Color.White, Color.Black, c);

    public StaticTile(char c, Color foreground, Color background) : this() =>
        (this.foreground, this.background, glyph) = (foreground, background, c);
    public StaticTile(char c, string foreground, string background) : this() {
        this.foreground=(Color)typeof(Color).GetField(foreground).GetValue(null);
        this.background=(Color)typeof(Color).GetField(background).GetValue(null);
        this.glyph = c;
    }
    public static implicit operator ColoredGlyph(StaticTile t) => t.Original;
    public static implicit operator StaticTile(ColoredGlyph cg) => new StaticTile(cg);
}
public record AlphaTile() : ITile {
    [JsonIgnore]
    public ColoredGlyph Original => new(foreground, background, glyph);
    [JsonProperty]
    [Opt] public Color foreground;
    [JsonProperty]
    [Opt] public Color background;
    [JsonProperty]
    [Req] public int glyph;
    [JsonIgnore]
    private double alpha => alphaRange * Math.Sin(ticks * 2 * Math.PI / cycle);
    [JsonIgnore]
    public ColoredGlyph Glyph => new ColoredGlyph(
        foreground.SetAlpha((byte)(foreground.A + alpha)),
        background.SetAlpha((byte)(background.A + alpha)),
        glyph);
    int cycle;
    int alphaRange;
    int ticks = 0;
    public AlphaTile(ColoredGlyph cg) : this() =>
        (foreground, background, glyph) = (cg.Foreground, cg.Background, cg.Glyph);
    public void Update() =>
        ticks++;
    public static implicit operator ColoredGlyph(AlphaTile t) => t.Original;
}
