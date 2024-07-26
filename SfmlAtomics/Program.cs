using LibAtomics;
using LibGamer;
using SadConsole;
using SFML.Audio;
using System.Collections.Concurrent;
using System.Reflection;
using ExtSadConsole;
using static ExtSadConsole.SadGamer;
SadConsole.Settings.WindowTitle = $"Rogue Atomics";

var assets = Assets.CreateAsync(new(async s => File.ReadAllText(s), async s => File.ReadAllBytes(s))).Result;
Runner.Run(RogueFrontier.Fonts.IBMCGA_8X8_FONT, r => {
	r.Go(new Mainframe(Runner.WIDTH, Runner.HEIGHT, assets));
});