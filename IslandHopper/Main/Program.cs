﻿

using System;
using SadConsole;
using Console = SadConsole.Console;
using SadRogue.Primitives;

namespace IslandHopper {
	class IslandHopper {
		const int Width = 150;
		const int Height = 90;

		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, "Content/IBMCGA.font");

			// Hook the start event so we can add consoles to the system.
			SadConsole.Game.Instance.OnStart = Init;

			// Start the game.
			SadConsole.Game.Instance.Run();
			SadConsole.Game.Instance.Dispose();
		}

		private static void Init() {
			SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height) { IsFocused = true };
		}
	}
}