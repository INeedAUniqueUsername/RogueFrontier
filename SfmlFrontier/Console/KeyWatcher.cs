using System;
using Console = SadConsole.Console;
using SadConsole.Input;

namespace RogueFrontier;

public class KeyWatcher : Console {
    public Action<Keyboard> KeyPressed;
    public KeyWatcher(int Width, int Height, Action<Keyboard> KeyPressed) : base(Width, Height) {
        this.KeyPressed = KeyPressed;
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        KeyPressed(keyboard);
        return base.ProcessKeyboard(keyboard);
    }
}
