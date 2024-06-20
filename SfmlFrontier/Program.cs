using SadConsole;
using LibGamer;
using static Common.Main;
using System.Reflection;
using System.Collections.Concurrent;
using SFML.Audio;
using ExtSadConsole;
using RogueFrontier;
const int WIDTH = Runner.WIDTH, HEIGHT = Runner.HEIGHT;
string main = ExpectFile($"{Assets.ROOT}/scripts/Main.xml");
string cover = ExpectFile($"{Assets.ROOT}/sprites/game_title.dat");
string splash = ExpectFile($"{Assets.ROOT}/sprites/game_splash_background.dat");

SadConsole.Settings.WindowTitle = $"Rogue Frontier";
Runner.Run(RogueFrontier.Fonts.IBMCGA_8X8_FONT, r => {
	r.Go(new TitleScreen(WIDTH, HEIGHT, new Lazy<RogueFrontier.System>(() => {
		var w = new RogueFrontier.System();
		w.types.LoadFile(main);
		if(w.types.TryLookup<SystemType>("system_intro", out var s)) {
			s.Generate(w);
		}
		return w;
	}).Value));
});