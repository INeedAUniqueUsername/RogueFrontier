using SadConsole;
using Console = SadConsole.Console;
using System.IO;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;
using ASECII;
using static RogueFrontier.Program;
using System;

namespace RogueFrontier;

public class Trailer {

    public static void Main2(string[] args) {
        // Setup the engine and create the main window.
        SadConsole.Game.Create(Width, Height, "RogueFrontierContent/sprites/IBMCGA.font", (o, gh) => { });
        // Hook the start event so we can add consoles to the system.
        SadConsole.Game.Instance.Started += (o, gh) => Init();
#if DEBUG
        // Start the game.
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
#else
			try {
				// Start the game.
				SadConsole.Game.Instance.Run();
			} catch (Exception e) {
				throw;
			} finally {
				SadConsole.Game.Instance.Dispose();
			}
#endif
    }

    private static void Init() {
#if false
            GameHost.Instance.Screen = new BackdropConsole(Width, Height, new Backdrop(), () => new Common.XY(0.5, 0.5));
			return;
#endif
        System w = new System();
        w.types.LoadFile("RogueFrontierContent/scripts/Main.xml");

        var poster = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/sprites/RogueFrontierPoster.cg")));

        Console container = new Console(Width, Height);
        GameHost.Instance.Screen = container;
        ShowSplash();

        void ShowSplash() {
            SplashScreen c = null;
            c = new SplashScreen(() => ShowPause(c));
            container.Children.Add(c);
        }
        void ShowPause(Console prev) {
            Console c = null;
            c = new PauseTransition(Width, Height, 1, prev, () => ShowFade(c));

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowFade(Console prev) {
            Console c = null;
            c = new FadeOut(prev, () => ShowPoster(c), 1);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowPoster(Console prev) {
            var display = new ImageDisplay(Width, Height, poster, new Point(-5, -5));

            Console pause = null;
            pause = new PauseTransition(Width, Height, 2, display, () => ShowPosterFade(pause));

            //Note that FadeIn automatically replaces the child console
            var c = new FadeIn(pause);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowPosterFade(Console prev) {
            var c = new FadeOut(prev, ShowTitle, 1);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }

        void ShowTitle() {
            var title = new TitleSlideOpening(new TitleScreen(Width, Height, w));
            GameHost.Instance.Screen = title;
        }
    }
}
