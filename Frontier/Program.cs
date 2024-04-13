using System;
using Console = System.Console;
using SadConsole;
using Con = SadConsole.Console;
using System.IO;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;
using System.Linq;
using ASECII;
using SadConsole.Input;
using static Common.Main;
using System.Globalization;
using SFML.Audio;
using System.Xml.Linq;
using System.Reflection;

namespace RogueFrontier;
public static class Tones {
    public static Sound pressed = new(new SoundBuffer("Assets/sounds/button_press.wav")) {
        Volume = 33
    };
}
partial class Program {
    public static int TICKS_PER_SECOND = 60;
    static Program() {
        Height = 60;
        Width = Height * 5 / 3;
    }
    public static int Width, Height;
    public static string font = ExpectFile("Assets/sprites/IBMCGA+.font");
    public static string main = ExpectFile("Assets/scripts/Main.xml");
    public static string cover = ExpectFile("Assets/sprites/RogueFrontierPosterV2.asc.cg");
    public static string splash = ExpectFile("Assets/sprites/SplashBackgroundV2.asc.cg");
    
    static void OutputSchema() {

        var d = new Dictionary<Type, XElement>();
        WriteSchema(typeof(ItemType), d);

        var module = new XElement("Schema");
        foreach (var (key, value) in d) {
            module.Add(value);
        }
        File.WriteAllText("RogueFrontierSchema.xml", module.ToString());
    }
    static void Main(string[] args) {
#if false
        XSave x = null;

        var o = GenerateIntroSystem();
        var map = new XMap(typeof(SoundBufferPort));
        var d = new XSave() { map = map };
        d.SavePointer(o);
        var s1 = d.root.ToString();

        var o2 = d.root.Load();
        d = new XSave() { map = map };
        d.SavePointer(o2);
        var s2 = d.root.ToString();
        var diff = s1.Length - s2.Length;

		Console.WriteLine($"{diff}");

        if (true) return;
#endif
        //OutputSchema();
        SadConsole.Settings.WindowTitle = $"Rogue Frontier v{Assembly.GetExecutingAssembly().GetName().Version}";
        /*
        var w = new System();
        w.types.LoadFile(main);
        string s = "";
        foreach(var type in w.types.Get<ItemType>()) {
            s += (@$"{'\n'}{{""{type.codename}"", {type.value}}}");
        }
        */
        StartGame(StartRegular);
    }

	public static void StartGame(Action OnStart) {
        if (!Directory.Exists("save"))
            Directory.CreateDirectory("save");
        //SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Add;
        Game.Create(Width, Height, font, (o, gh) => { });
        Game.Instance.Started += (o, gh)=>OnStart();
        Game.Instance.Run();
        Game.Instance.Dispose();
    }
    public static System GenerateIntroSystem() {
        var w = new System();
        w.types.LoadFile(main);
        if(w.types.TryLookup<SystemType>("system_intro", out var s)) {
            s.Generate(w);
        }
        return w;
    }
    public static void StartRegular() {
#if false
            GameHost.Instance.Screen = new BackdropConsole(Width, Height, new Backdrop(), () => new Common.XY(0.5, 0.5));
			return;
#endif
        //var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
        //SaveGame.Deserialize(File.ReadAllText(files.First()));

        var splashMusic = new Sound(new SoundBuffer("Assets/music/Splash.wav")) {
            Volume = 33
        };
        var poster = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(cover)));

        var title = new TitleScreen(Width, Height, GenerateIntroSystem());
        var titleSlide = new TitleSlideOpening(title) { IsFocused = true };

        var splashBack = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(splash)));
        var splashBackground = new ImageDisplay(Width / 2, Height / 2, splashBack, new Point()) { FontSize = title.FontSize * 2 };

        int index = 0;
        KeyWatcher container = null;
        container = new KeyWatcher(Width, Height, (k) => {
            if (k.IsKeyPressed(Keys.Enter)) {
                switch (index) {
                    case 1: {
                            container.Children.Clear();
                            Con c = new(Width, Height);
                            container.Children.Add(c);
                            ShowOpening(c);
                            break;
                        }
                    case 2: {
                            container.Children.Clear();
                            Con c = new(Width, Height);
                            container.Children.Add(c);
                            ShowPoster(c);
                            break;
                        }
                    case 3:
                    default:
                        ShowTitle();
                        break;
                }

            }
        }) {
            IsFocused = true, UseKeyboard = true,
        };
        container.Children.Add(splashBackground);

        GameHost.Instance.Screen = container;


#if DEBUG
        ShowTitle();
        //title.QuickStart();
        //title.StartSurvival();
#else
            ShowSplash();
#endif

        void ShowSplash() {


            //var p = Path.GetFullPath(theme);
            //MediaPlayer.Play(Song.FromUri(p, new(p)));

            index = 1;
            SplashScreen c = null;
            c = new SplashScreen(() => ShowCrawl(c));
            container.Children.Add(c);
        }
        void ShowCrawl(Con prev) {
            MinimalCrawlScreen c = null;
            string s = "Presents...";
            c = new MinimalCrawlScreen(s, () => {
                ShowPause(prev);
            }) { Position = new Point(prev.Width / 4 - s.Length / 2 + 1, 13), FontSize = prev.FontSize * 2 };
            prev.Children.Add(c);
        }
        void ShowPause(Con prev) {
            Con c = null;
            c = new PauseTransition(Width, Height, 1, prev, () => ShowFade(c));

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowFade(Con prev) {
            Con c = null;
            c = new FadeOut(prev, () => ShowOpening(c), 1);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }

        void ShowOpening(Con prev) {
            splashMusic.Play();
            index = 2;
            prev.Parent.Children.Remove(splashBackground);

            Con c = null;
            c = new MinimalCrawlScreen(
@"                  
A reimagining of...
                    
      Transcendence  
 by George Moromisato
                    
And the vision that was
more than just a dream...
                    ".Replace("\r", null), () => ShowFade2(c)) {
                Position = new Point(4, 4),
                FontSize = title.FontSize * 3
            };

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        /*
        void ShowPause2(Console prev) {
            Console c = null;
            c = new PauseTransition(Width, Height, 1, prev, () => ShowFade2(c));

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        */
        void ShowFade2(Con prev) {
            Con c = null;
            c = new FadeOut(prev, () => ShowPause2(c), 1);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowPause2(Con prev) {
            Con c = null;
            c = new PauseTransition(Width, Height, 1, prev, () => ShowPoster(c));

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowPoster(Con prev) {
            index = 3;
            var display = new ImageDisplay(poster.Size.X, poster.Size.Y, poster,
                new Point(Width / 2 - poster.Size.X / 2 + 4, -5)) {
                FontSize = title.FontSize * 3 / 4
            };

            Con pause = null;
            pause = new PauseTransition(display.Width, display.Height, 2, display, () => ShowPosterFade(pause)) {
                FontSize = display.FontSize
            };

            //Note that FadeIn automatically replaces the child console
            var c = new FadeIn(pause);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }
        void ShowPosterFade(Con prev) {
            var c = new FadeOut(prev, ShowTitle, 1);

            prev.Parent.Children.Add(c);
            prev.Parent.Children.Remove(prev);
        }

        void ShowTitle() {

            splashMusic.Stop();
            title.titleMusic.Play();
            index = 4;
            titleSlide.IsFocused = true;
            GameHost.Instance.Screen = titleSlide;
        }
        //GameHost.Instance.Screen = new TitleDraw();
    }
}
