using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace RogueFrontier;

public enum EShipBehavior {
    none, sulphin, swift, trader
}
public class ShipClass : IDesignType {
    public static ShipClass empty => new ShipClass() { devices = new(), damageDesc = new HitPointDesc(), rotationDecel = 1 };
    public HashSet<string> attributes;
    [Req] public string codename;
    [Req] public string name;
    [Req] public double thrust;
    [Req] public double maxSpeed;
    [Req] public double rotationMaxSpeed;
    [Req] public double rotationDecel;
    [Req] public double rotationAccel;
    [Opt] public bool crimeOnDestroy;
    [Opt] public double stealth;
    [Opt] public int capacity;
    public EShipBehavior behavior;
    public StaticTile tile;
    public HullSystemDesc damageDesc;
    public Group<Item> cargo;
    public Group<Device> devices;
    public PlayerSettings playerSettings;
    public ItemFilter restrictWeapon, restrictArmor;

    public void Validate() {
        if (rotationDecel == 0) {
            throw new Exception("Ship must be able to decelerate rotation");
        }
    }
    public ShipClass() { }
    public void Initialize(TypeCollection collection, XElement e) {
        var parent = e.TryAtt("inherit", out string inherit) ? collection.Lookup<ShipClass>(inherit) : null;
        e.Initialize(this, parent);
        if (parent != null) {
            tile = e.HasElement("Tile", out XElement xmlTile) ? 
                new(xmlTile) : parent.tile;
        } else {
            tile = new(e);
        }
        attributes = e.TryAtt("attributes", out string att) ? att.Split(";").ToHashSet() : parent?.attributes ?? new();
        behavior = e.TryAttEnum(nameof(behavior), parent?.behavior ?? EShipBehavior.none);

        damageDesc = e.HasElement("HPSystem", out var xmlHPSystem) ?
            new HitPointDesc(xmlHPSystem) :
            e.HasElement("LayeredArmorSystem", out var xmlLayeredArmor) ?
            new LayeredArmorDesc(xmlLayeredArmor) :
            parent?.damageDesc ??
            throw new Exception("<ShipClass> requires either <HPSystem> or <LayeredArmorSystem> subelement");

        devices = e.HasElement("Devices", out var xmlDevices) ?
            new(xmlDevices, SGenerator.DeviceFrom) :
            parent?.devices;
        cargo = e.HasElement("Cargo", out var xmlCargo) || e.HasElement("Items", out xmlCargo) ?
            new(xmlCargo, (XElement e) => SGenerator.ItemFrom(collection, e)) :
            parent?.cargo;
        playerSettings = e.HasElement("PlayerSettings", out var xmlPlayerSettings) ?
            new(xmlPlayerSettings, parent?.playerSettings) :
            parent?.playerSettings;
        restrictArmor = e.HasElement("RestrictArmor", out var xmlRequireArmor) ?
            new(xmlRequireArmor) :
            parent?.restrictArmor;
        restrictWeapon = e.HasElement("RestrictWeapon", out var xmlRequireWeapon) ?
            new(xmlRequireWeapon) :
            parent?.restrictWeapon;
    }
}
public interface HullSystemDesc {
    HullSystem Create(TypeCollection tc);
}
public record HitPointDesc : HullSystemDesc {
    [Req] public int maxHP;
    public HitPointDesc() { }
    public HitPointDesc(XElement e) {
        e.Initialize(this);
    }
    public HullSystem Create(TypeCollection tc) =>
        new HP(maxHP);
}
public record LayeredArmorDesc : HullSystemDesc {
    public Group<Armor> armorList;
    public LayeredArmorDesc() { }
    public LayeredArmorDesc(XElement e) {
        armorList = new Group<Armor>(e, SGenerator.ArmorFrom);
    }
    public HullSystem Create(TypeCollection tc) =>
        new LayeredArmor(armorList.Generate(tc));
}
public record PlayerSettings() {
    [Req] public bool startingClass;
    [Req] public string description;
    public string[] map;
    public PlayerSettings(XElement e, PlayerSettings source = null) : this() {
        e.Initialize(this, source);

        map = source?.map ?? e.Element("Map")?.Value?.Replace("\r", "").Split('\n');
    }
}
