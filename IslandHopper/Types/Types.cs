using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace IslandHopper;

public class TypeCollection {
    public Dictionary<string, XElement> sources;
    public Dictionary<string, DesignType> all;
    public Dictionary<string, ItemType> itemType;
    enum InitState {
        Uninitialized,
        Initializing,
        Initialized
    }
    InitState state;

    //After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
    public TypeCollection(XElement root) {
        sources = new Dictionary<string, XElement>();
        all = new Dictionary<string, DesignType>();
        itemType = new Dictionary<string, ItemType>();
        state = InitState.Uninitialized;

        Debug.Print("TypeCollection created");

        //We do two passes
        //The first pass creates DesignType references for each type and stores the source code
        ProcessRoot(root);
        //The second pass initializes each type from the source code
        Initialize();
    }
    void Initialize() {
        state = InitState.Initializing;
        //We don't evaluate all sources; just the ones that are used by DesignTypes
        foreach (string key in all.Keys.ToList()) {
            DesignType type = all[key];
            XElement source = sources[key];
            type.Initialize(this, source);
        }
        state = InitState.Initialized;
    }
    void ProcessRoot(XElement root) {
        foreach (var element in root.Elements()) {
            ProcessElement(element);
        }
    }
    public void ProcessElement(XElement element) {
        switch (element.Name.LocalName) {
            case "Module":
                XElement module = XDocument.Load(element.ExpectAtt("file")).Root.ExpectElement("IslandHopperModule");
                ProcessRoot(module);
                break;
            case "Source":
                AddSource(element);
                break;
            case "ItemType":
                AddType<ItemType>(element);
                break;
            default:
                //throw new Exception($"Unknown element <{element.Name}>");
                Debug.Print($"Unknown element <{element.Name}>");
                break;
        }
    }
    void AddSource(XElement element) {
        if (!element.TryAtt("codename", out string codename)) {
            //throw new Exception("DesignType requires type attribute");
            codename = System.Guid.NewGuid().ToString();
        }

        if (sources.ContainsKey(codename)) {
            throw new Exception($"DesignType {nameof(codename)} conflict: {codename}");
        } else {
            Debug.Print($"Created <{element.Name}> of {nameof(codename)} {codename}");
            sources[codename] = element;
        }
    }
    void AddType<T>(XElement element) where T : DesignType, new() {
        string codename;
        if (!element.TryAtt(nameof(codename), out codename)) {
            //throw new Exception("DesignType requires type attribute");
            codename = System.Guid.NewGuid().ToString();
        }

        if (sources.ContainsKey(codename)) {
            throw new Exception($"DesignType {nameof(codename)} conflict: {codename}");
        } else {
            Debug.Print($"Created <{element.Name}> of {nameof(codename)} {codename}");
            sources[codename] = element;
            all[codename] = new T();
            switch (all[codename]) {
                case ItemType it:
                    itemType[codename] = it;
                    break;
            }

            //If we're uninitialized, then we will definitely initialize later
            if (state != InitState.Uninitialized) {
                //Otherwise, initialize now
                all[codename].Initialize(this, sources[codename]);
            }
        }
    }
    public bool Lookup(string type, out DesignType result) {
        return all.TryGetValue(type, out result);
    }
    public bool Lookup<T>(string type, out T result) where T : DesignType {
        if (all.TryGetValue(type, out var t) && t is T) {
            result = (T)t;
            return true;
        } else {
            result = default(T);
            return false;
        }
    }
    public T Lookup<T>(string type) where T : DesignType {
        if (all.TryGetValue(type, out var t) && t is T) {
            return (T)t;
        } else {
            return default(T);
        }
    }
}
public interface DesignType {
    void Initialize(TypeCollection collection, XElement e);
}
public class ItemType : DesignType {
    public string name, desc;
    public double mass;

    public ColorImage image;

    //Identity identity;
    public int knownChance;
    public ItemType unknownType;
    public HeadDesc head;
    public GunDesc gun;
    public AmmoDesc ammo;
    public GrenadeType grenade;


    public void Initialize(TypeCollection collection, XElement e) {
        name = e.ExpectAtt("name");
        desc = e.ExpectAtt("desc");
        mass = e.TryAttDouble("mass", 0);
        var imageSource = e.TryAtt("image");
        if (imageSource.Length > 0) {
            image = ColorImage.FromFile(imageSource);
        }

        switch (e.Attribute("known")?.Value) {
            case "true":
                knownChance = 100;
                break;
            case "common":
                knownChance = 80;
                break;
            case "uncommon":
                knownChance = 60;
                break;
            case "rare":
                knownChance = 40;
                break;
            case "exotic":
                knownChance = 20;
                break;
            case "false":
                knownChance = 0;
                break;
        }
        string unknownType = e.TryAtt("unknownType");
        if (!string.IsNullOrWhiteSpace(unknownType)) {
            if (collection.Lookup(unknownType, out DesignType d) && d is ItemType it) {
                this.unknownType = it;
            } else {
                throw new Exception($"Unknown DesignType: {unknownType}");
            }
        }
        if (e.HasElement(HeadDesc.Tag, out XElement xmlHead)) {
            head = new(xmlHead);
        }
        if (e.HasElement(GrenadeType.Tag, out XElement xmlGrenade)) {
            grenade = new(collection, xmlGrenade);
        }
        if (e.HasElement(GunDesc.Tag, out XElement xmlGun)) {
            gun = new(collection, xmlGun);
        }
        if (e.HasElement(AmmoDesc.Tag, out XElement xmlAmmo)) {
            ammo = new(xmlAmmo);
        }


        //Initialize our nested types now (they are not accessible to anyone else at bind time)
        foreach (var inner in e.Elements("ItemType")) {
            collection.ProcessElement(inner);
        }
    }
    public class GrenadeType {
        public static string Tag = "Grenade";

        private string inherit;
        //TO DO: Implement DetonateOnDamage / DeonateOnImpact
        public bool detonateOnDamage;
        public bool detonateOnImpact;
        public bool canArm;
        public int fuseTime;
        public int explosionRadius;
        public int explosionDamage;
        public int explosionForce;

        public GrenadeType() { }
        public GrenadeType(TypeCollection collection, XElement e) {
            inherit = e.TryAtt(nameof(inherit), null);
            if (inherit != null) {
                var source = collection.sources[inherit].Element(Tag);
                e.InheritAttributes(source);
            }

            detonateOnDamage = e.TryAttBool(nameof(detonateOnDamage), true);
            detonateOnImpact = e.TryAttBool(nameof(detonateOnImpact), false);
            canArm = e.TryAttBool(nameof(canArm), true);
            fuseTime = e.TryAttInt(nameof(fuseTime), 5);
            explosionDamage = e.TryAttInt(nameof(explosionDamage), 5);
            explosionForce = e.TryAttInt(nameof(explosionForce), 30);
            explosionRadius = e.TryAttInt(nameof(explosionRadius), 5);
        }
    }
    public class HeadDesc {
        public static string Tag => "Head";
        public int defense, durability;
        public HeadDesc() { }
        public HeadDesc(XElement e) {
            defense = e.ExpectAttInt(nameof(defense));
            durability = e.ExpectAttInt(nameof(durability));
        }
    }
    public class GunDesc {
        public static string Tag = "Gun";

        public enum WeaponDifficulty {
            none = 0,
            easy = 20,
            medium = 40,
            hard = 60,
            expert = 80,
            master = 100
        }

        public interface ProjectileDesc {
            public int range { get; }
        }
        public class GrenadeDesc : ProjectileDesc {
            [Req] public int speed = 150;
            public GrenadeType grenadeType;
            public int range => speed * grenadeType.fuseTime / 30;
            public GrenadeDesc() { }
            public GrenadeDesc(XElement e) {
                e.Initialize(this);
            }
        }
        public class FlameDesc : ProjectileDesc {
            [Req] public int damage;
            [Req] public int speed = 90;
            [Req] public int lifetime;
            public int range => speed * lifetime / 30;
            public FlameDesc() { }
            public FlameDesc(XElement e) {
                e.Initialize(this);
            }
        }
        public class BulletDesc : ProjectileDesc {
            [Req] public int damage;
            public int speed = 90;
            public int knockback;
            public int lifetime = 90;

            public int range => speed * lifetime / 30;
            public BulletDesc() { }
            public BulletDesc(XElement e) {
                e.Initialize(this);
            }
        }

        private string inherit;

        public ProjectileDesc projectile;
        public int difficulty;
        public int recoil;
        public int noiseRange;

        public int projectileCount;
        public int spread;
        public int knockback;
        public int fireTime;
        public int reloadTime;

        public bool critOnLastShot;

        public int clipSize;
        public int maxAmmo;

        public int initialClip;
        public int initialAmmo;

        public GunDesc() { }
        public GunDesc(TypeCollection collection, XElement e) {
            //Don't modify the original source when we inherit
            e = new XElement(e);
            inherit = e.TryAtt(nameof(inherit), null);
            if (inherit != null) {
                var source = collection.sources[inherit].Element(Tag);
                e.InheritAttributes(source);
            }

            Dictionary<string, int> difficultyMap = new Dictionary<string, int> {
                    { "none", 0 },
                    { "easy", 20 },
                    { "medium", 40 },
                    { "hard", 60 },
                    { "expert", 80 },
                    { "master", 100 },
                };
            if (Enum.TryParse<WeaponDifficulty>(e.TryAtt(nameof(difficulty)), out var r)) {
                difficulty = (int)r;
            } else {
                throw new Exception("Difficulty expected");
            }
            recoil = e.TryAttInt(nameof(recoil), 0);
            noiseRange = e.TryAttInt(nameof(noiseRange), 0);

            projectileCount = e.TryAttInt(nameof(projectileCount), 1);
            knockback = e.TryAttInt(nameof(knockback), 0);
            spread = e.TryAttInt(nameof(spread), 0);
            fireTime = e.TryAttInt(nameof(fireTime), 0);
            reloadTime = e.TryAttInt(nameof(reloadTime), 0);

            critOnLastShot = e.TryAttBool(nameof(critOnLastShot), false);

            clipSize = e.TryAttInt(nameof(clipSize), 0);
            maxAmmo = e.TryAttInt(nameof(maxAmmo), 0);

            initialClip = e.TryAttInt(nameof(initialClip), clipSize);
            initialAmmo = e.TryAttInt(nameof(initialAmmo), maxAmmo);

            if (e.HasElement("Bullet", out var bulletXml)) {
                projectile = new BulletDesc(bulletXml);
            } else if (e.HasElement("Flame", out var flameXml)) {
                projectile = new FlameDesc(flameXml);
            } else if (e.HasElement("Grenade", out var grenadeXml)) {
                projectile = new GrenadeDesc(grenadeXml);
            }
        }
    }
    public class AmmoDesc {
        public static string Tag = "Ammo";
        public int amount;
        public AmmoDesc() { }
        public AmmoDesc(XElement e) {
            amount = e.ExpectAttInt(nameof(amount));
        }
    }
    class Symbol {
        private char c;
        private Color background, foreground;
        public Symbol(XElement e) {
            c = e.TryAtt("char", "?")[0];
            background = (Color)typeof(Color).GetProperty(e.TryAtt("background", "black")).GetValue(null, null);
            foreground = (Color)typeof(Color).GetProperty(e.TryAtt("foreground", "red")).GetValue(null, null);
        }
        public ColoredGlyph String => new ColoredGlyph(background, foreground, c);
    }
}
