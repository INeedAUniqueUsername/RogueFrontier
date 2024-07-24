using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibAtomics;
public record GetDataAsync (Func<string, Task<string>> getString, Func<string, Task<byte[]>> getBytes) { }
public class Assets {
 	private Assets () { }
	public TileImage title;
	public TileImage hive;

	public TileImage giantCockroachRobot;


#if GODOT
	public static string ROOT = "Lib/LibFrontier/Assets";
#else
	public static string ROOT = "Assets";
#endif
	public static string IBMCGA_8x8_JSON { get; } = $"{ROOT}/font/IBMCGA+_8x8.font";
	public static string IBMCGA_6x8_JSON { get; } = $"{ROOT}/font/IBMCGA+_6x8.font";
	public static string RF_8x8_JSON { get; } = $"{ROOT}/font/RF_8x8.font";
	public Tf IBMCGA_8x8;
	public Tf IBMCGA_6x8;
	public Tf RF_8x8;
	public static async Task<Assets> CreateAsync(GetDataAsync dl) {
		Console.WriteLine("Creating Assets");
		var a = new Assets();
		await a.InitAsync(dl);
		return a;
	}
	private async Task InitAsync (GetDataAsync dl) {
		Console.WriteLine("Initializing Assets");

		var (getString, getBytes) = dl;
		title = new(ImageLoader.ReadTile(await getString($"{ROOT}/sprite/title.dat")));
		hive = new(ImageLoader.ReadTile(await getString($"{ROOT}/sprite/icon.dat")));
		giantCockroachRobot = new(ImageLoader.ReadTile(await getString("Assets/sprite/giant_cockroach_robot.dat")));


		IBMCGA_8x8 = new Tf(await getBytes($"{ROOT}/font/IBMCGA+_8x8.png"), "IBMCGA+_8x8", 8, 8, 256 / 8, 256 / 8, 219);//
		IBMCGA_6x8 = new Tf(await getBytes($"{ROOT}/font/IBMCGA+_6x8.png"), "IBMCGA+_6x8", 6, 8, 192 / 6, 64 / 8, 219);
		RF_8x8 = new Tf(await getBytes($"{ROOT}/font/RF_8x8.png"), "RF_8x8", 8, 8, 256 / 8, 256 / 8, 219);
	}
}
