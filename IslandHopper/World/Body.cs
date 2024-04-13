using System.Collections.Generic;

namespace IslandHopper;

public interface IBodyPart {
    EBodyPartType Type { get; }
    string Name { get; }
    int MaxHP { get; }
    int CurrentHP { get; set; }
    int Bleeding { get; set; }
    HashSet<Item> Equipped { get; set; }
}
public static class BodyHelper {
    //Equipped items are kept in the parent entity's inventory
    //If we removed any items from our inventory, make sure they get removed from our equips
    public static void UpdateEquipped(this IBodyPart bp, HashSet<Item> Inventory) {
        bp.Equipped.RemoveWhere(i => !Inventory.Contains(i));
    }
    /*
    public static bool CanEquip(this IBodyPart bp, Item i) {
        switch(bp.Type) {
            case EBodyPartType.Head: return bp.Equipped.FirstOrDefault(e => e.Head != null);
            case EBodyPartType.Arm: return bp.Equipped.FirstOrDefault(e => e.Arm != null);
            case EBodyPartType.Leg: return bp.Equipped.FirstOrDefault(e => e.Leg != null);
        }
    }
    */
}
public interface Body {
    Dictionary<EBodyPartType, IBodyPart> parts { get; }
}
public enum EBodyPartType {
    Head,
    Arm,
    Torso,
    Leg,
}
public class BodyPart : IBodyPart {
    public EBodyPartType Type { get; }
    public string Name { get; private set; }
    public int MaxHP { get; private set; } = 100;
    public int CurrentHP { get; set; } = 100;
    public int Bleeding { get; set; } = 0;
    public HashSet<Item> Equipped { get; set; } = null;
    private static BodyPart CreateStandardPart(EBodyPartType type, string Name) => new() { Name = Name, MaxHP = 100, CurrentHP = 100, Equipped = new HashSet<Item>() };
    private static BodyPart CreateHead() => CreateStandardPart(EBodyPartType.Head, "Head");
    private static BodyPart CreateLeftArm() => CreateStandardPart(EBodyPartType.Arm, "Left Arm");
    private static BodyPart CreateRightArm() => CreateStandardPart(EBodyPartType.Arm, "Right Arm");
    private static BodyPart CreateTorso() => CreateStandardPart(EBodyPartType.Torso, "Torso");
    private static BodyPart CreateLeftLeg() => CreateStandardPart(EBodyPartType.Leg, "Left Leg");
    private static BodyPart CreateRightLeg() => CreateStandardPart(EBodyPartType.Leg, "Right Leg");
    public static HashSet<BodyPart> CreateStandardBody() => new(new[] { CreateHead(), CreateLeftArm(), CreateRightArm(), CreateTorso(), CreateLeftLeg(), CreateRightLeg() });
}
