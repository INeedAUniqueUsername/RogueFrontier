
using LibSadConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

namespace ASECII {
    public class HistoryMenu : Console {
        SpriteModel model;
        List<LabelButton> buttons;
        ScrollVertical scroll;
        MouseWatch mouse;

        DateTime lastScroll;
        int historyLength => model.Undo.Count + model.Redo.Count;
        public HistoryMenu(int width, int height, SpriteModel model) : base(width, height) {
            this.model = model;
            buttons = new List<LabelButton>();
            scroll = new ScrollVertical(height, historyLength, Scrolled) { Position = new Point(width - 1, 0) };
            this.Children.Add(scroll);
            mouse = new MouseWatch();

            void Scrolled() {
                lastScroll = DateTime.Now;
            }
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);

            if(lastScroll != DateTime.UnixEpoch && (DateTime.Now - lastScroll).TotalSeconds > 1) {
                lastScroll = DateTime.UnixEpoch;
                UpdateListing();
            }
        }

        public void SnapIndex() {
            scroll.index = Math.Max(0, model.Undo.Count - Math.Max(0, Height - model.Redo.Count));

            //Math.Max(0, model.Undo.Count);
            //Math.Max(0, model.Undo.Count - Height + model.Redo.Count);
        }
        public void HistoryChanged() {
            scroll.range = historyLength;
            UpdateListing();
        }
        public void UpdateListing() {
            Children.Clear();
            buttons.Clear();

            int y = 0;
            foreach(var e in model.Undo.Skip(scroll.index).Take(Height - y)) {
                var b = new LabelButton($"<{e.Name}", () => UndoTo(e)) { Position = new Point(0, y) };
                Children.Add(b);
                buttons.Add(b);
                y++;
            }
            
            foreach (var e in model.Redo.Reverse().Skip(scroll.index - model.Undo.Count()).Take(Height - y)) {
                var b = new LabelButton($">{e.Name}", () => RedoTo(e)) { Position = new Point(0, y) };
                Children.Add(b);
                buttons.Add(b);
                y++;
            }
        }
        public void UndoTo(Edit e) {
            Edit current;
            do {
                current = model.Undo.Last();
                model.Undo.RemoveLast();
                current.Undo();
                model.Redo.AddLast(current);
            } while (current != e);

            UpdateListing();
        }
        public void RedoTo(Edit e) {
            Edit current;
            do {
                current = model.Redo.Last();
                model.Redo.RemoveLast();
                current.Do();
                model.Undo.AddLast(current);
            } while (current != e);

            UpdateListing();
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            int deltaScroll = state.Mouse.ScrollWheelValueChange / 60;
            if(deltaScroll != 0) {
                scroll.index += deltaScroll;
                UpdateListing();
            }
            mouse.Update(state, IsMouseOver);
            if (mouse.left == ClickState.Held && mouse.leftPressedOnScreen) {
                var deltaY = mouse.prevPos.Y - mouse.nowPos.Y;
                if (deltaY != 0) {
                    scroll.index += deltaY;
                }
            }

            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            var mid = model.Undo.Skip(scroll.index).Count();
            if(mid > 0 && mid < Height) {
                this.Print(0, mid, new string('-', 16));
            } else if(mid <= 0) {
                this.Print(0, 0, new string('^', 16));
            } else if(mid >= Height) {
                this.Print(0, Height-1, new string('v', 16));
            }
            base.Render(delta);
        }
    }
}
