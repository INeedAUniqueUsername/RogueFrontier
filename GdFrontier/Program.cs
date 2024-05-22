using Godot;
using LibGamer;
using RogueFrontier;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using static Common.Main;

public partial class Program : Node{
	static Program () {
		HEIGHT = 60;
		WIDTH = HEIGHT * 5 / 3; //100
	}
	public static int WIDTH, HEIGHT;
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
		Go(new TitleScreen(96, 64, GenerateIntroSystem()));
		void Go (IScene next) {
			if(current is { } prev) {
				prev.Go -= Go;
				prev.Draw -= Draw; 
			}
			if(next == null) {
				throw new Exception("Main scene cannot be null");
			}
			current = next;
			current.Go += Go;
			current.Draw += Draw;
		};
		void Draw (Sf sf) {
			var c = surfaces.GetOrAdd(sf, sf => {
				var s = surface.Instantiate<Surface>();
				AddChild(s);
				s.Show();
				return s;
			});
			c.Clear();
			foreach(var ((x,y),t) in sf.Active) {
				c.Print(x, y, (char)t.Glyph, new Color(ABGR.ToRGBA(t.Foreground)), new Color(ABGR.ToRGBA(t.Background)));
			}
			c.QueueRedraw();
			return;
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

		current?.Render(TimeSpan.FromSeconds(delta));
		//c.QueueRedraw();
	}

	public override void _Input (InputEvent ev) {
		switch(ev) {
			case InputEventKey k: {
					if(k.IsPressed()) {
						down.Add((KC)k.Keycode);
					} else {
						down.Remove((KC)k.Keycode);
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
					};
					Debug.WriteLine(hand);
					break;
				}
			case InputEventMouseMotion m: {
					hand = hand with { pos = ((int)m.Position.X, (int)m.Position.Y) };
					Debug.WriteLine($"{hand.pos.x}, {hand.pos.y}");
					break;
				}
		}
		base._Input(ev);
	}
}
