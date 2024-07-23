using LibGamer;
using SadConsole;
using SFML.Audio;
using System.Collections.Concurrent;
using System.Reflection;
using static ExtSadConsole.SadGamer;
namespace ExtSadConsole;
public class Runner {
	ConcurrentDictionary<Sf, ScreenSurface> consoles = new();
	ConcurrentDictionary<SoundCtx, Sound> sounds = new();
	ConcurrentDictionary<byte[], SoundBuffer> soundBuffers = [];
	IScene current = null;
#if false
	public const int WIDTH = 106, HEIGHT = 64;
#else
	public const int WIDTH = 150, HEIGHT = 90;
#endif
	public static void Run (string font, Action<Runner> Start) {
		//SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Add;
		Game.Create(WIDTH, HEIGHT, font, (o, gh) => { });
		SadConsole.Host.Settings.SFMLScreenBlendMode = SFML.Graphics.BlendMode.Alpha;
		SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Alpha;

		var p = new Runner();
		Game.Instance.Started += (o, host) => p.OnStart(host);
		Start?.Invoke(p);

		Game.Instance.Run();
		Game.Instance.Dispose();
	}

	public void OnStart(GameHost host) {
		var kb = new KB();
		host.FrameUpdate += (o, gh) => {
			kb.Update([.. gh.Keyboard.KeysDown.Select(k => (KC)k.Key)]);
			if(current is { } c) {
				c.Update(gh.UpdateFrameDelta);
				c.HandleKey(kb);
				var m = gh.Mouse;
				c.HandleMouse(new HandState(m.ScreenPosition, m.ScrollWheelValue, m.LeftButtonDown, m.MiddleButtonDown, m.RightButtonDown, m.IsOnScreen));
			}
		};
		host.FrameRender += (o, gh) => {
			current.Render(gh.DrawFrameDelta);
		};
	}
	public void Go (IScene next) {
		if(current is { } prev) {
			prev.Go -= Go;
			prev.Draw -= Draw;
			prev.PlaySound -= PlaySound;
		}
		current = next ?? throw new Exception("Main scene cannot be null");
		current.Go += Go;
		current.Draw += Draw;
		current.PlaySound += PlaySound;
	}
	public void Draw (Sf sf) {
		var c = consoles.GetOrAdd(sf, sf => {
			var f = sf.font;
			if(!GameHost.Instance.Fonts.TryGetValue(f.name, out var font)) {
				var t = GameHost.Instance.GetTexture(new MemoryStream(f.data));
				font = new SadFont(f.GlyphWidth, f.GlyphHeight, 0, f.rows, f.cols, f.solidGlyphIndex, t, f.name);
				GameHost.Instance.Fonts[f.name] = font;
			}
			var c = new SadConsole.ScreenSurface(sf.GridWidth, sf.GridHeight) {
				Position = new(sf.pos.xi, sf.pos.yi),
				Font = font,
			};
			c.FontSize *= sf.scale;
			//c.FontSize *= sf.scale;
			return c;
		});
		c.Clear();
		foreach(var (x, y) in sf.Active) {
			var t = sf.Tile[x, y];
			var cg = c.Surface[sf.GetIndex(x, y)];
			cg.Foreground = new SadRogue.Primitives.Color(t.Foreground);
			cg.Background = new SadRogue.Primitives.Color(t.Background);
			cg.Glyph = (int)t.Glyph;
		}
		c.Render(new TimeSpan());

		return;
	}
	public void PlaySound (SoundCtx s) {
		var snd = sounds.GetOrAdd(s, s => {
			var snd = new Sound(new SoundBuffer(s.data)) { Volume = s.volume };
			s.IsPlaying = () => snd.Status == SoundStatus.Playing;
			s.Stop = snd.Stop;
			return snd;
		});
		if(snd.Status == SoundStatus.Playing)
			snd.Stop();
		snd.SoundBuffer = soundBuffers.GetOrAdd(s.data, d => new SoundBuffer(d));
		snd.Volume = s.volume;
		snd.Position = new SFML.System.Vector3f(s.pos.x, s.pos.y, 0);
		snd.Play();
	}
}


/*
using LibGamer;
using SadConsole;
using SFML.Audio;
using System.Collections.Concurrent;
namespace ExtSadConsole;
public class Runner {
	ConcurrentDictionary<Sf, ScreenSurface> consoles = new();
	ConcurrentDictionary<SoundCtx, Sound> sounds = new();
	ConcurrentDictionary<byte[], SoundBuffer> soundBuffers = [];
	IScene current = null;
#if false
	public const int WIDTH = 106, HEIGHT = 64;
#else
	public const int WIDTH = 150, HEIGHT = 90;
#endif
	public static void Run (string font, Action<Runner> Start) {
		//SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Add;
		Game.Create(WIDTH, HEIGHT, font, (o, gh) => { });
		SadConsole.Host.Settings.SFMLScreenBlendMode = SFML.Graphics.BlendMode.Alpha;
		SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Alpha;

		var p = new Runner();
		Game.Instance.Started += (o, host) => p.OnStart(host);
		Start?.Invoke(p);

		Game.Instance.Run();
		Game.Instance.Dispose();
	}

	public void OnStart(GameHost host) {
		var kb = new KB();
		host.FrameUpdate += (o, gh) => {
			kb.Update([.. gh.Keyboard.KeysDown.Select(k => (KC)k.Key)]);
			if(current is { } c) {
				c.Update(gh.UpdateFrameDelta);
				c.HandleKey(kb);
				var m = gh.Mouse;
				c.HandleMouse(new HandState(m.ScreenPosition, m.ScrollWheelValue, m.LeftButtonDown, m.MiddleButtonDown, m.RightButtonDown, m.IsOnScreen));
			}
		};
		host.FrameRender += (o, gh) => {
			current.Render(gh.DrawFrameDelta);
		};
	}

	public void Run () {
		var w = new SFML.Graphics.RenderWindow(new SFML.Window.VideoMode(WIDTH * 8, HEIGHT * 8), "SFML");

		var prev = DateTime.Now;
		var kb = new KB();
		var keysPressed = new HashSet<KC>();

		w.KeyPressed += (a, e) => {
			keysPressed.Add((KC)e.Code);
		};
		w.KeyReleased += (a, e) => {
			keysPressed.Remove((KC)e.Code);
		};

		while(w.IsOpen) {
			w.DispatchEvents();

			var now = DateTime.Now;
			var delta = now - prev;

			kb.Update(keysPressed);
			if(current is { } c) {
				c.Update(delta);
				c.HandleKey(kb);
				var m = gh.Mouse;
				c.HandleMouse(new HandState(m.ScreenPosition, m.ScrollWheelValue, m.LeftButtonDown, m.MiddleButtonDown, m.RightButtonDown, m.IsOnScreen));

				c.Render(delta);
			}

			w.Display();
		}
	}
	public void Go (IScene next) {
		if(current is { } prev) {
			prev.Go -= Go;
			prev.Draw -= Draw;
			prev.PlaySound -= PlaySound;
		}
		current = next ?? throw new Exception("Main scene cannot be null");
		current.Go += Go;
		current.Draw += Draw;
		current.PlaySound += PlaySound;
	}
	public void Draw (Sf sf) {
		var c = consoles.GetOrAdd(sf, sf => {
			var f = sf.font;
			if(!GameHost.Instance.Fonts.TryGetValue(f.name, out var font)) {
				var t = GameHost.Instance.GetTexture(new MemoryStream(f.data));
				font = new SadFont(f.GlyphWidth, f.GlyphHeight, 0, f.rows, f.cols, f.solidGlyphIndex, t, f.name);
				GameHost.Instance.Fonts[f.name] = font;
			}
			var c = new SadConsole.ScreenSurface(sf.Width, sf.Height) {
				Position = new(sf.pos.xi, sf.pos.yi),
				Font = font,
			};
			c.FontSize *= sf.scale;
			//c.FontSize *= sf.scale;
			return c;
		});
		c.Clear();
		foreach(var (x, y) in sf.Active) {
			c.SetCellAppearance(x, y, sf.Tile[x,y].ToCG());
		}
		c.Render(new TimeSpan());
		return;
	}
	public void PlaySound (SoundCtx s) {
		var snd = sounds.GetOrAdd(s, s => {
			var snd = new Sound(new SoundBuffer(s.data)) { Volume = s.volume };
			s.IsPlaying = () => snd.Status == SoundStatus.Playing;
			s.Stop = snd.Stop;
			return snd;
		});
		if(snd.Status == SoundStatus.Playing)
			snd.Stop();
		snd.SoundBuffer = soundBuffers.GetOrAdd(s.data, d => new SoundBuffer(d));
		snd.Volume = s.volume;
		snd.Position = new SFML.System.Vector3f(s.pos.x, s.pos.y, 0);
		snd.Play();
	}
}

*/ 