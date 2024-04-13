using ArchConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

namespace RogueFrontier.Screens;

class ConfigPane : Console {
    Settings settings;
    Control? currentSet;
    Dictionary<Control, LabelButton> buttons;

    public ConfigPane(int Width, int Height, Settings settings) : base(Width, Height) {
        this.settings = settings;

        UseKeyboard = true;
        FocusOnMouseClick = true;

        currentSet = null;
        buttons = new Dictionary<Control, LabelButton>();

        Init();
    }
    public void Reset() {
        Children.Clear();
        Init();
    }
    public void Init() {
        int x = 2;
        int y = 0;

        var controls = settings.controls;
        foreach (var control in controls.Keys) {
            var c = control;
            string label = GetLabel(c);
            LabelButton b = null;
            b = new LabelButton(label, () => {

                if (currentSet.HasValue) {
                    ResetLabel(currentSet.Value);
                }

                currentSet = c;
                b.text = $"{c,-16} {"[Press Key]",-12}";
                IsFocused = true;
            }) { Position = new Point(x, y++), FontSize = FontSize };

            buttons[control] = b;
            Children.Add(b);
        }
    }
    string GetLabel(Control control) => $"{control,-16} {settings.controls[control],-12}";
    public void ResetLabel(Control k) => buttons[k].text = GetLabel(k);
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            if (currentSet.HasValue) {
                ResetLabel(currentSet.Value);
                currentSet = null;
            } else {
                var p = Parent;
                p.Children.Remove(this);
                p.IsFocused = true;
            }
        } else if (info.KeysPressed.Any()) {
            if (currentSet.HasValue) {
                settings.controls[currentSet.Value] = info.KeysPressed.First().Key;
                ResetLabel(currentSet.Value);
                currentSet = null;
            }
        }
        return base.ProcessKeyboard(info);
    }
}
