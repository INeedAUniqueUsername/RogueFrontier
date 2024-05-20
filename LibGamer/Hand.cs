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
        Hover,
        Exit,
        Outside
    }
public record HandState((int x, int y) pos, int wheelValue, bool leftDown, bool middleDown, bool rightDown, bool nowOn) {
    public HandState OnRect (Rect r) => this with { pos = (pos.x - r.x, pos.y - r.y), nowOn = r.Contains(pos) };
}
public record Hand {
    public Hovering mouse;
    public bool nowOn;
    public bool prevOn;
    public Pressing left;
    public Pressing right;
    public bool leftPressOnScreen;
    public bool rightPressOnScreen;
    public (int x, int y) leftPressPos;
    public (int x, int y) rightPressPos;
    public (int x, int y) prevPos;
    public (int x, int y) nowPos;
    public (int x, int y) deltaPos => (nowPos.x - prevPos.x, nowPos.y - prevPos.y);
    public bool prevLeft;
    public bool prevWheel;
    public bool prevRight;
    public bool nowLeft;
    public bool nowMiddle;
    public bool nowRight;

    public int MouseWheelScroll;
    public void Update (HandState state) {
		prevPos = nowPos;
		nowPos = state.pos;

		prevLeft = nowLeft;
		prevRight = nowRight;
		nowLeft = state.leftDown;
		nowRight = state.rightDown;

		prevOn = nowOn;
        nowOn = state.nowOn;
        mouse = !prevOn ? (nowOn ? Hovering.Enter : Hovering.Outside) : (nowOn ? Hovering.Hover : Hovering.Exit);


        left = !prevLeft ? (!nowLeft ? Pressing.Up : Pressing.Pressed) : (nowLeft ? Pressing.Down : Pressing.Released);
        right = !prevRight ? (!nowRight ? Pressing.Up : Pressing.Pressed) : (nowRight ? Pressing.Down : Pressing.Released);
        if(left == Pressing.Pressed) {
            leftPressOnScreen = state.nowOn;
            leftPressPos = state.pos;
        }
        if(right == Pressing.Pressed) {
            rightPressOnScreen = state.nowOn;
            rightPressPos = state.pos;
        }
    }
}