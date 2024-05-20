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
	public LabelButton ((int,int) pos,string text, Action leftClick = null, Action rightClick = null, bool enabled = true) {
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
		if(mouse.leftPressedOnScreen) {
			switch(mouse.left) {
				case Pressing.Released:
					leftClick?.Invoke();
					break;
				case Pressing.Down:
					leftHold?.Invoke();
					break;
			}
		}
		if(mouse.rightPressedOnScreen) {
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
	public void Render (Sf on) {
		uint f, b;

		if(!enabled) {
			(f, b) = (ABGR.Gray, ABGR.Black);
		} else if(mouse.nowOn &&
			((mouse.nowLeft && mouse.leftPressedOnScreen)
			|| (mouse.nowRight && mouse.rightPressedOnScreen))) {
			(f, b) = (ABGR.Black, ABGR.White);
		} else {
			(f, b) = (ABGR.White, mouse.nowOn ? ABGR.Gray : ABGR.Black);
		}
		on.Print(pos.x, pos.y, text, f, b);
	}
}