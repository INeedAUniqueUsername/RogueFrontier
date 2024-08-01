using Newtonsoft.Json;
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
        HashSet<Achievement> result = [Achievement.MementoMori];
        return result;
    }
}
public class Profile {
    public static string file = "Profile.json";
    public bool allowDictator = false;
    public HashSet<Achievement> achievements = [];

    //public ShipControls controls;
    public static bool Load(out Profile p) {
        p = File.Exists(file) ? JsonConvert.DeserializeObject<Profile>(File.ReadAllText(file)) : null;
		return p != null;
	}
    public void Save() => File.WriteAllText(file, JsonConvert.SerializeObject(this));

}
