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
	IScene current;
	HashSet<KC> down = [];
	KB kb = new();
	HandState hand = new((0,0), 0, false, false, false, true);
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
		var surface = ResourceLoader.Load<PackedScene>("res://Surface.tscn");
		ConcurrentDictionary<Sf, Surface> surfaces = new();
		ConcurrentDictionary<Tf, SurfaceFont> fonts = [];
		ConcurrentDictionary<SoundCtx, AudioStreamPlayer2D> sounds = new();
		ConcurrentDictionary<byte[], AudioStream> streams = new();
		Go(new TitleScreen(WIDTH, HEIGHT, GenerateIntroSystem()));
		void Go (IScene next) {
			if(current is { } prev) {
				prev.Go -= Go;
				prev.Draw -= Draw;
				prev.PlaySound -= PlaySound;
			}
			if(next == null) {
				throw new Exception("Main scene cannot be null");
			}
			current = next;
			current.Go += Go;
			current.Draw += Draw;
			current.PlaySound += PlaySound;
		};
		void Draw (Sf sf) {
			var c = surfaces.GetOrAdd(sf, sf => {
				var s = surface.Instantiate<Surface>();
				AddChild(s);
				s.Show();
				s.GridWidth = sf.Width;
				s.GridHeight = sf.Height;
				var pos = sf.pos * sf.font.GlyphSize;
				s.Position = new Vector2(pos.xf, pos.yf);
				s.font = fonts.GetOrAdd(sf.font, f => {
					var i = Image.Create(f.Width, f.Height, false, Image.Format.Rgba8);
					i.LoadPngFromBuffer(f.data);
					return new SurfaceFont() {
						GlyphWidth = f.GlyphWidth,
						GlyphHeight = f.GlyphHeight,
						GlyphPadding = 0,
						SolidGlyphIndex = f.solidGlyphIndex,
						Columns = f.cols,
						Texture = ImageTexture.CreateFromImage(i)
					};
				});
				return s;
			});
			c.Clear();
			foreach(var ((x,y),t) in sf.Active) {
				c.Print(x, y, (char)t.Glyph, new Color(ABGR.ToRGBA(t.Foreground)), new Color(ABGR.ToRGBA(t.Background)));
			}
			c.Show();
			//c.QueueRedraw();
			return;
		}
		void PlaySound(SoundCtx s) {
			var snd = sounds.GetOrAdd(s, s => {
				var snd = new AudioStreamPlayer2D();
				s.Stop = snd.Stop;
				s.IsPlaying = () => snd.Playing;
				snd.Finished += () => {
					
				};
				AddChild(snd);
				return snd;
			});
			snd.Stream = streams.GetOrAdd(s.data, d => {
				var str = new AudioStreamWav();
				str.Data = d;
				str.Format = AudioStreamWav.FormatEnum.Format16Bits;
				str.Stereo = true;
				return str;
			});
			//snd.VolumeDb = s.volume;
			snd.Position = new Vector2(s.pos.x, s.pos.y);
			if(snd.Playing) snd.Stop();
			//snd.Play();
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		//var c = (Surface)GetNode("Surface");
		//c.Print(1, 1, "Hello World");
		kb.Update(down);
		//Debug.WriteLine($"press: {string.Join(' ', kb.Press)}, down: {string.Join(' ', kb.Down)}");
		current?.Update(TimeSpan.FromSeconds(delta));
		current?.HandleKey(kb);
		current?.HandleMouse(hand);
		foreach(Node2D c in GetChildren().OfType<Node2D>()) {
			c.Hide();
		}
		current?.Render(TimeSpan.FromSeconds(delta));

	}
	public override void _PhysicsProcess (double delta) {}
	public override void _Draw () {
		base._Draw();
	}
	public override void _Notification (int what) {

		switch((long)what) {
			case NotificationWMMouseEnter:
				hand = hand with { on = true };
				break;
			case NotificationWMMouseExit:
				hand = hand with { on = false };
				break;
		}
		base._Notification(what);
	}
	public override void _Input (InputEvent ev) {
		switch(ev) {
			case InputEventKey k: {
				var kc = k.Keycode;
				KC c = kc switch {
					Key.Left => KC.Left,
					Key.Up => KC.Up,
					Key.Right => KC.Right,
					Key.Down => KC.Down,
					Key.Minus => KC.OemMinus,
					Key.Equal => KC.OemPlus,
					Key.Shift => KC.LeftShift,
					Key.Ctrl => KC.LeftControl,
					Key.Escape => KC.Escape,
					Key.Enter => KC.Enter,
					_ => (KC)kc
				};
				if(k.IsPressed()) {
					down.Add(c);
				} else {
					down.Remove(c);
				}
				//Debug.WriteLine(string.Join(' ', down));
				break;
			}
			case InputEventMouseButton b: {
				var p = b.Pressed;
				hand = b.ButtonIndex switch {
					MouseButton.Left => hand with { leftDown = p },
					MouseButton.Middle => hand with { middleDown = p },
					MouseButton.Right => hand with { rightDown = p },
					_ => hand
				} with { on = true };
				//Debug.WriteLine(hand);
				break;
			}
			case InputEventMouseMotion m: {
				hand = hand with { pos = ((int)m.Position.X, (int)m.Position.Y), on = true };
				//Debug.WriteLine($"{hand.pos.x}, {hand.pos.y}");
				break;
			}
		}
		base._Input(ev);
	}
}
