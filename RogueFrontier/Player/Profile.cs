using System.Collections.Generic;
using System.IO;

namespace RogueFrontier;

public enum Achievement {
    MementoMori
}
public static class SAchievements {
    public static Dictionary<Achievement, string> names = new() {
        { Achievement.MementoMori, "Memento Mori" }
    };
    public static HashSet<Achievement> GetAchievements(this Profile profile, PlayerShip player) {
        HashSet<Achievement> result = new();
        result.Add(Achievement.MementoMori);
        return result;
    }
}
public class Profile {
    public static string file = "Profile.json";

    public bool allowDictator = false;
    public HashSet<Achievement> achievements = new();
    
    public static bool Load(out Profile p) {
        if (File.Exists(file) && (p = (Profile)SaveGame.Deserialize(File.ReadAllText(file))) != null) {
            return true;
        } else {
            p = null;
            return false;
        }
    }
    public void Save() => File.WriteAllText(file, SaveGame.Serialize(this));

}
