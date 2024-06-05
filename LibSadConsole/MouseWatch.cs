using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtSadConsole;
public enum ButtonStates {
    PrevUp = 1,
    PrevDown = 2,
    NowUp = 4,
    NowDown = 8
}
public enum ClickState {
    Up = ButtonStates.PrevUp | ButtonStates.NowUp,
    Pressed = ButtonStates.PrevUp | ButtonStates.NowDown,
    Held = ButtonStates.PrevDown | ButtonStates.NowDown,
    Released = ButtonStates.PrevDown | ButtonStates.NowUp
}
public enum MouseState {
    Enter,
    Hover,
    Exit,
    Outside
}
public class MouseWatch {
    public MouseState mouseOver;
    public bool nowMouseOver;
    public bool prevMouseOver;
    public ClickState left;
    public ClickState right;
    public bool leftPressedOnScreen;
    public bool rightPressedOnScreen;
    public Point leftPressedPos;
    public Point rightPressedPos;
    public Point prevPos;
    public Point nowPos;
    public bool prevLeft;
    public bool prevRight;
    public bool nowLeft;
    public bool nowRight;
    public void Update(MouseScreenObjectState state, bool IsMouseOver) {
        prevMouseOver = nowMouseOver;
        nowMouseOver = IsMouseOver;
        mouseOver = !prevMouseOver ? (nowMouseOver ? MouseState.Enter : MouseState.Outside) : (nowMouseOver ? MouseState.Hover : MouseState.Exit);
        prevPos = nowPos;
        nowPos = state.SurfaceCellPosition;
        prevLeft = nowLeft;
        prevRight = nowRight;
        nowLeft = state.Mouse.LeftButtonDown;
        nowRight = state.Mouse.RightButtonDown;
        left = !prevLeft ? (!nowLeft ? ClickState.Up : ClickState.Pressed) : (nowLeft ? ClickState.Held : ClickState.Released);
        right = !prevRight ? (!nowRight ? ClickState.Up : ClickState.Pressed) : (nowRight ? ClickState.Held : ClickState.Released);
        if(left == ClickState.Pressed) {
            leftPressedOnScreen = IsMouseOver;
            leftPressedPos = state.SurfaceCellPosition;
        }
        if(right == ClickState.Pressed) {
            rightPressedOnScreen = IsMouseOver;
            rightPressedPos = state.SurfaceCellPosition;
        }
    }
}