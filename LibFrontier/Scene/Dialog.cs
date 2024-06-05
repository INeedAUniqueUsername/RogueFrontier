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
public interface IRender {
	Sf sf { get; }
	public int Width => sf.Width;
	public int Height => sf.Height;
}

public enum NavFlags : long {
	ESC = 0b1,
	ENTER = 0b10
}
public record NavChoice (char key, string name, Func<IScene, IScene> next, NavFlags flags = 0, bool enabled = true) {
	public delegate void Next (IScene prev);
	public NavChoice () : this('\0', "", null, 0) { }
	public NavChoice (string name) : this(name, null, 0) { }
	public NavChoice (string name, Func<IScene, IScene> next, NavFlags flags = 0, bool enabled = true) : this(name.FirstOrDefault(char.IsLetterOrDigit), name, next, flags, enabled) { }
}
public static class SNav {
	public static NavChoice DockArmorRepair (SceneCtx c, int price) =>
		DockArmorReplacement(c, a => price);
	public static NavChoice DockArmorRepair (SceneCtx c, Func<Armor, int> GetPrice) =>
		new("Service: Armor Repair", prev => SMenu.DockArmorRepair(c with { prev = prev }, GetPrice, null));
	public static NavChoice DockArmorReplacement (SceneCtx c , int price) =>
		DockArmorReplacement(c, a => price);
	public static NavChoice DockArmorReplacement (SceneCtx c, Func<Armor, int> GetPrice) =>
		new("Service: Armor Replacement", prev => SMenu.DockArmorReplacement(c with { prev = prev }, GetPrice, null));
	public static NavChoice DockDeviceInstall (SceneCtx c, Func<Device, int> GetPrice) =>
		new("Service: Device Install", prev => SMenu.DockDeviceInstall(c with { prev=prev}, GetPrice, null));
	public static NavChoice DockDeviceRemoval (SceneCtx c, Func<Device, int> GetPrice) =>
		new("Service: Device Removal", prev => SMenu.DockDeviceRemoval(c with { prev = prev }, GetPrice, null));
}
public class Dialog : IScene {
	public delegate void AddNav (int index);
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
	Sf surf;

	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Dialog (SceneCtx ctx, string descStr, List<NavChoice> navigation){
		surf = new Sf(ctx.Width, ctx.Height, Fonts.FONT_8x8);
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


	void IScene.Update (TimeSpan delta) {
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
				foreach(var (i, option) in navigation.Select((n, i) => (i, n))) {
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
				navigation[navIndex].next.Invoke(this);
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
	void IScene.Render (TimeSpan delta) {
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
		Draw?.Invoke(surf);
	}
	void IScene.HandleKey (KB kb) {
		charging = false;
		if(kb[KC.Escape] == KS.Press || prevEscape && kb[KC.Escape, 1]) {
			if(!prevEscape) {
				PlaySound?.Invoke(Tones.pressed);
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
					PlaySound?.Invoke(Tones.pressed);
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
						PlaySound?.Invoke(Tones.pressed);
					}

					navIndex = index;
					charging = true;
					enter = true;
				}
			}
			if(kb[KC.Up] == KS.Press) {
				PlaySound?.Invoke(Tones.pressed);
				navIndex = (navIndex - 1 + navigation.Count) % navigation.Count;
			}
			if(kb[KC.Down] == KS.Down) {
				PlaySound?.Invoke(Tones.pressed);
				navIndex = (navIndex + 1) % navigation.Count;
			}
		}
	}
	void IScene.HandleMouse (HandState state) {
		if(state.leftDown) {
			if(descIndex < desc.Length - 1) {
				descIndex = desc.Length - 1;
				allowEnter = false;
			}
		}
	}
}
