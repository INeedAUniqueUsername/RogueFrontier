using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LibGamer;
namespace LibGamer;
public interface SfControl {
	public Action<Sf> Draw { set; get; }
	void Render (TimeSpan delta);
	void HandleMouse (HandState state) { }
	void HandleKey (KB kb) { }

	bool IsFocused => false;
}
public class SfLabel : SfControl {
	public Action<Sf> Draw { set; get; }
	Sf on;
	(int x, int y) pos;
	public Tile[] text;
	public SfLabel (Sf on, (int,int) pos, string text) {
		this.on = on;
		this.pos = pos;
		this.text = Tile.Arr(text);
	}
	public void Render (TimeSpan delta) {
		on.Print(pos.x, pos.y, text);
	}
}
public class SfLink : SfControl{
	public Action<Sf> Draw { set; get; }

	(int x, int y) pos;
	public string text {
		set {
			_text = value;
		}
		get { return _text; }
	}
	private string _text;
	public Action leftClick;
	public Action rightClick;
	public Action leftHold;
	public Action rightHold;

	Hand mouse;
	public bool enabled;
	public Sf on;

	public Rect rect => new Rect(pos.x * on.GlyphWidth, pos.y * on.font.GlyphHeight, text.Length * on.GlyphWidth, 1 * on.font.GlyphHeight);
	public SfLink (Sf on, (int,int) pos, string text, Action leftClick = null, Action rightClick = null, bool enabled = true) {
		this.on = on;
		this.pos = pos;
		this.text = text;
		this.leftClick = leftClick;
		this.rightClick = rightClick;
		this.mouse = new();
		this.enabled = enabled;
	}
	public void HandleMouse (HandState state) {
		mouse.Update(state.OnRect(rect));
		if(!enabled) {
			return;
		}
		if(!mouse.nowOn) {
			return;
		}
		if(mouse.leftPress.on) {
			switch(mouse.left) {
				case Pressing.Released:
					leftClick?.Invoke();
					break;
				case Pressing.Down:
					leftHold?.Invoke();
					break;
			}
		}
		if(mouse.rightPress.on) {
			switch(mouse.right) {
				case Pressing.Released:
					rightClick?.Invoke();
					break;
				case Pressing.Down:
					rightHold?.Invoke();
					break;
			}
		}
	}
	public void Render (TimeSpan delta) {
		uint f, b;

		if(!enabled) {
			(f, b) = (ABGR.Gray, ABGR.Black);
		} else if(mouse.nowOn &&
			((mouse.nowLeft && mouse.leftPress.on)
			|| (mouse.nowRight && mouse.leftPress.on))) {
			(f, b) = (ABGR.Black, ABGR.White);
		} else {
			(f, b) = (ABGR.White, mouse.nowOn ? ABGR.Gray : ABGR.Black);
		}
		on.Print(pos.x, pos.y, text, f, b);
	}
}
public class SfLinkGroup {
	public List<SfControl> container;
	public (int x, int y) pos;
	public List<SfControl> controls = [];

	public delegate SfLink MakeLink (string label, Action clicked);
	MakeLink makeLink;
	public SfLinkGroup (List<SfControl> container, (int x, int y) pos, Sf on) {
		this.container = container;
		this.pos = pos;
		makeLink = (text, clicked) => new SfLink(on, (pos.x, pos.y + controls.Count), text, clicked);
	}
	public void Add (string label, Action clicked) {
		var b = makeLink(label, clicked);
		controls.Add(b);
		container.Add(b);
	}
	public void Clear () {
		foreach(var b in controls) {
			container.Remove(b);
		}
		controls.Clear();
	}
}

public class SfBool :SfControl{

	public Action<Sf> Draw { set; get; }

	public (int x, int y) pos;
	public Sf on;
	bool b;
	public SfBool((int x, int y) pos, Sf on) {
		this.pos = pos;
		this.on = on;
	}
	Hand h = new();
	void SfControl.HandleMouse(LibGamer.HandState state) {

		if(on.SubRect(pos.x, pos.y, 1, 1).Contains(state.pos)) {
			
		}
	}
	void SfControl.Render(System.TimeSpan delta) {
		on.Print(pos.x, pos.y, b ? "True" : "False");
	}

}

public class SfField : SfControl {
	public Action<Sf> Draw { get; set; }

	private int _index;
	public int index {
		get => _index;
		set {
			_index = Math.Clamp(value, 0, text.Length);
			UpdateTextStart();
		}
	}
	private int textStart;
	public string _text;
	public string text {
		get => _text; set {
			_text = value;
			TextChanged?.Invoke(this);
		}
	}
	public int selectStart;
	public int selectEnd; //exclusive
	public string placeholder;
	private double time;
	private Hand mouse;
	public Predicate<char> CharFilter;
	public Action<SfField> TextChanged;
	public Action<SfField> EnterPressed;
	Sf on;
	public int Width;
	public (int x, int y) pos;

	bool IsFocused { get; set; } = false;


	public Rect rect => new Rect(pos.x * on.GlyphWidth, pos.y * on.GlyphHeight, Width * on.GlyphWidth, 1 * on.GlyphHeight);

	public SfField (Sf on, (int x, int y) pos, int width, string text = "") {
		this.on = on;
		this.Width = width;
		this.pos = pos;
		_index = 0;
		_text = "";
		this.text = text;
		placeholder = new string('.', width);
		time = 0;
		mouse = new();
	}
	public void UpdateTextStart () {
		textStart = Math.Max(Math.Min(text.Length, _index) - Width + 1, 0);
	}
	public void Update (TimeSpan delta) {
		time += delta.TotalSeconds;
	}
	public void Render (TimeSpan delta) {
		var text = this.text;
		var showPlaceholder = this.text.Length == 0 && !IsFocused;
		if(showPlaceholder) {
			text = placeholder;
		}
		int x2 = Math.Min(text.Length - textStart, Width);

		bool showCursor = time % 2 < 1;

		var foreground = mouse.nowOn ? ABGR.Yellow : ABGR.White;
		var background = IsFocused ? ABGR.RGBA(51, 51, 51, 255) : ABGR.Black;

		if(((mouse.leftPress.on && mouse.left == Pressing.Down) ||
				(mouse.rightPress.on && mouse.right == Pressing.Down))
				&& mouse.nowOn) {
			(foreground, background) = (background, foreground);
		}
		for(int x = 0; x < Width; x++) {
			on.SetBack(pos.x + x, pos.y + 0, background);
		}
		Func<int, Tile> getGlyph = (i) => new Tile(foreground, background, text[i]);
		if(showCursor && IsFocused) {
			if(_index < text.Length) {
				getGlyph = i =>	i == _index ? new Tile(background, foreground, text[i])
											: new Tile(foreground, background, text[i]);
			} else {
				on.SetBack(pos.x + x2, pos.y + 0, foreground);
			}
		}
		for(int x = 0; x < x2; x++) {
			var i = textStart + x;
			on.SetTile(pos.x + x, pos.y + 0, getGlyph(i));
		}
	}
	public void HandleKey (KB keyboard) {
		if(!IsFocused) {
			return;
		}
		
		if(keyboard.Press.Any()) {
			//bool moved = false;
			foreach(var key in keyboard.Press) {
				switch(key) {
					case KC.Up:
						_index = 0;
						time = 0;
						UpdateTextStart();
						break;
					case KC.Down:
						_index = text.Length;
						time = 0;
						UpdateTextStart();
						break;
					case KC.Right:
						_index = Math.Min(_index + 1, text.Length);
						time = 0;
						UpdateTextStart();
						break;
					case KC.Left:
						_index = Math.Max(_index - 1, 0);
						time = 0;
						UpdateTextStart();
						break;
					case KC.Home:
						_index = 0;
						time = 0;
						break;
					case KC.End:
						_index = text.Length;
						time = 0;
						break;
					case KC.Back:
						if(text.Length > 0) {
							bool repeat = false;
						Delete:
							if(_index > 0) {
								char deleted;
								string next;
								if(_index >= text.Length) {
									deleted = text.Last();
									next = text.Substring(0, text.Length - 1);
								} else {
									deleted = text[_index];
									next = text.Substring(0, _index - 1) + text.Substring(_index);
								}
								if(repeat && !char.IsLetterOrDigit(deleted)) {
									goto Done;
								}
								text = next;
								_index--;
								if((keyboard.IsDown(KC.LeftControl) || keyboard.IsDown(KC.RightControl)) && char.IsLetterOrDigit(deleted)) {
									repeat = true;
									goto Delete;
								}
							}
						Done:
							time = 0;
							UpdateTextStart();
						}

						break;
					case KC.Enter:
						EnterPressed?.Invoke(this);
						break;
					default:
						var kc = key switch {
							>= KC.A and <= KC.Z => ((Func<char,char>)(
								keyboard.IsDown(KC.LeftShift) ?
									char.ToUpper :
									char.ToLower))((char)key),
							>= KC.D0 and <= KC.D9 => (char)key,
							_ => '\0',
						};
						if(kc != 0) {
							if(CharFilter?.Invoke(kc) != false) {
								if(_index == text.Length) {
									text += kc;
									_index++;
								} else if(_index > 0) {
									text = text.Substring(0, index) + kc + text.Substring(index);
									_index++;
								} else {
									text = (kc) + text;
									_index++;
								}
								time = 0;
								UpdateTextStart();
							}
						}
						break;
				}
			}
			keyboard.Handled = true;
		}
	}
	public void HandleMouse (HandState state) {
		mouse.Update(state.OnRect(rect));
		if(mouse.nowOn && mouse.nowLeft && mouse.leftPress.on) {
			IsFocused = true;
			_index = Math.Min(mouse.nowPos.x, text.Length);
			time = 0;
		} else if(!mouse.nowOn && mouse.left == Pressing.Pressed) {
			IsFocused = false;
		}
	}
}


public class LabeledField : SfControl {
	public Action<Sf> Draw { get; set; }
	public string label;
	public SfField textBox;
	public List<SfControl> controls = [];
	public Sf on;
	public (int x, int y) pos;
	public LabeledField (Sf on, (int x, int y) pos, string label, string text = "", Action<SfField, string> TextChanged = null) {
		this.on = on;
		this.pos = pos;
		this.label = label;
		this.textBox = new SfField(on, (pos.x + (label.Length / 8 + 1) * 8, pos.y), 16, text);
		if(TextChanged != null)
			this.textBox.TextChanged += (e) => TextChanged?.Invoke(this.textBox, this.textBox.text);
		controls.Add(textBox);
	}
	public void HandleKey(KB keyboard) {
		textBox.HandleKey(keyboard);
	}
	public void HandleMouse(HandState state) {
		textBox.HandleMouse(state);
	}
	public void Render (TimeSpan delta) {
		on.Print(pos.x, pos.y, label, ABGR.White, ABGR.Black);
		textBox.Render(delta);
		Draw?.Invoke(on);
	}
}