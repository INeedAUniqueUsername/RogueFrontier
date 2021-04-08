using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

namespace ArchConsole {
    public class CellButton : SadConsole.Console {
        public delegate bool Active();
        public delegate void Click();
        Active active;
        bool isActive;
        Click click;
        MouseWatch mouse;

        char ch;
        public CellButton(Active active, Click click, char ch = '+') : base(1, 1) {
            this.active = active;
            this.click = click;
            this.mouse = new MouseWatch();

            this.ch = ch;

            UpdateActive();
        }
        public void UpdateActive() {
            isActive = active();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (isActive && IsMouseOver) {
                if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {
                    click();
                }

            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
                this.Print(0, 0, $"{ch}", Color.White, Color.Black);
            } else if (isActive) {
                this.Print(0, 0, $"{ch}", Color.Black, IsMouseOver ? Color.Yellow : Color.White);
            } else {
                this.Print(0, 0, " ", Color.Transparent, Color.Gray);
            }


            base.Render(timeElapsed);
        }
    }
    public class ListItem<T> {
        public string name;
        public T item;
        public ListItem(string name, T item) {
            this.name = name;
            this.item = item;
        }
        public static implicit operator T(ListItem<T> i) => i.item;
    }
    public class TextField : Console {
        public int index {
            get => _index;
            set {
                _index = Math.Clamp(value, 0, text.Length);
                UpdateTextStart();
            }
        }
        private int _index;
        private int textStart;

        public string _text;
        public string text { get => _text; set {
                _text = value;
                TextChanged?.Invoke(this);
            } }
        public string placeholder;
        private double time;
        private MouseWatch mouse;

        public Predicate<char> CharFilter;
        public Action<TextField> TextChanged;
        public Action<TextField> EnterPressed;
        public TextField(int Width) : base(Width, 1) {
            _index = 0;
            _text = "";
            placeholder = new string('.', Width);
            time = 0;
            mouse = new MouseWatch();
            FocusOnMouseClick = true;
        }
        public void UpdateTextStart() {
            textStart = Math.Max(Math.Min(text.Length, _index) - Width + 1, 0);
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds;
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            this.Clear();


            var text = this.text;
            var showPlaceholder = this.text.Length == 0 && !IsFocused;
            if (showPlaceholder) {
                text = placeholder;
            }
            int x2 = Math.Min(text.Length - textStart, Width);

            bool showCursor = time % 2 < 1;

            Color foreground = IsMouseOver ? Color.Yellow : Color.White;
            Color background = IsFocused ? new Color(51, 51, 51, 255) : Color.Black;

            if ((mouse.leftPressedOnScreen && mouse.left == ClickState.Held) ||
                    (mouse.rightPressedOnScreen && mouse.right == ClickState.Held)) {
                (foreground, background) = (background, foreground);
            }
            for (int x = 0; x < Width; x++) {
                this.SetBackground(x, 0, background);
            }
            Func<int, ColoredGlyph> getGlyph = (i) => new ColoredGlyph(foreground, background, text[i]);
            if (showCursor && IsFocused) {
                if (_index < text.Length) {
                    getGlyph = i =>
                               i == _index ? new ColoredGlyph(background, foreground, text[i])
                                           : new ColoredGlyph(foreground, background, text[i]);
                } else {
                    this.SetBackground(x2, 0, foreground);
                }
            }
            for (int x = 0; x < x2; x++) {
                var i = textStart + x;
                this.SetCellAppearance(x, 0, getGlyph(i));
            }
            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.KeysPressed.Any()) {
                //bool moved = false;
                foreach (var key in keyboard.KeysPressed) {
                    switch (key.Key) {
                        case Keys.Up:
                            _index = 0;
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Down:
                            _index = text.Length;
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Right:
                            _index = Math.Min(_index + 1, text.Length);
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Left:
                            _index = Math.Max(_index - 1, 0);
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Home:
                            _index = 0;
                            time = 0;
                            break;
                        case Keys.End:
                            _index = text.Length;
                            time = 0;
                            break;
                        case Keys.Back:
                            if (text.Length > 0) {
                                if (_index > 0) {
                                    if (_index >= text.Length) {
                                        text = text.Substring(0, text.Length - 1);
                                    } else if (_index > 0) {
                                        var l = text.Length;
                                        var before = text.Substring(0, _index - 1);
                                        var after = text.Substring(_index);
                                        text = before + after;
                                    }
                                    _index--;
                                }
                                time = 0;
                                UpdateTextStart();
                            }

                            break;
                        case Keys.Enter:
                            EnterPressed?.Invoke(this);
                            break;
                        default:
                            if (key.Character != 0) {
                                if (CharFilter?.Invoke(key.Character) != false) {
                                    if (_index == text.Length) {
                                        text += key.Character;
                                        _index++;
                                    } else if (_index > 0) {
                                        text = text.Substring(0, index) + key.Character + text.Substring(index);
                                        _index++;
                                    } else {
                                        text = (key.Character) + text;
                                        _index++;
                                    }
                                    time = 0;
                                    UpdateTextStart();
                                }
                            }
                            break;
                    }
                }
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if(IsMouseOver && mouse.nowLeft) {
                _index = Math.Min(mouse.nowPos.X, text.Length);
                time = 0;
            }
            return base.ProcessMouse(state);
        }
    }

    public class ButtonList {
        public Console Parent;
        public Point Position;
        public List<LabelButton> buttons;
        public ButtonList(Console Parent, Point Position) {
            this.Parent = Parent;
            this.Position = Position;
            buttons = new List<LabelButton>();
        }
        public void Add(string label, Action clicked) {
            var b = new LabelButton(label, clicked) {
                Position = Position + new Point(0, buttons.Count),
            };
            buttons.Add(b);
            Parent.Children.Add(b);
        }
        public void Clear() {
            foreach (var b in buttons) {
                Parent.Children.Remove(b);
            }
            buttons.Clear();
        }
    }
    public class KeyedButtonList : Console {
        public ButtonList buttons;
        public int navIndex;
        public KeyedButtonList(Console parent, Point position) : base(parent.Width, parent.Height) {
            buttons = new ButtonList(this, position);
        }
        public override void Render(TimeSpan delta) {
            var (x, y) = buttons.Position;
            this.Print(x - 4, y + navIndex, new ColoredString("--->", Color.Yellow, Color.Transparent));
            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            var b = buttons.buttons;
            if (keyboard.IsKeyDown(Keys.Enter)) {
                b[navIndex].leftClick?.Invoke();
            }
            if (keyboard.IsKeyPressed(Keys.Up)) {
                navIndex = (navIndex - 1 + b.Count) % b.Count;
            }
            if (keyboard.IsKeyPressed(Keys.Down)) {
                navIndex = (navIndex + 1) % b.Count;
            }
            return base.ProcessKeyboard(keyboard);
        }
    }

    public class LabeledField : ControlsConsole {
        public string label;
        public TextField textBox;
        public LabeledField(string label, string text = "", Action<TextField, string> TextChanged = null) : base((label.Length / 8 + 1) * 8 + 16, 1) {
            (DefaultBackground, DefaultForeground) = (Color.Black, Color.White);
            this.label = label;
            this.textBox = new TextField(16) {
                text = text,
                Position = new Point((label.Length / 8 + 1) * 8, 0),
            };
            if (TextChanged != null)
                this.textBox.TextChanged += (e) => TextChanged?.Invoke(this.textBox, this.textBox.text);
            this.Children.Add(textBox);
            this.FocusOnMouseClick = true;
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.IsKeyPressed(Keys.Enter)) {
                this.IsFocused = false;
                textBox.IsFocused = false;
                this.Parent.IsFocused = true;
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            this.Print(0, 0, label, Color.White, Color.Black);
            base.Render(delta);
        }
    }
    public class Label : SadConsole.Console {
        public ColoredString text {
            set {
                _text = value;
                Resize(_text.Count, 1, _text.Count, 1, false);
            }
            get {
                return _text;
            }
        }
        private ColoredString _text;
        public Label(string text) : base(text.Length, 1) {
            this.text = new ColoredString(text);
        }
        public override void Render(TimeSpan delta) {
            this.Print(0, 0, text);
            base.Render(delta);
        }
    }
    public class LabelButton : SadConsole.Console {
        public string text {
            set {
                _text = value;
                Resize(_text.Length, 1, _text.Length, 1, false);
            }
            get { return _text; }
        }
        private string _text;
        public Action leftClick;
        public Action rightClick;


        public Action leftHold;
        public Action rightHold;

        MouseWatch mouse;

        public LabelButton(string text, Action leftClick = null, Action rightClick = null) : base(1, 1) {
            this.text = text;
            this.leftClick = leftClick;
            this.rightClick = rightClick;
            this.mouse = new MouseWatch();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (IsMouseOver) {
                if (mouse.leftPressedOnScreen) {
                    switch(mouse.left) {
                        case ClickState.Released:
                            leftClick?.Invoke();
                            break;
                        case ClickState.Held:
                            leftHold?.Invoke();
                            break;
                    }
                }

                if(mouse.rightPressedOnScreen) {
                    switch (mouse.left) {
                        case ClickState.Released:
                            rightClick?.Invoke();
                            break;
                        case ClickState.Held:
                            rightHold?.Invoke();
                            break;
                    }
                }
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
                this.Print(0, 0, text, Color.Black, Color.White);
            } else {
                this.Print(0, 0, text, Color.White, IsMouseOver ? Color.Gray : Color.Black);
            }


            base.Render(timeElapsed);
        }
    }

    public class ScrollVertical : Console {
        private int _index;

        public int index {
            get => _index; set {
                _index = value;
                ClampIndex();
                UpdateButtons();
            }
        }
        private int _range;
        public int range { get => _range; set {
                _range = value;
                ClampIndex();
                UpdateButtons();
            }
        }

        Action scrolled;
        CellButton up, down;

        MouseWatch mouse;
        private int yLastPressedOnBar;
        private int scrollStep => range / Height;
        public ScrollVertical(int height, int range, Action scrolled) : base(1, height) {
            this._index = 0;
            this._range = range;
            this.scrolled = scrolled;

            int delta = Height / 2;
            up = new CellButton(() => _index > 0, () => Up(delta), '-') { Position = new Point(0, 0) };
            down = new CellButton(() => _index < range, () => Down(delta), '+') { Position = new Point(0, Height - 1) };
            this.Children.Add(up);
            this.Children.Add(down);
            UpdateButtons();

            mouse = new MouseWatch();
            yLastPressedOnBar = -1;
        }

        public void Up(int delta) {
            index -= delta;
            scrolled?.Invoke();
        }
        public void Down(int delta) {
            index += delta;
            scrolled?.Invoke();
        }
        private void UpdateButtons() {
            up.UpdateActive();
            down.UpdateActive();
        }

        public void ClampIndex() {
            _index = Math.Clamp(_index, 0, Math.Max(0, range - Height));
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            var delta = state.Mouse.ScrollWheelValueChange / 60;
            if (delta != 0) {
                index += delta;
                scrolled?.Invoke();
            }
            mouse.Update(state, IsMouseOver);
            if(GetBar(out int barStart, out int barSize) && mouse.leftPressedOnScreen) {
                if(mouse.left == ClickState.Pressed) {
                    var y = state.SurfaceCellPosition.Y;
                    if (y < barStart || y > barStart + barSize) {
                        index = (range - Height) * y / Height - Height / 2;
                        yLastPressedOnBar = -1;
                    } else {
                        yLastPressedOnBar = y;
                    }
                } else if(mouse.left == ClickState.Held) {
                    var y = state.SurfaceCellPosition.Y;
                    if (yLastPressedOnBar == -1) {
                        index = (range - Height) * y / Height - Height / 2;
                    } else {
                        if (y != yLastPressedOnBar) {
                            index += (y - yLastPressedOnBar) * scrollStep;
                            yLastPressedOnBar = y;
                        }
                    }
                }
            }
            return base.ProcessMouse(state);
        }

        public bool GetBar(out int barStart, out int barSize) {
            if (range > Height) {
                barStart = Height * index / range;
                barSize = Height * Height / range;
                return true;
            } else {
                barStart = 0;
                barSize = Height;
                return false;
            }
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            
            if (GetBar(out int barStart, out int barSize)) {

                for (int i = 0; i < barStart; i++) {
                    this.Print(0, i, new ColoredGlyph(Color.White, Color.Black, '|'));
                }
                for (int i = barStart; i < barStart + barSize; i++) {
                    this.Print(0, i, new ColoredGlyph(Color.White, Color.Black, '#'));
                }
                for (int i = barStart + barSize; i < Height; i++) {
                    this.Print(0, i, new ColoredGlyph(Color.White, Color.Black, '|'));
                }
            } else {
                for (int i = 0; i < range; i++) {
                    this.Print(0, i, new ColoredGlyph(Color.White, Color.Black, '|'));
                }
            }
            base.Render(delta);
        }
    }
}