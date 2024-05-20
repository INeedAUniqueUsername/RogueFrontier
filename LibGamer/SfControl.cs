using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LibGamer;
namespace LibGamer;



public class LabelButton {
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
	public LabelButton ((int,int) pos, string text, Action leftClick = null, Action rightClick = null, bool enabled = true) {
		on = new Sf(text.Length, 1, 1) { pos = pos };
		this.pos = pos;
		this.text = text;
		this.leftClick = leftClick;
		this.rightClick = rightClick;
		this.mouse = new();
		this.enabled = enabled;
	}
	public void HandleMouse (HandState state) {
		mouse.Update(state);
		if(!enabled) {
			return;
		}
		if(!mouse.nowOn) {
			return;
		}
		if(mouse.leftPressOnScreen) {
			switch(mouse.left) {
				case Pressing.Released:
					leftClick?.Invoke();
					break;
				case Pressing.Down:
					leftHold?.Invoke();
					break;
			}
		}
		if(mouse.rightPressOnScreen) {
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
	public void Render () {
		uint f, b;

		if(!enabled) {
			(f, b) = (ABGR.Gray, ABGR.Black);
		} else if(mouse.nowOn &&
			((mouse.nowLeft && mouse.leftPressOnScreen)
			|| (mouse.nowRight && mouse.rightPressOnScreen))) {
			(f, b) = (ABGR.Black, ABGR.White);
		} else {
			(f, b) = (ABGR.White, mouse.nowOn ? ABGR.Gray : ABGR.Black);
		}
		on.Print(0, 0, text, f, b);
		Draw?.Invoke(on);
	}
}