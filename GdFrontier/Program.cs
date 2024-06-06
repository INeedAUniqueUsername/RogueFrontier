using Godot;
using LibGamer;
using RogueFrontier;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Common.Main;
public partial class Program : Node2D {
	public static int WIDTH = 100, HEIGHT = 60;
	//public static string FONT_8X8 = ExpectFile("Assets/sprites/IBMCGA+.font");
	public static string main = ExpectFile($"{Assets.ROOT}/scripts/Main.xml");
	//public static string cover = ExpectFile("Assets/sprites/RogueFrontierPosterV2.dat");
	//public static string splash = ExpectFile("Assets/sprites/SplashBackgroundV2.dat");
	public override void _Ready() {
		RogueFrontier.System GenerateIntroSystem () {
			var a = new Assets();
			var u = new Universe(a);
			var w = new RogueFrontier.System(u);
			w.types.LoadFile(main);
			if(w.types.TryLookup<SystemType>("system_intro", out var s)) {
				s.Generate(w);
			}
			return w;
		}
		var r = new Runner();
		AddChild(r);
		r.Go(new TitleScreen(WIDTH, HEIGHT, GenerateIntroSystem()));
	}

}
