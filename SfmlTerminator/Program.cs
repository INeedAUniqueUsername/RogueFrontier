using LibTerminator;
using LibGamer;
using SadConsole;
using SFML.Audio;
using System.Collections.Concurrent;
using System.Reflection;
using ExtSadConsole;
using static ExtSadConsole.SadGamer;
SadConsole.Settings.WindowTitle = $"Rogue Terminator";
Runner.Run(RogueFrontier.Fonts.IBMCGA_8X8_FONT, r => {
	r.Go(new Mainframe(Runner.WIDTH, Runner.HEIGHT));
});