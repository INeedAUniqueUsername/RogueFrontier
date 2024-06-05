using LibTerminator;
using LibGamer;
using RogueFrontier;
using SadConsole;
using SFML.Audio;
using System.Collections.Concurrent;
using System.Reflection;
using static LibSadConsole.SadGamer;

ConcurrentDictionary<Sf, SadConsole.Console> consoles = new();
ConcurrentDictionary<SoundCtx, Sound> sounds = new();
ConcurrentDictionary<byte[], SoundBuffer> soundBuffers = [];
IScene current = null;
const int WIDTH = 100, HEIGHT = 60;
void Go (IScene next) {
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
void Draw (Sf sf) {
	var c = consoles.GetOrAdd(sf, sf => {
		var f = sf.font;
		if(!GameHost.Instance.Fonts.TryGetValue(f.name, out var font)) {
			var t = GameHost.Instance.GetTexture(new MemoryStream(f.data));
			font = new SadFont(f.GlyphWidth, f.GlyphHeight, 0, f.rows, f.cols, f.solidGlyphIndex, t, f.name);
			GameHost.Instance.Fonts[f.name] = font;
		}
		var c = new SadConsole.Console(sf.Width, sf.Height) {
			Position = new(sf.pos.xi, sf.pos.yi),
			Font = font,
		};
		//c.FontSize *= sf.scale;
		return c;
	});
	c.Clear();
	foreach(var ((x, y), t) in sf.Active) {
		c.SetCellAppearance(x, y, t.ToCG());
	}
	c.Render(new TimeSpan());
	return;
}
void PlaySound (SoundCtx s) {
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
SadConsole.Settings.WindowTitle = $"Rogue Atomics";
if(!Directory.Exists("save"))
	Directory.CreateDirectory("save");
//SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Add;
Game.Create(WIDTH, HEIGHT, Fonts.IBMCGA_8X8_FONT, (o, gh) => { });
SadConsole.Host.Settings.SFMLScreenBlendMode = SFML.Graphics.BlendMode.Alpha;
SadConsole.Host.Settings.SFMLSurfaceBlendMode = SFML.Graphics.BlendMode.Alpha;
Game.Instance.Started += (o, host) => {
	Go(new LibTerminator.Mainframe(WIDTH, HEIGHT));

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
};
Game.Instance.Run();
Game.Instance.Dispose();