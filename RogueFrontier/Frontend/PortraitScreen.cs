
using SadConsole;
using System;
using Console = SadConsole.Console;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RogueFrontier;

class PortraitScreen : Console {
    Point cursor;
    ColoredGlyph[,] image;
    double time;

    ColoredGlyph empty => new ColoredGlyph(Color.White, Color.Black);
    public PortraitScreen(Console prev) : base(16, 16) {
        image = new ColoredGlyph[16, 16];
        for (int y = 0; y < 16; y++) {
            for (int x = 0; x < 16; x++) {
                image[x, y] = empty;
            }
        }
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        for (int y = 0; y < 16; y++) {
            for (int x = 0; x < 16; x++) {
                this.SetCellAppearance(x, y, image[x, y]);
            }
        }
        if ((int)time % 2 == 0) {
            this.SetCellAppearance(cursor.X, cursor.Y, new ColoredGlyph(Color.White, Color.Black, '_'));
        }
        base.Render(delta);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var pressed in keyboard.KeysPressed) {
            switch (pressed.Key) {
                case Keys.Right:
                    if (cursor.X < 15)
                        cursor += new Point(1, 0);
                    break;
                case Keys.Left:
                    if (cursor.X > 0)
                        cursor -= new Point(1, 0);
                    break;
                case Keys.Up:
                    if (cursor.Y < 15)
                        cursor += new Point(0, 1);
                    break;
                case Keys.Down:
                    if (cursor.Y > 0)
                        cursor += new Point(0, -1);
                    break;
                case Keys.Back:
                case Keys.Space:
                    image[cursor.X, cursor.Y] = empty;
                    break;
                default:
                    if (pressed.Character != 0 && pressed.Character != ' ') {
                        image[cursor.X, cursor.Y] = new ColoredGlyph(Color.White, Color.Black, pressed.Character);
                    }
                    break;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }
}
