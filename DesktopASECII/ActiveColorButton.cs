using ArchConsole;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;

using SadRogue.Primitives;
namespace ASECII {
    public class ColorLabel : SadConsole.Console {
        Func<Color> color;
        public ColorLabel(int width, Func<Color> color) : base(width, 1) {
            this.color = color;
        }
        public override void Render(TimeSpan timeElapsed) {
            var b = color();
            this.Print(0, 0, new string(' ', Width), Color.Transparent, b);
            base.Render(timeElapsed);
        }
    }
    public class ColorCellButton : SadConsole.Console {
        Func<Color> color;
        Action click;
        MouseWatch mouse;
        public char ch;
        public ColorCellButton(Func<Color> color, Action click, char ch = '+') : base(1, 1) {
            this.color = color;
            this.click = click;
            this.mouse = new MouseWatch();
            this.ch = ch;
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (IsMouseOver) {
                if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {
                    click();
                }
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            var f = color();
            var b = f.GetTextColor();
            if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
                this.Print(0, 0, $"{ch}", b, f);
            } else {
                this.Print(0, 0, $"{ch}", f, b);
            }
            base.Render(timeElapsed);
        }
    }
    public class ColorButton : SadConsole.Console {
        public string text {
            set {
                _text = value;
                Resize(_text.Length, 1, _text.Length, 1, false);
            }
            get { return _text; }
        }
        private string _text;
        MouseWatch mouse;

        public Func<Color> color;
        public Action leftClick;
        public Action rightClick;

        public ColorButton(string text, Func<Color> color, Action leftClick, Action rightClick = null) : base(1, 1) {
            this.text = text;
            this.color = color;
            this.leftClick = leftClick;
            this.rightClick = rightClick;
            this.mouse = new MouseWatch();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (IsMouseOver) {
                if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {
                    leftClick();
                }
                if (mouse.rightPressedOnScreen && mouse.right == ClickState.Released) {
                    rightClick();
                }

            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            var f = color();
            var b = f.GetTextColor();
            if (IsMouseOver && (
                (mouse.nowLeft && mouse.leftPressedOnScreen) ||
                (mouse.nowRight && mouse.rightPressedOnScreen)
                )) {
                this.Print(0, 0, text.PadRight(Width), b, f);
            } else {
                this.Print(0, 0, text.PadRight(Width), f, b);
            }
            base.Render(timeElapsed);
        }
    }
    public class ActiveColorButton : SadConsole.Console {
        public string text {
            set {
                _text = value;
                Resize(_text.Length, 1, _text.Length, 1, false);
            }
            get { return _text; }
        }
        private string _text;
        MouseWatch mouse;

        public Func<bool> active;
        public Func<Color> color;
        public Action click;

        public bool IsActive;

        public ActiveColorButton(string text, Func<bool> active, Func<Color> color, Action click) : base(1, 1) {
            this.text = text;
            this.active = active;
            this.color = color;
            this.click = click;
            this.mouse = new MouseWatch();
        }
        public void UpdateActive() {
            IsActive = active();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (IsActive) {
                if (IsMouseOver) {
                    if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {
                        click();
                    }
                }
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            var b = color();
            var f = b.GetTextColor();
            if (IsActive) {
                if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
                    this.Print(0, 0, text.PadRight(Width), b, f);
                } else {
                    this.Print(0, 0, text.PadRight(Width), f, b);
                }
            } else {
                this.Print(0, 0, new string(' ', Width), f, b);
            }


            base.Render(timeElapsed);
        }
    }



    public class ActiveLabelButton : SadConsole.Console {
        public string text {
            set {
                _text = value;
                Resize(_text.Length, 1, _text.Length, 1, false);
            }
            get { return _text; }
        }
        private string _text;
        MouseWatch mouse;

        public Func<bool> active;
        public Func<Color> color;
        public Action click;

        public bool IsActive;

        public ActiveLabelButton(string text, Func<bool> active, Action click) : base(1, 1) {
            this.text = text;
            this.active = active;
            this.click = click;
            this.mouse = new MouseWatch();
        }
        public void UpdateActive() {
            IsActive = active();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (IsActive) {
                if (IsMouseOver) {
                    if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {
                        click();
                    }
                }
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            var f = Color.White;
            var b = Color.Black;
            if (IsActive) {
                if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
                    this.Print(0, 0, text.PadRight(Width), b, f);
                } else {
                    this.Print(0, 0, text.PadRight(Width), f, b);
                }
            } else {
                this.Print(0, 0, new string(' ', Width), f, b);
            }


            base.Render(timeElapsed);
        }
    }
}
