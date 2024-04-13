using ASECII;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SadConsole;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using static SadConsole.ColoredString;
using SadRogue.Primitives;

namespace RogueFrontier;

//https://stackoverflow.com/a/18548894
class WritablePropertiesOnlyResolver : DefaultContractResolver {
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
        IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
        return props.Where(p => p.Writable).ToList();
    }
}
//https://stackoverflow.com/a/61549273
public class DictionaryAsArrayResolver : DefaultContractResolver {
    protected override JsonContract CreateContract(Type objectType) {
        if (IsDictionary(objectType)) {
            JsonArrayContract contract = base.CreateArrayContract(objectType);
            contract.OverrideCreator = (args) => CreateInstance(objectType);
            return contract;
        }

        return base.CreateContract(objectType);
    }

    internal static bool IsDictionary(Type objectType) {
        if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
            return true;
        }

        if (objectType.GetInterface(typeof(IDictionary<,>).Name) != null) {
            return true;
        }

        return false;
    }
    private object CreateInstance(Type objectType) {
        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(objectType.GetGenericArguments());
        return Activator.CreateInstance(dictionaryType);
    }
}
public class ColoredStringConverter : TypeConverter {
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        ColoredString cs = value as ColoredString;
        List<uint> elements = new List<uint>();
        foreach (var c in cs) {
            elements.Add(c.Foreground.PackedValue);
            elements.Add(c.Background.PackedValue);
            elements.Add((uint)c.Glyph);
        }
        return string.Join(',', elements);
    }
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        var elements = Convert.ToString(value).Trim('(', ')').Split(',', StringSplitOptions.RemoveEmptyEntries);

        ColoredString result = new ColoredString(elements.Length / 3);
        for (int i = 0; i + 2 < elements.Length; i += 3) {

            result[i / 3] = new ColoredGlyphAndEffect() {
                Foreground = new Color(uint.Parse(elements[i])),
                Background = new Color(uint.Parse(elements[i + 1])),
                Glyph = (int)uint.Parse(elements[i + 2])
            };
        }
        return result;
    }
}

public class ColoredGlyphConverter : TypeConverter {
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        ColoredGlyph c = value as ColoredGlyph;
        var elements = new List<uint> { c.Foreground.PackedValue , c.Background.PackedValue, (uint)c.Glyph};
        return string.Join(',', elements);
    }
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        var elements = Convert.ToString(value).Trim('(', ')').Split(',', StringSplitOptions.RemoveEmptyEntries);
        var result = new ColoredGlyph(
            new Color(uint.Parse(elements[0])),
            new Color(uint.Parse(elements[1])),
            (int)uint.Parse(elements[2]));
        return result;
    }
}
public static class SaveGame {
    public static void PrepareConvert() {
        //TypeDescriptor.AddAttributes(typeof(ColoredString), new TypeConverterAttribute(typeof(ColoredStringConverter)));
        TypeDescriptor.AddAttributes(typeof(ColoredGlyph), new TypeConverterAttribute(typeof(ColoredGlyphConverter)));
    }

    public static bool TryDeserializeFile<T>(string file, out T result) {
        if (File.Exists(file)) {
            result = Deserialize<T>(file);
            return true;
        } else {
            result = default(T);
            return false;
        }
    }
    public static void SerializeFile(this object o, string file) {
        File.WriteAllText(file, Serialize(o));
    }
    public static string Serialize(object o) {
        PrepareConvert();
        STypeConverter.PrepareConvert();
        return JsonConvert.SerializeObject(o, format, settings);
    }
    public static T Deserialize<T>(string s) {
        PrepareConvert();
        STypeConverter.PrepareConvert();
        return JsonConvert.DeserializeObject<T>(s, settings);
    }
    public static object Deserialize(string s) {
        PrepareConvert();
        STypeConverter.PrepareConvert();
        return JsonConvert.DeserializeObject(s, settings);
    }
    public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        ContractResolver = new WritablePropertiesOnlyResolver(),

    };
    static SaveGame() {
        settings.ContractResolver = new DictionaryAsArrayResolver();
    }
    public static readonly Formatting format = Formatting.Indented;
}
public class LiveGame {
    public System world;
    public Player player => playerShip.person;
    public PlayerShip playerShip;
    public Lis<LoadHook> hook;

    public delegate void LoadHook(Mainframe main);
    public LiveGame() { }
    public LiveGame(System world, PlayerShip playerShip, Lis<LoadHook> onLoad = null) {
        this.world = world;
        this.playerShip = playerShip;
        this.hook = onLoad;
    }
    public void OnLoad(Mainframe main) => hook?.Value?.Invoke(main);
    public void Save() {
        var s = SaveGame.Serialize(this);
        File.WriteAllText(player.file, s);
    }
}
public class DeadGame {
    public System world;
    public Player player { get; private set; }
    public PlayerShip playerShip;
    public Epitaph epitaph;
    public DeadGame() { }
    public DeadGame(System world, PlayerShip playerShip, Epitaph epitaph) {
        this.world = world;
        this.player = playerShip.person;
        this.playerShip = playerShip;
        this.epitaph = epitaph;
    }
    public void Save() {
        var str = SaveGame.Serialize(this);
        Directory.CreateDirectory("save");
        File.WriteAllText(player.file, str);
        File.WriteAllBytes($"{player.file}.bin", Space.Zip(str));
    }
}
