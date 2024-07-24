using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibAtomics;
public record GetDataAsync (Func<string, Task<string>> GetStringAsync, Func<string, Task<byte[]>> GetBytesAsync) { }
public class Assets {
 	private Assets () { }
	public TileImage title;
	public TileImage hive;
	public static async Task<Assets> CreateAsync(GetDataAsync getter) {
		Console.WriteLine("Creating Assets");
		var a = new Assets();
		await a.InitAsync(getter);
		return a;
	}
	private async Task InitAsync (GetDataAsync getter) {
		Console.WriteLine("Initializing Assets");
		title = new(ImageLoader.ReadTile(await getter.GetStringAsync("Assets/sprite/title.dat")));
		hive = new(ImageLoader.ReadTile(await getter.GetStringAsync("Assets/sprite/icon.dat")));
	}
}
