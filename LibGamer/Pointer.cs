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
public class Pointer {
    public Hovering mouse;
    public bool nowMouseOver;
    public bool prevMouseOver;
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
    public void Update ((int x, int y) pos, bool leftDown, bool rightDown, bool IsMouseOver) {
		prevPos = nowPos;
		nowPos = pos;

		prevLeft = nowLeft;
		prevRight = nowRight;
		nowLeft = leftDown;
		nowRight = rightDown;

		prevMouseOver = nowMouseOver;
        nowMouseOver = IsMouseOver;
        mouse = !prevMouseOver ? (nowMouseOver ? Hovering.Enter : Hovering.Outside) : (nowMouseOver ? Hovering.Hover : Hovering.Exit);


        left = !prevLeft ? (!nowLeft ? Pressing.Up : Pressing.Pressed) : (nowLeft ? Pressing.Down : Pressing.Released);
        right = !prevRight ? (!nowRight ? Pressing.Up : Pressing.Pressed) : (nowRight ? Pressing.Down : Pressing.Released);
        if(left == Pressing.Pressed) {
            leftPressedOnScreen = IsMouseOver;
            leftPressedPos = pos;
        }
        if(right == Pressing.Pressed) {
            rightPressedOnScreen = IsMouseOver;
            rightPressedPos = pos;
        }
    }
}