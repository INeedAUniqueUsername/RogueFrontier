using System;
using System.Collections.Generic;
using System.Text;

namespace LibGamer;
    public enum ButtonStates {
        PrevUp = 1,
        PrevDown = 2,
        NowUp = 4,
        NowDown = 8
    }
    public enum Pressing {
        Up = ButtonStates.PrevUp | ButtonStates.NowUp,
        Pressed = ButtonStates.PrevUp | ButtonStates.NowDown,
        Down = ButtonStates.PrevDown | ButtonStates.NowDown,
        Released = ButtonStates.PrevDown | ButtonStates.NowUp
    }
    public enum Hovering {
        Enter,
        On,
        Exit,
        Off
    }
public record HandState((int x, int y) pos, int wheelValue, bool leftDown, bool middleDown, bool rightDown, bool on) {
    public bool Handled = false;
    public HandState OnRect (Rect r) => this with { pos = (pos.x - r.x, pos.y - r.y), on = r.Contains(pos) };
}
public record Hand {
    public Hovering mouse => (prevOn, nowOn) switch {
        (true, true) => Hovering.On,
        (true, false) => Hovering.Exit,
        (false, true) => Hovering.Enter,
        (false, false) => Hovering.Off,
    };
    public Pressing left => (prevLeft, nowLeft) switch {
		(true, true) => Pressing.Down,
		(true, false) => Pressing.Released,
		(false, true) => Pressing.Pressed,
		(false, false) => Pressing.Up
	};

	public Pressing middle => (prevMiddle, nowMiddle) switch {
		(true, true) => Pressing.Down,
		(true, false) => Pressing.Released,
		(false, true) => Pressing.Pressed,
		(false, false) => Pressing.Up
	};
	public Pressing right => (prevRight, nowRight) switch {
        (true, true) => Pressing.Down,
        (true, false) => Pressing.Released,
        (false, true) => Pressing.Pressed,
        (false, false) => Pressing.Up
    };

	public bool nowOn => now.on;
	public bool prevOn => prev.on;

    public record Press((int x, int y) pos, bool on);

	public Press leftPress = new Press((0,0), false);
    public Press rightPress = new Press((0, 0), false);
    public (int x, int y) prevPos => prev.pos;
    public (int x, int y) nowPos => now.pos;
    public (int x, int y) deltaPos => (nowPos.x - prevPos.x, nowPos.y - prevPos.y);
    public bool prevLeft => prev.leftDown;
    public bool prevMiddle => prev.middleDown;
    public bool prevRight => prev.rightDown;
    public bool nowLeft => now.leftDown;
    public bool nowMiddle => now.middleDown;
    public bool nowRight => now.rightDown;

    public int deltaWheel => now.wheelValue - prev.wheelValue;

	private HandState prev = new HandState((0,0), 0, false, false, false, false);
	private HandState now = new HandState((0, 0), 0, false, false, false, false);
    public void Update (HandState state) {
        prev = now;
        now = state;
        if(left == Pressing.Pressed) {
            leftPress = new(state.pos, state.on);
        }
        if(right == Pressing.Pressed) {
			leftPress = new(state.pos, state.on);
		}
    }
}