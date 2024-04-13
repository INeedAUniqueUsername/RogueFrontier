using ASECII;
using Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ASECII;
public static class ASECIILoader {
	public static Dictionary<(int, int), TileValue> LoadCG (string path) =>
		DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(path));
	public static T DeserializeObject<T> (string s) {
		STypeConverter.PrepareConvert();
		return JsonConvert.DeserializeObject<T>(s, SFileMode.settings);
	}
	public static string SerializeObject (object o) {
		STypeConverter.PrepareConvert();
		return JsonConvert.SerializeObject(o, SFileMode.settings);
	}
}