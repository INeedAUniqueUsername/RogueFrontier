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
public record HandState((int x, int y) pos, bool leftDown, bool rightDown, bool nowOn);
public class Hand {
    public Hovering mouse;
    public bool nowOn;
    public bool prevOn;
    public Pressing left;
    public Pressing right;
    public bool leftPressedOnScreen;
    public bool rightPressedOnScreen;
    public (int x, int y) leftPressedPos;
    public (int x, int y) rightPressedPos;
    public (int x, int y) prevPos;
    public (int x, int y) nowPos;
    public bool prevLeft;
    public bool prevRight;
    public bool nowLeft;
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
            leftPressedOnScreen = state.nowOn;
            leftPressedPos = state.pos;
        }
        if(right == Pressing.Pressed) {
            rightPressedOnScreen = state.nowOn;
            rightPressedPos = state.pos;
        }
    }
}