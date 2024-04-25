using Antlr4.Runtime.Atn;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;


public interface IScene {
	public delegate void Set (IScene next);
	public Set Transition { set; get; }
}
public enum NavFlags : long {
	ESC = 0b1,
	ENTER = 0b10
}
public record NavChoice (char key, string name, Func<IScene, IScene> next, NavFlags flags = 0, bool enabled = true) {
	public NavChoice () : this('\0', "", null, 0) { }
	public NavChoice (string name) : this(name, null, 0) { }
	public NavChoice (string name, Func<IScene, IScene> next, NavFlags flags = 0, bool enabled = true) : this(name.FirstOrDefault(char.IsLetterOrDigit), name, next, flags, enabled) { }
}
public static class SNav {

	public static NavChoice DockArmorRepair (PlayerShip p, int price) =>
		DockArmorReplacement(p, a => price);
	public static NavChoice DockArmorRepair (PlayerShip p, Func<Armor, int> GetPrice) =>
		new("Service: Armor Repair", prev => SMenu.DockArmorRepair(prev, p, GetPrice, null));
	public static NavChoice DockArmorReplacement (PlayerShip p, int price) =>
		DockArmorReplacement(p, a => price);
	public static NavChoice DockArmorReplacement (PlayerShip p, Func<Armor, int> GetPrice) =>
		new("Service: Armor Replacement", prev => SMenu.DockArmorReplacement(prev, p, GetPrice, null));

	public static NavChoice DockDeviceInstall (PlayerShip p, Func<Device, int> GetPrice) =>
		new("Service: Device Install", prev => SMenu.DockDeviceInstall(prev, p, GetPrice, null));

	public static NavChoice DockDeviceRemoval (PlayerShip p, Func<Device, int> GetPrice) =>
		new("Service: Device Removal", prev => SMenu.DockDeviceRemoval(prev, p, GetPrice, null));
}
public class Dialog : IScene {
	public delegate void AddNav (int index);
	public IScene.Set Transition { get; set; }
	public event Action<SoundDesc> PlaySound;
	public event Action PrintComplete;
	public string descStr;
	public Tile[] desc;
	public bool charging;
	public int descIndex;
	public int ticks;
	List<NavChoice> navigation;
	public int navIndex = 0;
	double[] charge;
	public Dictionary<(int, int), Tile> background = new();
	Dictionary<char, int> keyMap = new();
	bool prevEscape;
	bool allowEnter;
	bool prevEnter;
	bool enter;
	int escapeIndex;
	int lineCount;
	public static double maxCharge = 0.5;
	public SoundDesc button_press = new() {
		data = File.ReadAllBytes("Assets/sounds/button_press.wav"),
		volume = 33
	};
	public Dialog (string descStr, List<NavChoice> navigation){
		this.descStr = descStr;
		
		var quoted = false;
		var highlight = false;

		this.desc = [.. descStr.Replace("\r", null).Select(c => {
			var b = ABGR.Black;
			var T = (uint f) => new Tile(f, b, c);
			switch(c){
				case '"':
					quoted = !quoted;
					return T(ABGR.LightBlue);
				case '[' or ']':
					highlight = !highlight;
					return T(ABGR.Yellow);
				default:
					var f =
						highlight ?
							ABGR.Yellow :
						quoted ?
							ABGR.LightBlue :
						ABGR.LightYellow;
					return T(f);
			}
		})];
		navigation.RemoveAll(s => s == null);
		this.navigation = navigation;
		charge = new double[navigation.Count];
		descIndex = 0;
		ticks = 0;
		escapeIndex = navigation.FindIndex(o => (o.flags & NavFlags.ESC) != 0);
		if(escapeIndex == -1) escapeIndex = navigation.Count - 1;
	}
	int deltaIndex = 2;
	public void Update (TimeSpan delta) {
		ticks++;
		if(ticks % 2 * deltaIndex == 0) {
			if(descIndex < desc.Length - 1) {
				deltaIndex = 1;
				while(descIndex < desc.Length - 1) {
					descIndex++;
					deltaIndex++;

					var g = desc[descIndex].Glyph;
					if(g == ' ') {
						break;
					}
				}
			} else if(descIndex < desc.Length) {
				lineCount = desc.Count(c => c.Glyph == '\n');
				foreach(var (i, option) in navigation.Index()) {
					keyMap[char.ToUpper(option.key)] = i;
				}
				PrintComplete();
			}
		}
		if(charging && navIndex != -1 && navigation[navIndex].enabled) {
			ref double c = ref charge[navIndex];
			if(c < maxCharge) {
				c += delta.TotalSeconds;
			}
		}
		if(prevEnter && !enter) {
			ref double c = ref charge[navIndex];
			if(c >= maxCharge) {
				//Make sure we aren't sent back to the screen again
				prevEnter = false;
				Transition(navigation[navIndex].next?.Invoke(this));
				c = maxCharge - 0.01;
			}
		} else {
			prevEnter = enter;
		}
		for(int i = 0; i < charge.Length; i++) {
			if(i == navIndex && charging) {
				continue;
			}
			ref double c = ref charge[i];
			if(c > 0) {
				c -= delta.TotalSeconds;
			}
		}
	}
	public void Render (ISurf surf, TimeSpan delta) {
		//this.RenderBackground();
		if(background != null) {
			foreach(((var px, var py), var t) in background) {
				surf.Tile[px, py] = t;
			}
		}
		int descX = surf.Width / 2 + 8, descY = 8;

		int left = descX;
		int top = lineCount + descY;
		int y = top;
		int x = left;
		for(int i = 0; i < descIndex; i++) {
			switch(desc[i].Glyph) {
				case '\n':
					x = left;
					y++;
					break;
				default:
					surf.Tile[x, y] = desc[i];
					x++;
					break;
			}
		}
		if(descIndex < desc.Length) {
			surf.Tile[x, y] = new Tile(ABGR.LightBlue, ABGR.Black, '>');
		} else {
			var barLength = 4;
			var arrow = $"{new string('-', barLength - 1)}>";
			x = descX - barLength;
			y = descY + desc.Count(c => c.Glyph == '\n') + 3;
			foreach(var (c, i) in charge.Select((c, i) => (c, i))) {
				surf.Print(x, y + i, Tile.Arr(arrow[0..(int)(barLength * Math.Clamp(c / maxCharge, 0, 1))], ABGR.Gray, ABGR.Black));
			}
			if(navIndex > -1) {
				surf.Print(x, y + navIndex, Tile.Arr(arrow, ABGR.Gray, ABGR.Black));
				var ch = charge[navIndex];
				surf.Print(x, y + navIndex, Tile.Arr(arrow.Substring(0, (int)(barLength * Math.Clamp(ch / maxCharge, 0, 1))), ch < maxCharge ? ABGR.Yellow : ABGR.Orange, ABGR.Black));
			}
		}
	}
	public void ProcessKeyboard (KB kb) {
		charging = false;
		if(kb[KC.Escape] == KS.Pressed || prevEscape && kb[KC.Escape, 1]) {
			if(!prevEscape) {
				PlaySound(button_press);
			}
			navIndex = escapeIndex;
			charging = true;
			enter = true;
			prevEscape = true;
		} else {
			prevEscape = false;
			enter = kb[KC.Enter, 1];
			if(enter) {
				if(!prevEnter) {
					PlaySound(button_press);
				}

				if(descIndex < desc.Length - 1) {
					descIndex = desc.Length - 1;
					allowEnter = false;
				} else if(allowEnter) {
					charging = true;
				}
			} else if(allowEnter) {
				if(kb[KC.Right, 1]) {
					enter = true;
					charging = true;
				}
			} else {
				allowEnter = true;
			}
			foreach(var c in kb.Down.Where(c => char.IsLetterOrDigit((char)c)).Select(c => char.ToUpper((char)c))) {
				if(keyMap.TryGetValue(c, out int index)) {
					if(!prevEnter) {
						PlaySound(button_press);
					}

					navIndex = index;
					charging = true;
					enter = true;
				}
			}
			if(kb[KC.Up] == KS.Pressed) {
				PlaySound(button_press);
				navIndex = (navIndex - 1 + navigation.Count) % navigation.Count;
			}
			if(kb[KC.Down] == KS.Down) {
				PlaySound(button_press);
				navIndex = (navIndex + 1) % navigation.Count;
			}
		}
	}
	public void ProcessMouse (Pointer state) {
		if(state.nowLeft) {
			if(descIndex < desc.Length - 1) {
				descIndex = desc.Length - 1;
				allowEnter = false;
			}
		}
	}
}
