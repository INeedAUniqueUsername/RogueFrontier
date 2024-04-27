using SadRogue.Primitives;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchConsole;

namespace ASECII {
    class PickerMenu : SadConsole.Console {
        SpriteModel model;
        MouseWatch mouse;
        PickerModel colorPicker;
        Action colorPicked;
        public PickerMenu(int width, int height, SpriteModel model, PickerModel colorGrid, Action colorPicked) : base(width, height) {
            this.model = model;
            this.mouse = new MouseWatch();
            this.colorPicker = colorGrid;
            this.colorPicked = colorPicked;
        }
        public override bool ProcessKeyboard(Keyboard info) {
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject) {

                //If the mouse enters the screen held but the hold started from outside the screen, we ignore it
                var (x, y) = state.SurfaceCellPosition;
                var colors = colorPicker.colors;
                if (state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                    model.brush.foreground = colors[x, y];
                    colorPicker.foregroundPoint = new Point(x, y);

                    colorPicked?.Invoke();
                }
                if (state.Mouse.RightButtonDown && mouse.rightPressedOnScreen) {
                    model.brush.background = colors[x, y];
                    colorPicker.backgroundPoint = new Point(x, y);

                    colorPicked?.Invoke();
                }
            }
            
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            this.Clear();
            var colors = colorPicker.colors;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    this.SetCellAppearance(x, y, new ColoredGlyph(Color.Transparent, colors[x, y]));
                }
            }

            foreach (var (x, y) in colorPicker.palettePoints) {
                var c = colors[x, y];
                var f = c.GetTextColor();
                this.SetCellAppearance(x, y, new ColoredGlyph(f, c, '.'));
            }
            var backgroundPoint = colorPicker.backgroundPoint;
            var foregroundPoint = colorPicker.foregroundPoint;
            if (foregroundPoint != null || backgroundPoint != null) {
                if (foregroundPoint == backgroundPoint) {
                    var (x, y) = foregroundPoint.Value;
                    var c = colors[x, y];
                    var f = c.GetTextColor();
                    this.SetCellAppearance(x, y, new ColoredGlyph(f, c, 'X'));
                } else {
                    if (foregroundPoint != null) {
                        var (x, y) = foregroundPoint.Value;
                        var c = colors[x, y];
                        var f = c.GetTextColor();
                        this.SetCellAppearance(x, y, new ColoredGlyph(f, c, 'F'));
                    }
                    if (backgroundPoint != null) {
                        var (x, y) = backgroundPoint.Value;
                        var c = colors[x, y];
                        var f = c.GetTextColor();
                        this.SetCellAppearance(x, y, new ColoredGlyph(f, c, 'B'));
                    }
                }
            }
            base.Render(timeElapsed);
        }
    }
    class PickerModel {
        SpriteModel model;
        int Width, Height;
        public double hue;
        public Color[,] colors;
        public HashSet<Point> palettePoints;
        public Point? foregroundPoint;
        public Point? backgroundPoint;

        public PickerModel(int Width, int Height, SpriteModel model) {
            this.model = model;
            this.Width = Width;
            this.Height = Height;
            colors = new Color[Width, Height];
        }
        public void UpdateColors() {
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var c = Helper.HsvToRgb(hue, 1f * x / (Width - 1), 1f * y / (Height - 1));
                    colors[x, Height - y - 1] = c;
                }
            }
        }

        public void UpdateBrushPoints(PaletteModel paletteModel) {
            foregroundPoint = null;
            backgroundPoint = null;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var c = colors[x, y];
                    var p = new Point(x, y);
                    if (model.brush.foreground == c) {
                        foregroundPoint = p;
                    }
                    if (model.brush.background == c) {
                        backgroundPoint = p;
                    }
                }
            }
        }
        public void UpdatePalettePoints(PaletteModel paletteModel) {
            var palette = paletteModel.paletteSet;
            palettePoints = new HashSet<Point>();
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var c = colors[x, y];
                    var p = new Point(x, y);
                    if (palette.Contains(c)) {
                        palettePoints.Add(p);
                    }
                }
            }
        }
    }
    class HueBar : SadConsole.Console {
        PickerModel colorPicker;
        PaletteModel paletteModel;
        int index = 0;
        Color[] bar;

        MouseWatch mouse;

        public HueBar(int width, int height, PaletteModel paletteModel, PickerModel model) : base(width, height) {
            this.paletteModel = paletteModel;
            this.colorPicker = model;
            bar = new Color[width];
            for (int i = 0; i < width; i++) {
                bar[i] = Helper.HsvToRgb(i * 360d / width, 1.0, 1.0);
            }
            mouse = new MouseWatch();
        }

        public override bool ProcessMouse(MouseScreenObjectState state) {
            var (x, y) = state.SurfaceCellPosition;
            if (state.IsOnScreenObject) {
                mouse.Update(state, IsMouseOver);
                if (state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                    index = x;
                    colorPicker.hue = index * 360d / bar.Length;
                    colorPicker.UpdateColors();
                    colorPicker.UpdateBrushPoints(paletteModel);
                    colorPicker.UpdatePalettePoints(paletteModel);
                }
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            for (int x = 0; x < Width; x++) {
                this.Print(x, 0, " ", Color.Transparent, bar[x]);
            }
            var c = this.GetCellAppearance(index, 0).Background;
            var g = c.GetTextColor();
            this.SetCellAppearance(index, 0, new ColoredGlyph(g, c, '*'));

            base.Render(timeElapsed);
        }
    }

    enum Channel {
        Red, Green, Blue, Alpha,
        Hue, Saturation, Value,
        Gray
    }
    class ChannelBar : SadConsole.Console {
        Channel channel;
        int index = 0;
        Color[] bar;
        Color selectedColor => bar[index];

        MouseWatch mouse;

        Action indexChanged;

        public ChannelBar(int width, Channel channel, Action indexChanged) : base(width, 1) {
            this.channel = channel;
            bar = new Color[width + 1];
            mouse = new MouseWatch();
            this.indexChanged = indexChanged;
        }

        public void ModifyColor(ref Color c) => c = bar[index];

        public delegate Color SetChannel(Color c, byte amount);

        private SetChannel GetModifier() => channel switch {
            Channel.Red => ColorExtensions2.SetRed,
            Channel.Green => ColorExtensions2.SetGreen,
            Channel.Blue => ColorExtensions2.SetBlue,
            Channel.Alpha => ColorExtensions2.SetAlpha,

            Channel.Hue => (c, b) => Helper.HsvToRgb(b * 360d / 255, c.GetSat(), c.GetValue()).SetAlpha(c.A),
            Channel.Saturation => (c, b) => Helper.HsvToRgb(c.GetHue(), b / 255f, c.GetValue()).SetAlpha(c.A),
            Channel.Value => (c, b) => Helper.HsvToRgb(c.GetHue(), c.GetSat(), b / 255f).SetAlpha(c.A),

            Channel.Gray => (c, b) => new Color(b, b, b, c.A)
        };
        private int GetIndex(Color c) => channel switch
        {
            Channel.Red => c.R,
            Channel.Green => c.G,
            Channel.Blue => c.B,
            Channel.Alpha => c.A,

            Channel.Hue => (int) (c.GetHue() * 255 / 360),
            Channel.Saturation => (int) (c.GetSat() * 255),
            Channel.Value => (int) (c.GetValue() * 255),

            Channel.Gray => (int) (c.R + c.G + c.B) / 3
        } * (Width - 1) / 255;

        public void UpdateColors(Color source) {
            SetChannel func = GetModifier();

            for (int i = 0; i < Width + 1; i++) {
                bar[i] = func(source, (byte)(i * 255 / Width));
            }
            index = GetIndex(source);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject && state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                var (x, y) = state.SurfaceCellPosition;
                index = x + 1;

                indexChanged();
            }
            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            for (int x = 0; x < Width; x++) {
                this.Print(x, 0, " ", Color.Transparent, bar[x]);
            }
            var c = bar[index];
            var back = Color.Black;
            var g = c.Blend(back).GetTextColor();
            this.SetCellAppearance(index, 0, new ColoredGlyph(g, c, '*'));

            base.Render(timeElapsed);
        }
    }
}
