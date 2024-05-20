using LibSadConsole;
using Newtonsoft.Json;
using SadConsole;
using SadConsole.Input;
using SadConsole.SerializedTypes;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static SadConsole.Input.Keys;
using Console = SadConsole.Console;
using SadConsole.Renderers;
using Label = LibSadConsole.Label;
using LibGamer;

namespace ASECII {
    class EditorMain : SadConsole.Console {
        SpriteModel model;
        SpriteMenu spriteMenu;
        Console controlsMenu;
        HistoryMenu historyMenu;
        public EditorMain(int width, int height, SpriteModel model) :base(width, height) {
            UseKeyboard = true;
            UseMouse = true;

            this.model = model;

            InitUI();

            spriteMenu.Position = new Point(controlsMenu.Width, 0);
            controlsMenu.Position = new Point(0, 0);

            this.Children.Add(spriteMenu);
            this.Children.Add(controlsMenu);
			Settings.WindowTitle = $"ASECII: {model.filepath}";
		}
        public void InitControls() {
            int x = 0;
            int y = 0;

            var tileModel = model.tiles;
            var paletteModel = model.palette;
            var pickerModel = new PickerModel(16, 16, model);

            pickerModel.UpdateColors();
            pickerModel.UpdateBrushPoints(paletteModel);
            pickerModel.UpdatePalettePoints(paletteModel);
            controlsMenu = new Console(16, Height) {
                UseKeyboard = true,
                UseMouse = true,
            };

            Action UpdateChannels = () => { };
            Action UpdateColorButtons = () => { };

            LabelButton tileButton = null;

            ColorLabel foregroundLabel = null, backgroundLabel = null;
            CellButton foregroundAddButton = null, backgroundAddButton = null;
            CellButton foregroundRemoveButton = null, backgroundRemoveButton = null;

            LabelButton infiniteButton = null;
            TextField widthField = null, heightField = null;
            infiniteButton = new LabelButton(model.infinite ? "Infinite" : "Size", () => {
                model.infinite = !model.infinite;
                //infiniteButton.text = model.infinite ? "Infinite" : "Finite";
                ResetControls();
            }) { Position = new Point(x, y) };
            controlsMenu.Children.Add(infiniteButton);
            if (!model.infinite) {
                widthField = new TextField(5) {
                    text = model.width.ToString(),
                    CharFilter = c => char.IsNumber(c),
                    TextChanged = f => {
                        f.text.TrimStart('0');

                        model.width = f.text.Any() ? int.Parse(f.text) : 0;
                    },
                    Position = new(x+5, y)
                };
                heightField = new TextField(5) {
                    text = model.height.ToString(),
                    CharFilter = c => char.IsNumber(c),
                    TextChanged = f => {
                        var s = f.text.TrimStart('0');
                        model.height = f.text.Any() ? int.Parse(f.text) : 0;
                    },
                    Position = new(x + 11, y)
                };
                controlsMenu.Children.Add(widthField);
                controlsMenu.Children.Add(heightField);
            }

            y++;

            LabelButton colorModeButton = null;
            colorModeButton = new LabelButton((model.colorMode switch
            {
                ColorMode.RGB =>       "RGB      ",
                ColorMode.Grayscale => "Grayscale",
                ColorMode.Notepad =>   "Notepad  ",
                _ => "Color Mode"
            }), ChangeColorMode) { Position = new Point(0, y) };
            controlsMenu.Children.Add(colorModeButton);

            tileButton = new LabelButton("+", null) {
                Position = new Point(15, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(tileButton);




            y++;
            void ChangeColorMode() {
                model.colorMode = (ColorMode) ((int)(model.colorMode + 1) % Enum.GetValues(typeof(ColorMode)).Length);
                ResetControls();
            }
            void ResetControls() {
                this.Children.Remove(controlsMenu);
                InitControls();
                this.Children.Add(controlsMenu);
            }

            var tileMenu = new TileMenu(controlsMenu.Width, 4, model, tileModel, () => {
                tileModel.UpdateIndexes(model);
                UpdateTileSetButtons();

                paletteModel.UpdateIndexes(model);
                PaletteChanged();

                UpdateChannels();
            }) {
                Position = new Point(0, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(tileMenu);


            y += 4;

            void UpdateTileSetButtons() {
                if (tileModel.brushIndex > -1) {
                    tileButton.text = "-";
                    tileButton.leftClick = () => {
                        tileModel.RemoveTile(tileModel.brushIndex.Value);
                        tileModel.UpdateIndexes(model);
                        UpdateTileSetButtons();
                    };
                } else {
                    tileButton.text = "+";
                    tileButton.leftClick = () => {
                        tileModel.AddTile(model.brush.cell);
                        tileModel.UpdateIndexes(model);
                        UpdateTileSetButtons();
                    };
                }
            }
            /*
            tileButton = new ActiveColorButton("Add Tile", () => {
                var c = model.brush.cell;
                return tileModel.brushIndex == null;
            }, () => {
                return model.brush.cell.Background;
            }, () => {
                tileModel.AddTile(model.brush.cell);
                tileModel.UpdateIndexes(model);
                tileButton.UpdateActive();
            }) {
                Position = new Point(0, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            */

            int rows = Font.TotalGlyphs / 16;
            var glyphMenu = new GlyphMenu(16, rows, model, () => {
                tileModel.UpdateIndexes(model);
                UpdateTileSetButtons();
            }) {
                Position = new Point(0, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(glyphMenu);

            y += rows;

            Console colorMenu = null;
            int yColorMenu = y;
            int yColor = 0;

            void AddPaletteMenu() {
                var paletteMenu = new PaletteMenu(16, 4, model, paletteModel, () => {
                    tileModel.UpdateIndexes(model);
                    UpdateTileSetButtons();
                    pickerModel.UpdateBrushPoints(paletteModel);
                    PaletteChanged();

                    UpdateChannels();
                }) {
                    Position = new Point(0, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                colorMenu.Children.Add(paletteMenu);

                yColor += 4;
            }
            void AddForegroundLabel() {
                foregroundRemoveButton = new CellButton(() => paletteModel.foregroundIndex != null, () => {
                        paletteModel.RemoveColor(model.brush.foreground);
                        paletteModel.UpdateIndexes(model);
                        PaletteChanged();
                }, '-') {
                    Position = new Point(0, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                foregroundLabel = new ColorLabel(14, () => model.brush.foreground) {
                    Position = new Point(1, yColor)
                };
                foregroundAddButton = new CellButton(() => paletteModel.foregroundIndex == null, () => {
                    paletteModel.AddColor(model.brush.foreground);
                    paletteModel.UpdateIndexes(model);

                    PaletteChanged();
                }, '+') {
                    Position = new Point(15, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                UpdateColorButtons += foregroundRemoveButton.UpdateActive;
                UpdateColorButtons += foregroundAddButton.UpdateActive;

                colorMenu.Children.Add(foregroundLabel);
                colorMenu.Children.Add(foregroundAddButton);
                colorMenu.Children.Add(foregroundRemoveButton);

                yColor++;
            }
            void AddBackgroundLabel() {
                backgroundRemoveButton = new CellButton(() => {
                    return paletteModel.backgroundIndex != null;
                }, () => {
                    paletteModel.RemoveColor(model.brush.background);
                    paletteModel.UpdateIndexes(model);
                    PaletteChanged();
                }, '-') {
                    Position = new Point(0, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                backgroundLabel = new ColorLabel(14, () => model.brush.background) {
                    Position = new Point(1, yColor)
                };
                backgroundAddButton = new CellButton(() => {
                    return paletteModel.backgroundIndex == null;
                }, () => {
                    paletteModel.AddColor(model.brush.background);
                    paletteModel.UpdateIndexes(model);
                    PaletteChanged();
                }, '+') {
                    Position = new Point(15, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                UpdateColorButtons += backgroundRemoveButton.UpdateActive;
                UpdateColorButtons += backgroundAddButton.UpdateActive;

                colorMenu.Children.Add(backgroundLabel);
                colorMenu.Children.Add(backgroundAddButton);
                colorMenu.Children.Add(backgroundRemoveButton);

                yColor++;

            }
            ChannelBar Bar(Channel c, Action a) =>
                new(16, c, a) {
                    Position = new Point(0, yColor++),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
            ChannelBar ForeBar(Channel c) {
                ChannelBar bar = null;
                return bar = Bar(c, () => {
                    bar.ModifyColor(ref model.brush.foreground);
                    ChannelChanged();
                });
            }
            ChannelBar BackBar(Channel c) {
                ChannelBar bar = null;
                return bar = Bar(c, () => {
                    bar.ModifyColor(ref model.brush.background);
                    ChannelChanged();
                });
            }
            switch (model.colorMode) {
                case ColorMode.RGB: {
                        bool rgb = true;
                        AddRGB();
                        controlsMenu.Children.Add(colorMenu);
                        void ChangeBarMode() {
                            controlsMenu.Children.Remove(colorMenu);
                            rgb = !rgb;
                            if (rgb) {
                                AddRGB();
                            } else {
                                AddHSB();
                            }
                            UpdateChannels();
                            controlsMenu.Children.Add(colorMenu);
                        }
                        void AddRGB() {
                            colorMenu = new(controlsMenu.Width, 16) { Position = new Point(0, yColorMenu) };
                            yColor = 0;
                            colorMenu.Children.Add(new LabelButton("Color Bars: RGBA", ChangeBarMode) { Position = new Point(0, yColor++) });
                            AddPaletteMenu();

                            var ch = new[] { Channel.Red, Channel.Green, Channel.Blue, Channel.Alpha };
                            AddForegroundLabel();
                            var fore = ch.Select(ForeBar).ToList(); yColor++;
                            fore.ForEach(colorMenu.Children.Add);
                            AddBackgroundLabel();
                            var back = ch.Select(BackBar).ToList(); yColor++;
                            back.ForEach(colorMenu.Children.Add);
                            y++;

                            UpdateChannels = () => {
                                fore.ForEach(f => f.UpdateColors(model.brush.foreground));
                                back.ForEach(b => b.UpdateColors(model.brush.background));
                            };
                            AddPickerMenu();
                        }
                        void AddHSB() {
                            colorMenu = new Console(controlsMenu.Width, 16) { Position = new Point(0, yColorMenu) };
                            yColor = 0;
                            colorMenu.Children.Add(new LabelButton("Color Bars: HSBA", ChangeBarMode) { Position = new Point(0, yColor++) });
                            AddPaletteMenu();

                            var ch = new[] { Channel.Hue, Channel.Saturation, Channel.Value, Channel.Alpha };
                            AddForegroundLabel();
                            var fore = ch.Select(ForeBar).ToList(); yColor++;
                            fore.ForEach(colorMenu.Children.Add);
                            AddBackgroundLabel();
                            var back = ch.Select(BackBar).ToList(); yColor++;
                            back.ForEach(colorMenu.Children.Add);
                            y++;

                            UpdateChannels = () => {
                                fore.ForEach(f => f.UpdateColors(model.brush.foreground));
                                back.ForEach(b => b.UpdateColors(model.brush.background));
                            };
                            AddPickerMenu();
                        }
                        break;
                    }
                case ColorMode.Grayscale: {
                        colorMenu = new Console(controlsMenu.Width, 16) { Position = new Point(0, yColorMenu) };

                        AddPaletteMenu();

                        var ch = new[] { Channel.Gray, Channel.Alpha };
                        AddForegroundLabel();
                        var fore = ch.Select(ForeBar).ToList(); yColor++;
                        fore.ForEach(colorMenu.Children.Add);
                        AddBackgroundLabel();
                        var back = ch.Select(BackBar).ToList(); yColor++;
                        back.ForEach(colorMenu.Children.Add);
                        y++;

                        UpdateChannels = () => {
                            fore.ForEach(f => f.UpdateColors(model.brush.foreground));
                            back.ForEach(b => b.UpdateColors(model.brush.background));
                        };
                        break;
                    }
                case ColorMode.Notepad: {
                        y += 1;
                        break;
                    }
            }
            void ChannelChanged() {
                UpdateChannels();

                tileModel.UpdateIndexes(model);
                UpdateTileSetButtons();

                paletteModel.UpdateIndexes(model);

                UpdateColorButtons();
            }

            void PaletteChanged() {
                UpdateColorButtons();

                pickerModel.UpdateColors();
                pickerModel.UpdateBrushPoints(paletteModel);
                pickerModel.UpdatePalettePoints(paletteModel);
            }


            void AddPickerMenu() {

                var hueBar = new HueBar(16, 1, paletteModel, pickerModel) {
                    Position = new Point(0, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                colorMenu.Children.Add(hueBar);
                yColor++;


                var pickerMenu = new PickerMenu(16, 16, model, pickerModel, () => {
                    tileModel.UpdateIndexes(model);
                    UpdateTileSetButtons();

                    paletteModel.UpdateIndexes(model);

                    UpdateColorButtons();

                    UpdateChannels();
                }) {
                    Position = new Point(0, yColor),
                    FocusOnMouseClick = true,
                    UseMouse = true
                };
                colorMenu.Children.Add(pickerMenu);

                yColor += 16;
                yColor++;
            }

            LayerMenu layerMenu = null;

            y = Height - 8;
            var layerAddButton = new LabelButton("+", () => {

                //model.currentLayer = Math.Min(model.currentLayer, model.sprite.layers.Count - 1);
                model.sprite.layers.Insert(model.currentLayerIndex + 1, new Layer() { name = $"Layer {model.sprite.layers.Count}" });
                layerMenu.UpdateListing();
            }) {
                Position = new Point(0, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(layerAddButton);


            var layerCutButton = new ActiveLabelButton("Cut to Layer", () => model.selection.Exists, () => {
                model.SelectionToLayer();
                layerMenu.UpdateListing();
            }) {
                Position = new Point(4, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(layerCutButton);

            y++;


            layerMenu = new LayerMenu(32, 8, model) {
                Position = new Point(0, y),
                FocusOnMouseClick = true,
                UseMouse = true
            };
            controlsMenu.Children.Add(layerMenu);

            model.selection.selectionChanged = layerCutButton.UpdateActive;
            
            model.brushChanged = () => {
                tileModel.UpdateIndexes(model);
                UpdateTileSetButtons();
                paletteModel.UpdateIndexes(model);
                PaletteChanged();

                UpdateChannels();
                UpdateColorButtons();
            };

            //No per-color transparency; layer-based only

            layerMenu.UpdateListing();

            tileModel.UpdateIndexes(model);
            UpdateTileSetButtons();

            paletteModel.UpdateIndexes(model);
            UpdateColorButtons();
            layerCutButton.UpdateActive();

            UpdateChannels();
        }
        public void InitUI() {
            InitControls();
            historyMenu = new HistoryMenu(16, Height, model);
            historyMenu.UpdateListing();
            model.historyChanged += historyMenu.HistoryChanged;
            spriteMenu = new SpriteMenu(Width - controlsMenu.Width, Height, model) {
                FocusOnMouseClick = true,
                UseMouse = true
            };
            spriteMenu.OnKeyboard += OnKeyboard;
            void OnKeyboard(Keyboard info) {
                
                if (info.IsKeyPressed(Tab)) {
                    bool controlsVisible = this.ToggleChild(controlsMenu);
                    model.camera += new Point((controlsVisible ? 1 : -1) * controlsMenu.Width, 0);
                    PlaceUI();
                }
                if (info.IsKeyPressed(Z) &&
                    model.mode != Mode.Keyboard &&
                    !(info.IsKeyDown(LeftControl) || info.IsKeyDown(LeftShift) || info.IsKeyDown(LeftAlt))) {
                    bool historyVisible = this.ToggleChild(historyMenu);
                    model.camera += new Point((historyVisible ? 1 : -1) * historyMenu.Width, 0);
                    PlaceUI();
                }
                void PlaceUI() {
                    int x = 0;
                    if (Children.Contains(controlsMenu)) {
                        controlsMenu.Position = new Point(x, 0);
                        x += controlsMenu.Width;
                    }
                    if(Children.Contains(historyMenu)) {
                        historyMenu.Position = new Point(x, 0);
                        x += historyMenu.Width;
                    }
                    spriteMenu.Position = new Point(x, 0);
                    spriteMenu.Resize(Width - x, Height, Width - x, Height, false);
                }
            }
        }
        public override bool ProcessKeyboard(Keyboard info) {
            return base.ProcessKeyboard(info);
        }
        public override void Render(TimeSpan timeElapsed) {
            this.Clear();
            base.Render(timeElapsed);
        }
    }

    class SpriteMenu : SadConsole.Console {
        public SpriteModel model;
        public Action<Keyboard> OnKeyboard;
        public SpriteMenu(int width, int height, SpriteModel model) : base(width, height) {
            UseMouse = true;
            UseKeyboard = true;
            this.model = model;
        }
        public override void Update(TimeSpan timeElapsed) {
            base.Update(timeElapsed);
        }
        public override void Render(TimeSpan timeElapsed) {
            /*
            if(model.rendered) {
                return;
            }
            model.rendered = true;
            */

            this.Clear();
            model.ticks++;
            model.ticksSelect++;

            int hx = Width / 2;
            int hy = Height / 2;

            var camera = model.camera - model.pan.offsetPan;

            var c1 = new Color(25, 25, 25);
            var c2 = new Color(51, 51, 51);
            Color GetBackColor(int ax, int ay) => ((ax + ay) % 2 == 0) ? c1 : c2;
            model.sprite.UpdatePreview();
            var center = new Point(hx, hy);
            Color shade = Color.Black.SetAlpha(128);
            if(model.infinite) {
                for (int x = -hx; x < hx + 1; x++) {
                    for (int y = -hy; y < hy + 1; y++) {
                        var pos = camera + new Point(x, y) + center;

                        int ax = center.X + x;
                        int ay = center.Y + y;

                        var back = GetBackColor(ax, ay);
                        if (model.sprite.preview.TryGetValue(pos, out var tile)) {
                            var cg = tile.cg;
                            if(cg.Background.A < 255) {
                                cg = cg.SetBackground(tile.cg.Background.Blend(back));
                            }

                            this.SetCellAppearance(ax, ay, cg);
                        } else {
                            if (model.sprite.InRect(pos)) {
                                this.SetCellAppearance(ax, ay, new ColoredGlyph(Color.Transparent, back));
                            } else {
                                this.SetCellAppearance(ax, ay, new ColoredGlyph(Color.Transparent, shade.Blend(back)));
                            }
                        }
                    }
                }
            } else {
                for (int x = -hx; x < hx + 1; x++) {
                    for (int y = -hy; y < hy + 1; y++) {
                        var pos = camera + new Point(x, y) + center;

                        int ax = center.X + x;
                        int ay = center.Y + y;
                        if (model.InBounds(pos)) {
                            var back = GetBackColor(ax, ay);
                            if (model.sprite.preview.TryGetValue(pos, out var tile)) {
                                var cg = tile.cg;
                                if (cg.Background.A < 255) {
                                    cg = cg.SetBackground(tile.cg.Background.Blend(back));
                                }

                                this.SetCellAppearance(ax, ay, cg);
                            } else {
                                this.SetCellAppearance(ax, ay, new ColoredGlyph(Color.Transparent, back, ' '));
                            }
                        } else {
                            this.SetCellAppearance(ax, ay, new ColoredGlyph(Color.Transparent, Color.Black, ' '));
                        }
                    }
                }
                var origin = new Point(0, 0);
                var p = origin - camera;

                //this.SetCellAppearance(p.X, p.Y, new ColoredGlyph(Color.White, Color.Black, '+'));
            }
            
            if(model.pan.quickPan) {

            } else {
                switch (model.mode) {
                    case Mode.Brush:
                        if (model.ticksSelect % 30 < 15) {
                            DrawSelection();
                        }
                        if ((model.ticks % 30) < 15) {
                            if (IsMouseOver) {
                                model.RenderCursor(this);
                            }
                        }
                        break;
                    case Mode.Line:
                        if ((model.ticks % 15) < 7) {
                            var cell = model.brush.cell;
                            if (model.line.start.HasValue) {
                                var start = model.line.start.Value + camera;
                                var end = model.line.end + camera;
                                if (start == end) {
                                    this.SetCellAppearance(start.X, start.Y, cell);
                                } else {
                                    foreach(var p in model.line.GetPoints()) {
                                        (var x, var y) = p - camera;
                                        var t = cell.cg.SetBackground(this.GetBackground(x, y));
                                        this.SetCellAppearance(x, y, t);
                                    }
                                }
                            } else {
                                model.RenderCursor(this);
                            }
                        }

                        break;
                    case Mode.Erase:
                        if (model.ticksSelect % 30 < 15) {
                            DrawSelection();
                        }
                        if ((model.ticks % 30) < 15) {
                            if (IsMouseOver) {
                                this.SetCellAppearance(model.cursorScreen.X, model.cursorScreen.Y, new ColoredGlyph(Color.Transparent, Color.Transparent));
                            }
                        }
                        break;
                    case Mode.Keyboard:
                        var c = model.brush.cell;
                        if(model.ticksSelect%30 < 15) {
                            DrawSelection();
                        }
                        if (model.ticks % 30 < 15) {
                            var p = (model.keyboard.keyCursor ?? model.cursor) - camera;
                            var t = new ColoredGlyph(c.Foreground, this.GetBackground(p.X, p.Y).Blend(c.Background), '_');
                            this.SetCellAppearance(p.X, p.Y, t);
                        }
                        break;
                    case Mode.SelectRect: {

                            int x = model.cursorScreen.X + 2;
                            int y = model.cursorScreen.Y;

                            var f = model.brush.foreground;
                            var b = model.brush.background;
                            this.Print(x, y, new ColoredString($"{model.cursorScreen.X,4} {model.cursorScreen.Y,4}", f, b));


                            if (model.ticksSelect % 60 < 30) {
                                if(model.selectRect.alt && GetEllipseDisplay(out Ellipse e)) {
                                    y += 1;
                                    this.Print(x, y, new ColoredString($"{e.Width,4} {e.Height,4}", f, b));

                                    DrawSelectionWith(e.Positions());
                                } else if (GetRectDisplay(out Rectangle r)) {
                                    y += 1;
                                    this.Print(x, y, new ColoredString($"{r.Width,4} {r.Height,4}", f, b));

                                    DrawSelection();
                                    DrawRect(r);
                                } else {
                                    var p = model.cursorScreen;

                                    DrawSelection();
                                    DrawBox(p.X, p.Y, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single, w = Line.Single });
                                }
                            } else if (GetRectDisplay(out Rectangle r)) {
                                y += 1;
                                this.Print(x, y, new ColoredString($"{r.Width,4} {r.Height,4}", f, b));
                            }
                            bool GetRectDisplay(out Rectangle r) => model.selectRect.GetAdjustedRect(out r) ||
                                !((r = model.selection.rects.FirstOrDefault(r => r.Contains(model.cursor))).IsEmpty);
                            bool GetEllipseDisplay(out Ellipse e) => model.selectRect.GetAdjustedEllipse(out e);
                            break;
                        }
                    case Mode.SelectOutline: {
                            int x = model.cursorScreen.X + 2;
                            int y = model.cursorScreen.Y;
                            var f = model.brush.foreground;
                            var b = model.brush.background;
                            this.Print(x, y, new ColoredString($"{model.cursorScreen.X,4} {model.cursorScreen.Y,4}", f, b));
                            if (model.ticksSelect % 30 < 15) {
                                IEnumerable<Point> points = model.selectOutline.outline;
                                if(points.Any()) {
                                    var start = points.First();
                                    var end = points.Last();
                                    points = points.Union(model.selectOutline.GetLine(start, model.cursor));
                                    points = points.Union(model.selectOutline.GetLine(end, model.cursor));
                                    DrawSelectionWith(points);
                                } else {
                                    DrawSelection();
                                }

                            }
                            break;
                        }
                    case Mode.SelectWand: {
                            int x = model.cursorScreen.X + 2;
                            int y = model.cursorScreen.Y;
                            var f = model.brush.foreground;
                            var b = model.brush.background;
                            this.Print(x, y, new ColoredString($"{model.cursorScreen.X,4} {model.cursorScreen.Y,4}", f, b));
                            if (model.ticksSelect % 30 < 15) {
                                DrawSelection();
                            }
                            break;
                        }
                    case Mode.Move:
                        DrawSelection();
                        //Draw offset
                        //TO DO: Fix
                        if (model.ticksSelect % 10 < 5 && model.move.current.HasValue) {
                            var offset = model.move.end - model.move.start.Value;
                            int x = model.move.start.Value.X - camera.X;
                            int y = model.move.start.Value.Y - camera.Y;
                            bool first = true;
                            if(Math.Abs(offset.X) > Math.Abs(offset.Y)) {
                                DrawHorizontal();
                                DrawVertical();
                            } else if (Math.Abs(offset.X) < Math.Abs(offset.Y)) {
                                DrawVertical();
                                DrawHorizontal();
                            } else {
                                char glyph = Math.Sign(offset.X) == Math.Sign(offset.Y) ? '\\' : '/';
                                for (int i = 0; i < Math.Abs(offset.X) + 1; i++) {
                                    /*
                                    DrawBox(x, y, new BoxGlyph {
                                        n = Line.Single,
                                        e = Line.Single,
                                        s = Line.Single,
                                        w = Line.Single,
                                    });
                                    */
                                    this.SetCellAppearance(x, y, new ColoredGlyph(model.brush.foreground, model.brush.background, glyph));
                                    x += Math.Sign(offset.X);
                                    y += Math.Sign(offset.Y);
                                }
                            }

                            void DrawHorizontal() {
                                if(offset.X == 0) {
                                    return;
                                }
                                //Start
                                if(first) {
                                    DrawBox(x, y, new BoxGlyph {
                                        e = offset.X > 0 ? Line.Double : Line.None,
                                        w = offset.X < 0 ? Line.Double : Line.None,
                                        n = Line.Single,
                                        s = Line.Single
                                    });
                                }
                                //Body
                                for (int i = 1; i < Math.Abs(offset.X); i++) {
                                    x += Math.Sign(offset.X);
                                    DrawBox(x, y, new BoxGlyph {
                                        e = Line.Double,
                                        w = Line.Double
                                    });
                                }
                                x += Math.Sign(offset.X);
                                //End
                                if (first) {
                                    DrawBox(x, y, new BoxGlyph {
                                        e = offset.X < 0 ? Line.Double : offset.X == 0 ? Line.Single : Line.None,
                                        w = offset.X > 0 ? Line.Double : offset.X == 0 ? Line.Single : Line.None,
                                        n = offset.Y < 0 ? Line.Double : offset.Y == 0 ? Line.Single : Line.None,
                                        s = offset.Y > 0 ? Line.Double : offset.Y == 0 ? Line.Single : Line.None
                                    });
                                } else {
                                    DrawBox(x, y, new BoxGlyph {
                                        e = offset.X < 0 ? Line.Double : Line.None,
                                        w = offset.X > 0 ? Line.Double : Line.None,
                                        n = Line.Single,
                                        s = Line.Single
                                    });
                                }
                                
                                first = false;
                            }
                            void DrawVertical() {
                                if (offset.Y == 0) {
                                    return;
                                }
                                //Start
                                if (first) {
                                    DrawBox(x, y, new BoxGlyph {
                                        n = offset.Y < 0 ? Line.Double : Line.None,
                                        s = offset.Y > 0 ? Line.Double : Line.None,
                                        e = Line.Single,
                                        w = Line.Single
                                    });
                                }
                                //Body
                                for (int i = 1; i < Math.Abs(offset.Y); i++) {
                                    y += Math.Sign(offset.Y);
                                    DrawBox(x, y, new BoxGlyph {
                                        n = Line.Double,
                                        s = Line.Double
                                    });
                                }
                                y += Math.Sign(offset.Y);
                                //End
                                if (first) {
                                    DrawBox(x, y, new BoxGlyph {
                                        e = offset.X > 0 ? Line.Double : offset.X == 0 ? Line.Single : Line.None,
                                        w = offset.X < 0 ? Line.Double : offset.X == 0 ? Line.Single : Line.None,
                                        n = offset.Y > 0 ? Line.Double : offset.Y == 0 ? Line.Single : Line.None,
                                        s = offset.Y < 0 ? Line.Double : offset.Y == 0 ? Line.Single : Line.None
                                    });
                                } else {
                                    DrawBox(x, y, new BoxGlyph {
                                        n = offset.Y > 0 ? Line.Double : Line.None,
                                        s = offset.Y < 0 ? Line.Double : Line.None,
                                        e = Line.Single,
                                        w = Line.Single
                                    });
                                }
                                
                                first = false;
                            }
                        }
                        break;
                    case Mode.Pick:
                        if (model.ticksSelect % 30 < 15) {
                            DrawSelection();
                        }
                        if ((model.ticks % 30) < 15) {
                            if (IsMouseOver) {
                                this.SetCellAppearance(model.cursorScreen.X, model.cursorScreen.Y, new ColoredGlyph(model.brush.foreground, model.brush.background, '!'));
                            }
                        }
                        break;
                }
            }
            base.Render(timeElapsed);
            void DrawSelection() => DrawPoints(model.selection.GetAll());
            void DrawSelectionWith(IEnumerable<Point> points) => DrawPoints(model.selection.GetAll().Union(points));
            void DrawPoints(IEnumerable<Point> all) {
                foreach(var point in all) {
                    bool n = Contains(new Point(0, -1)), e = Contains(new Point(1, 0)), s = Contains(new Point(0, +1)), w = Contains(new Point(-1, 0)), ne = Contains(new Point(1, -1)), se = Contains(new Point(1, 1)), sw = Contains(new Point(-1, 1)), nw = Contains(new Point(-1, -1));
                    bool Contains(Point offset) => all.Contains(point + offset);
                    var p = point - camera;
                    if (!n && !e && !s && !w) {
                        //Isolated cell
                        this.AddDecorator(p.X, p.Y, 1, new CellDecorator(model.brush.foreground, '+', Mirror.None));
                        continue;
                    } else if (n && e && s && w && ne && nw && se && sw) {
                        //Surrounded cell
                        DrawBox(p.X, p.Y, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single, w = Line.Single });
                    } else {
                        BoxGlyph g = new BoxGlyph {
                            n = n && (!e && !w) ? Line.Double :                     //Thin Line Body
                                n && (!ne && !nw) ? Line.Double :                   //Intersection
                                n && (!e || !w || !ne || !nw) ? Line.Single :       //Edge Line Body
                                !n && !s && (w != e) ? Line.Single :                //Thin line End
                                Line.None,
                            e = e && (!n && !s) ? Line.Double :                     //Thin Line Body
                                e && (!ne && !se) ? Line.Double :                   //Intersection
                                e && (!n || !s || !se || !ne) ? Line.Single :       //Edge Line Body
                                !e && !w && (n != s) ? Line.Single :                //Thin line End
                                Line.None,
                            s = s && (!e && !w) ? Line.Double :                     //Thin Line Body
                                s && (!se && !sw) ? Line.Double :                   //Intersection
                                s && (!e || !w || !se || !sw) ? Line.Single :       //Edge Line Body
                                !s && !n && (w != e) ? Line.Single :                //Thin line End
                                Line.None,
                            w = w && (!n && !s) ? Line.Double :                     //Thin Line Body
                                w && (!nw && !sw) ? Line.Double :                   //Intersection
                                w && (!n || !s || !nw || !sw) ? Line.Single :       //Edge Line Body
                                !w && !e && (n != s) ? Line.Single :                //Thin line End
                                Line.None,
                        };
                        if(!BoxInfo.IBMCGA.glyphFromInfo.ContainsKey(g)) {
                            if(g.n == Line.Single && g.e == Line.Single) {
                                g = new BoxGlyph {
                                n = g.n,
                                e = g.e
                                };
                            } else if (g.e == Line.Single && g.s == Line.Single) {
                                g = new BoxGlyph {
                                    e = g.e,
                                    s = g.s
                                };
                            } else if (g.s == Line.Single && g.w == Line.Single) {
                                g = new BoxGlyph {
                                    s = g.s,
                                    w = g.w
                                };
                            } else if (g.w == Line.Single && g.n == Line.Single) {
                                g = new BoxGlyph {
                                    w = g.w,
                                    n = g.n
                                };
                            } else {
                                g = new BoxGlyph {
                                    n = Line.Single,
                                    e = Line.Single,
                                    s = Line.Single,
                                    w = Line.Single
                                };
                            }
                        }
                        DrawBox(p.X, p.Y, g);
                    }
                }
            }
            void DrawRect(Rectangle r) {
                var (left, top) = (r.X, r.Y);
                if (r.Width > 1 && r.Height > 1) {
                    DrawSidesX(Line.Single);
                    DrawSidesY(Line.Single);
                    DrawCornerNW();
                    DrawCornerNE();
                    DrawCornerSW();
                    DrawCornerSE();
                } else if (r.Width > 1) {
                    DrawSidesX(Line.Double);
                    DrawBox(left, top, new BoxGlyph { n = Line.Single, e = Line.Double, s = Line.Single });
                    DrawBox(left + r.Width - 1, top, new BoxGlyph { n = Line.Single, w = Line.Double, s = Line.Single });
                } else if (r.Height > 1) {
                    DrawSidesY(Line.Double);
                    DrawBox(left, top, new BoxGlyph { s = Line.Double, e = Line.Single, w = Line.Single });
                    DrawBox(left, top + r.Height - 1, new BoxGlyph { n = Line.Double, w = Line.Single, e = Line.Single });
                } else {
                    DrawBox(left, top, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single, w = Line.Single });
                }
                void DrawCornerNW() {
                    //Left top
                    DrawBox(left, top, new BoxGlyph { e = Line.Single, s = Line.Single });
                }
                void DrawCornerNE() {
                    //Right top
                    DrawBox(left + r.Width - 1, top, new BoxGlyph { w = Line.Single, s = Line.Single });
                }
                void DrawCornerSW() {
                    //Left bottom
                    DrawBox(left, top + r.Height - 1, new BoxGlyph { e = Line.Single, n = Line.Single });
                }
                void DrawCornerSE() {
                    //Right bottom
                    DrawBox(left + r.Width - 1, top + r.Height - 1, new BoxGlyph { n = Line.Single, w = Line.Single });
                }
                void DrawSidesX(Line style = Line.Single) {
                    foreach (var x in Enumerable.Range(left, r.Width)) {
                        DrawBox(x, top, new BoxGlyph { e = style, w = style });
                        DrawBox(x, top + r.Height - 1, new BoxGlyph { e = style, w = style });
                    }
                }
                void DrawSidesY(Line style = Line.Single) {
                    foreach (var y in Enumerable.Range(top, r.Height)) {
                        DrawBox(left, y, new BoxGlyph { n = style, s = style });
                        DrawBox(left + r.Width - 1, y, new BoxGlyph { n = style, s = style });
                    }
                }
            }
            void DrawBox(int x, int y, BoxGlyph g) {
                this.AddDecorator(x, y, 1, new CellDecorator(model.brush.foreground.SetAlpha(102), BoxInfo.IBMCGA.glyphFromInfo[g], Mirror.None));
            }
        }
        public override bool ProcessKeyboard(Keyboard info) {
            OnKeyboard?.Invoke(info);
            if (info.IsKeyPressed(Escape)) {
                if (model.mode != Mode.Read) {
                    model.ProcessKeyboard(info);
                } else {
                    var p = Game.Instance.Screen as Console;
                    Game.Instance.Screen = new FileMenu(p.Width, p.Height, new LoadMode());
                    Program.SaveState(null);
                }
            } else {
                if (info.IsKeyDown(LeftControl)) {
                    if (info.IsKeyPressed(OemPlus)) {
                        FontSize *= 2;
                    } else if (info.IsKeyPressed(OemMinus)) {
                        FontSize /= 2;
                    }
                    if (info.IsKeyPressed(S)) {
                        //File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.GetTempFileName())), JsonConvert.SerializeObject(model));
                        if (model.filepath == null || info.IsKeyDown(LeftShift)) {
                            var p = Game.Instance.Screen as Console;
                            Game.Instance.Screen = new FileMenu(p.Width, p.Height, new SaveMode(model, this));
                        } else {
                            model.Save();
                        }
                    }
                }
                model.ProcessKeyboard(info);
            }
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            IsFocused = true;
            var delta = state.Mouse.ScrollWheelValueChange;
            if (model.ctrl) {
                if(delta < 0) {
                    FontSize += new Point(1, 1);
                    (int w, int h) = Program.calculate(FontSize.X);
                    Resize(w, h, w, h, false);
                } else if(delta > 0) {
                    FontSize -= new Point(1, 1);
                    (int w, int h) = Program.calculate(FontSize.X);
                    Resize(w, h, w, h, false);
                }
            } else {
                if (model.shift) {
                    model.camera += new Point(delta, 0);
                } else {
                    model.camera += new Point(0, delta);
                }

                model.ProcessMouse(state, IsMouseOver);
            }
            
            return base.ProcessMouse(state);
        }
    }

    public enum ColorMode {
        RGB, Grayscale, Notepad
    }
    public enum Mode {
        Read, Brush, Fill, Pick, Erase, SelectRect, SelectCircle, SelectOutline, SelectWand, Move, Keyboard, Pan, Line,
    }
    [JsonObject(MemberSerialization.Fields)]
    public class SpriteModel {
        public ColorMode colorMode = ColorMode.RGB;

        public bool rendered = false;

        public bool infinite = true;
        public int width, height;

        public bool ctrl, shift, alt;

        public LinkedList<Edit> Undo;
        public LinkedList<Edit> Redo;

        [JsonIgnore]
        public Action historyChanged;
        [JsonIgnore]
        public Action brushChanged;

        public string filepath;
        public Sprite sprite;

        public int brushGlyph => tiles.brushIndex > -1 ? tiles.brushTile.Glyph : brush.cell.Glyph;
        public TileRef brushTile => colorMode switch {
            ColorMode.Notepad => new NotepadTile(brushGlyph),
            _ => (tiles.brushIndex > -1 ? (TileRef)tiles.brushTile : (TileRef)brush.cell)
        };

        public TileModel tiles;
        public PaletteModel palette;
        public BrushMode brush;
        public LineMode line;
        public FillMode fill;
        public PickMode pick;
        public EraseMode erase;
        public KeyboardMode keyboard;
        public MoveMode move;

        public Selection selection;
        public SelectOutlineMode selectOutline;
        public SelectRectMode selectRect;
        public SelectWandMode selectWand;
        public PanMode pan;

        public int ticks = 0;
        public int ticksSelect = 0;

        public Point camera = new Point();
        public Point cursor = new Point();       //Position on the image; stored as offset from camera
        public Point cursorScreen = new Point();
        public bool keyboardMode;

        public Point prevCell;
        public bool prevLeft;

        public Mode mode;

        public int currentLayerIndex = 0;
        public Layer currentLayer => sprite.layers[currentLayerIndex];

        public void RenderCursor(Console c) {
            (var x, var y) = cursorScreen;
            if(!c.IsValidCell(x, y)) {
                return;
            }
            var t = brush.cell.cg.SetBackground(c.GetBackground(x, y));
            c.SetCellAppearance(x, y, t);
        }
        public SpriteModel(int width, int height) {
            this.width = width;
            this.height = height;
            sprite = new Sprite();
            tiles = new TileModel();
            palette = new PaletteModel();
            brush = new BrushMode(this);
            line = new LineMode(this);
            fill = new FillMode(this);
            pick = new PickMode(this);
            erase = new EraseMode(this);
            keyboard = new KeyboardMode(this);
            selection = new Selection();
            selectOutline = new SelectOutlineMode(this, selection);
            selectRect = new SelectRectMode(this, selection);
            selectWand = new SelectWandMode(this, selection);
            pan = new PanMode(this);
            Undo = new LinkedList<Edit>();
            Redo = new LinkedList<Edit>();
            mode = Mode.Brush;

        }
        public void Save() {
            var fileName = Path.GetFullPath(Path.GetDirectoryName(filepath) + "/" + Path.GetFileNameWithoutExtension(filepath));
            File.WriteAllText($"{fileName}.asc", ASECIILoader.SerializeObject(this));
			File.WriteAllText($"{fileName}.dat", ASECIILoader.SerializeObject(sprite.exportData));
			File.WriteAllText($"{fileName}.dat_gd", ASECIILoader.SerializeObject(sprite.exportDataGd));
			var preview = sprite.preview;
			StringBuilder str = new StringBuilder();
            for (int y = sprite.origin.Y; y <= sprite.end.Y; y++) {
                for (int x = sprite.origin.X; x <= sprite.end.X; x++) {
                    if (preview.TryGetValue((x, y), out var tile)) {
                        str.Append((char)tile.Glyph);
                    } else {
                        str.Append(' ');
                    }
                }
                str.AppendLine();
            }
            //File.WriteAllText($"{fileName}.txt", str.ToString());
            Console c = null;
            if (infinite) {
                c = new Console(sprite.end.X - sprite.origin.X + 1, sprite.end.Y - sprite.origin.Y + 1);
                for (int y = sprite.origin.Y; y <= sprite.end.Y; y++) {
                    for (int x = sprite.origin.X; x <= sprite.end.X; x++) {
                        if (preview.TryGetValue((x, y), out var tile)) {
                            c.SetCellAppearance(x - sprite.origin.X, y - sprite.origin.Y, tile);
                        }
                    }
                }
            } else {
                c = new Console(width, height);
                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {
                        if (preview.TryGetValue((x, y), out var tile)) {
                            //c.SetCellAppearance(x - sprite.origin.X, y - sprite.origin.Y, tile);
                            c.SetCellAppearance(x, y, tile);
                        }
                    }
                }
            }
            c.Render(new TimeSpan());
            var t = (c.Renderer as ScreenSurfaceRenderer)._backingTexture;
            t.Texture.CopyToImage().SaveToFile($"{fileName}.png");

            AddAction(new SaveEdit());
        }
        public bool InBounds(Point p) => (p.X > -1 && p.X < width && p.Y > -1 && p.Y < height);
        public bool IsEditable(Point p) {
            bool result = infinite || InBounds(p);
            
            /*
            if(selectRect.rect.HasValue) {
                var r = selectRect.rect.Value;
                result = result && p.X >= r.X && p.X <= r.X + r.Width && p.Y >= r.Y && p.Y <= r.Y + r.Height;
            }
            */
            var selected = selection.GetAll();
            if (selected.Any()) {
                result = result && selected.Contains(p);
            }
            return result;
        }

        public Mode GetEffectiveMode() {
            if(pan.quickPan) {
                return Mode.Pan;
            } else if(pick.quickPick) {
                return Mode.Pick;
            } else {
                return mode;
            }
        }
        public void ProcessKeyboard(Keyboard info) {
            ctrl = info.IsKeyDown(LeftControl);
            shift = info.IsKeyDown(LeftShift);
            alt = info.IsKeyDown(LeftAlt);

            if (info.KeysDown.Any()) {
                int i = 0;
            }
            if (info.KeysPressed.Any()) {
                int i = 0;
            }

            if(info.IsKeyPressed(Z)) {
                if (ctrl) {
                    if (alt) {

                    } else {
                        if (shift) {
                            RedoLast();
                            return;
                        } else {
                            UndoLast();
                            return;
                        }
                    }
                }
            }

            if (mode == Mode.Keyboard) {
                if(info.IsKeyPressed(Escape)) {
                    mode = Mode.Brush;
                } else {
                    keyboard.ProcessKeyboard(info);
                }
            } else {
                if (info.IsKeyPressed(Escape)) {
                    switch (mode) {
                        case Mode.Read:
                            //Not supposed to happen since we exit
                            break;
                        case Mode.Brush:
                            SetMode(Mode.Read);
                            break;
                        default:
                            SetMode(Mode.Brush);
                            break;
                    }
                } else if (info.IsKeyPressed(Space)) {
                    if (!pan.quickPan) {
                        pan.quickPan = true;
                        pan.startPan = cursor;
                    }
                } else if (info.IsKeyReleased(Space)) {
                    pan.quickPan = false;
                    camera -= pan.offsetPan;
                    pan.startPan = new Point(0, 0);
                    pan.offsetPan = new Point(0, 0);
                } else if (info.IsKeyPressed(LeftAlt)) {
                    if (mode == Mode.Brush) {
                        pick.quickPick = true;
                    }
                } else if (info.IsKeyReleased(LeftAlt)) {
                    pick.quickPick = false;
                } else if (info.IsKeyPressed(Back)) {
                    AddAction(new FillEdit(sprite.layers[currentLayerIndex], selection.GetAll(), null));
                } else if (info.IsKeyPressed(B)) {
                    SetMode(Mode.Brush);
                } else if (info.IsKeyPressed(D)) {
                    selection.Clear();
                } else if (info.IsKeyPressed(E)) {
                    SetMode(Mode.Erase);
                } else if (info.IsKeyPressed(F)) {
                    SetMode(Mode.Fill);
                } else if (info.IsKeyPressed(I)) {
                    SetMode(Mode.Pick);
                } else if (info.IsKeyPressed(L)) {
                    SetMode(Mode.Line);
                } else if (info.IsKeyPressed(M)) {
                    SetMode(Mode.Move);
                    if (selection.Exists) {
                        var moveLayer = Cut(selection.GetAll());
                        move = new MoveMode(this, selection, moveLayer);
                    } else {
                        move = new MoveMode(this, selection, sprite.layers[currentLayerIndex]);
                    }
                } else if(info.IsKeyPressed(O)) {
                    SetMode(Mode.SelectOutline);
                } else if (info.IsKeyPressed(S) && !info.IsKeyDown(LeftControl)) {
                    SetMode(Mode.SelectRect);
                } else if (info.IsKeyPressed(T)) {
                    SetMode(Mode.Keyboard);
                    keyboard.keyCursor = null;
                    //keyboard.keyCursor = cursor;
                    //keyboard.margin = cursor;
                } else if (info.IsKeyPressed(W)) {
                    SetMode(Mode.SelectWand);
                }

                if (pan.quickPan) {
                    pan.ProcessKeyboard(info);
                }
            }
            void SetMode(Mode next) {
                if(mode == Mode.Move) {
                    //If we're moving an existing layer
                    move ??= new MoveMode(this, selection, sprite.layers[currentLayerIndex]);
                    if(sprite.layers.Contains(move.layer)) {
                    } else {
                        //We're moving a selection between layers
                        //Flatten selection onto layer
                        var layer = sprite.layers[currentLayerIndex];
                        layer.Flatten(move.layer);
                    }
                }

                mode = next;
            }
        }
        public Layer Cut(HashSet<Point> points) {
            Layer result = new Layer() { name = $"Layer {sprite.layers.Count}" };
            foreach(var point in points) {
                result[point] = sprite.layers[currentLayerIndex][point];
                sprite.layers[currentLayerIndex][point] = null;
            }
            return result;
        }
        public Layer SelectionToLayer() {
            var layer = Cut(selection.GetAll());
            if (currentLayerIndex == sprite.layers.Count - 1) {
                sprite.layers.Add(layer);
                currentLayerIndex++;
            } else {
                currentLayerIndex++;
                sprite.layers.Insert(currentLayerIndex, layer);
            };
            return layer;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool IsMouseOver) {
            cursorScreen = state.SurfaceCellPosition;
            cursor = cursorScreen + camera;
            
            switch (GetEffectiveMode()) {
                case Mode.Pan:
                    pan.ProcessMouse(state);
                    break;
                case Mode.Brush:
                    brush.ProcessMouse(state, IsMouseOver);
                    break;
                case Mode.Line:
                    line.ProcessMouse(state);
                    break;
                case Mode.Fill:
                    fill.ProcessMouse(state, IsMouseOver, shift, ctrl, alt);
                    break;
                case Mode.Pick:
                    pick.ProcessMouse(state, IsMouseOver);
                    break;
                case Mode.Erase:
                    erase.ProcessMouse(state, IsMouseOver);
                    break;
                case Mode.SelectOutline:
                    selectOutline ??= new SelectOutlineMode(this, selection);
                    selectOutline.ProcessMouse(state, ctrl, shift, alt);
                    break;
                case Mode.SelectRect:
                    selectRect.ProcessMouse(state, ctrl, shift, alt);
                    break;
                case Mode.SelectWand:
                    selectWand = new SelectWandMode(this, selection);
                    selectWand?.ProcessMouse(state, ctrl, shift, alt);
                    break;
                case Mode.Move:
                    move.ProcessMouse(state);
                    break;
                case Mode.Keyboard:
                    keyboard.ProcessMouse(state);
                    break;
            }


            prevCell = cursor;
            prevLeft = state.Mouse.LeftButtonDown;
        }

        public void OnLoad() {
            Save();
            line ??= new LineMode(this);
            fill ??= new FillMode(this);
        }
        public void RedoLast() {
            if (Redo.Any()) {
                var u = Redo.Last();
                Redo.RemoveLast();
                u.Do();
                Undo.AddLast(u);

                historyChanged?.Invoke();
            }
        }
        public void UndoLast() {
            if (Undo.Any()) {
                var u = Undo.Last();
                Undo.RemoveLast();
                u.Undo();
                Redo.AddLast(u);

                historyChanged?.Invoke();
            }
        }
        public void AddAction(Edit edit) {
            Undo.AddLast(edit);
            //Redo.Clear();
            edit.Do();

            historyChanged?.Invoke();
        }
    }
    public interface Edit {
        string Name { get; }
        void Undo();
        void Do();
    }

    public class SaveEdit : Edit {
        public string Name => "Save";
        public void Undo() { }
        public void Do() { }
    }
    public class SingleEdit : Edit {
        public string Name => "Brush";
        public Point cursor;
        public Layer layer;
        public TileRef prev;
        public TileRef next;
        public SingleEdit(Point cursor, Layer layer, TileRef next) {
            this.cursor = cursor;
            this.layer = layer;
            this.prev = layer[cursor];
            if(next != null && next.Foreground.A == 0 && next.Background.A == 0) {
                next = null;
            }
            this.next = next;
        }
        public void Undo() {
            layer[cursor] = prev;
        }
        public void Do() {
            layer[cursor] = next;
        }
        public bool IsRedundant() => prev == null ? next == null : next != null && (prev.Background == next.Background && prev.Foreground == next.Foreground && prev.Glyph == next.Glyph);
    }
    public class MultiEdit : Edit {
        public string Name => "Multi";
        public Layer layer;
        public Dictionary<(int, int), TileRef> prev;
        public Dictionary<(int, int), TileRef> next;
        public MultiEdit() {
            this.layer = null;
            this.prev = new Dictionary<(int, int), TileRef>();
            this.next = new Dictionary<(int, int), TileRef>();
        }
        public MultiEdit(Layer layer, Dictionary<(int, int), TileRef> next) {
            this.layer = layer;
            this.next = next;

            prev = new Dictionary<(int, int), TileRef>();
            foreach((var p, var t) in next) {
                prev[p] = layer[p];
            }
        }
        public MultiEdit(Layer layer) {
            this.layer = layer;
            this.next = new Dictionary<(int, int), TileRef>();
            this.prev = new Dictionary<(int, int), TileRef>();
        }
        public void Append(SingleEdit e) {
            if(!prev.ContainsKey(e.cursor)) {
                prev[e.cursor] = e.prev;
            }
            next[e.cursor] = e.next;
        }
        public void Undo() {
            foreach ((var p, var t) in prev) {
                layer[p] = t;
            }
        }
        public void Do() {
            foreach ((var p, var t) in next) {
                layer[p] = t;
            }
        }
    }
    public class FillEdit : Edit {
        public string Name => "Fill";
        public Layer layer;
        public Dictionary<(int, int), TileRef> prev;
        public TileRef next;

        public FillEdit(Layer layer, HashSet<Point> affected, TileRef next) {
            this.layer = layer;
            this.prev = new Dictionary<(int, int), TileRef>();
            foreach(var p in affected ?? new HashSet<Point>()) {
                this.prev[p] = layer[p];
            }

            if (next != null && next.Foreground.A == 0 && next.Background.A == 0) {
                next = null;
            }
            this.next = next;
        }
        public void Undo() {
            foreach((var point, var tile) in prev) {
                layer[point] = tile;
            }
        }
        public void Do() {
            foreach (var point in prev.Keys) {
                layer[point] = next;
            }
        }
    }

    public class MoveEdit {
        public string Name => "Move";

        Layer layer;
        Point offset;
        public MoveEdit(Layer layer, Point offset) {
            this.layer = layer;
            this.offset = offset;
        }

        public void Undo() => layer.pos -= offset;
        public void Do() => layer.pos += offset;
    }
    public class ExitMoveSelection : Edit {
        public string Name => "Exit Move";

        SpriteModel model;
        Flatten flatten;
        Mode next;
        public ExitMoveSelection(SpriteModel model, Layer selectionLayer, Mode next) {
            this.model = model;
            this.flatten = new Flatten(selectionLayer, model.sprite.layers[model.currentLayerIndex]);
            this.next = next;
        }
        public void Do() {
            this.flatten.Do();
            model.mode = next;
        }
        public void Undo() {
            model.mode = Mode.Move;
            model.move.layer = flatten.layer;
            flatten.Undo();
        }
    }
    //We don't consider this an individual edit since it's usually part of a larger action with more side effects
    //i.e. ExitMoveSelection and flatten layer button
    public class Flatten {

        public Layer layer;
        public Layer below;

        Dictionary<(int, int), TileRef> oldCells;
        public Flatten(Layer layer, Layer below) {
            this.layer = layer;
            this.below = below;

            oldCells = new Dictionary<(int, int), TileRef>();
            foreach (var p in layer.cells.Keys) {
                oldCells[p + layer.pos] = below[p + layer.pos];
            }
        }
        //To do: Test with both layers moved
        public void Undo() {
            foreach ((var p, var t) in oldCells) {
                below[p] = t;
            }
        }
        public void Do() {
            below.Flatten(layer);
        }
    }

    public class PanMode {
        public SpriteModel model;
        public bool quickPan;
        public Point startPan;     //Position on screen where user held down space and left clicked
        public Point offsetPan;

        public PanMode(SpriteModel model) {
            this.model = model;
        }
        public void ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Right)) {
                startPan += new Point(-1, 0);
            }
            if (info.IsKeyPressed(Left)) {
                startPan += new Point(1, 0);
            }
            if (info.IsKeyPressed(Up)) {
                startPan += new Point(0, 1);
            }
            if (info.IsKeyPressed(Down)) {
                startPan += new Point(0, -1);
            }
            UpdateOffset();
        }
        public void ProcessMouse(MouseScreenObjectState state) {
            UpdateOffset();
        }
        public void UpdateOffset() {
            offsetPan = model.cursor - (Point)startPan;
        }
    }
    [JsonObject(MemberSerialization.Fields)]
    public class GlyphScramble {
        public SpriteModel model;
        public BrushMode brush;

        public Random r;
        public Point mousePos;
        public GlyphScramble(SpriteModel model, BrushMode brush) {
            this.model = model;
            this.brush = brush;
            
            r = new Random();
            mousePos = Point.None;
        }

        public void Update() {
            if(brush.mouse.nowPos != mousePos) {
                int glyph = r.Next('A', 'Z' + 1);
                if (brush.glyph != glyph) {
                    brush.glyph = glyph;
                    model.brushChanged?.Invoke();
                }

                mousePos = brush.mouse.nowPos;
            }
        }

    }
    [JsonObject(MemberSerialization.Fields)]
    public class BrushMode {
        public SpriteModel model;
        [JsonIgnore]
        public MouseWatch mouse = new();
        public int glyph = 'A';
        public Color foreground = Color.White;
        public Color background = Color.Black;

        //Apply filter to Fill Mode
        public GlyphScramble filter;

        public MultiEdit placement;
        public TileValue cell {
            get => new TileValue(foreground, background, glyph); set {
                foreground = value.Foreground;
                background = value.Background;
                glyph = value.Glyph;
            }
        }

        public BrushMode () {
        }
        public BrushMode(SpriteModel model) {
            this.model = model;

            filter = new GlyphScramble(model, this);
        }

        public void ProcessMouse(MouseScreenObjectState state, bool IsMouseOver) {
            mouse ??= new MouseWatch();
            mouse.Update(state, IsMouseOver);
            //glyph = (char)((glyph + state.Mouse.ScrollWheelValueChange / 120 + 255) % 255);
            if(state.IsOnScreenObject) {

                //filter?.Update();

                if (state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                    var prev = model.prevCell;
                    var offset = (model.cursor - prev);
                    var length = offset.Length();
                    for (int i = 0; i < length; i++) {

                        var p = prev + new Point((int)(i * offset.X / length), (int)(i * offset.Y / length));
                        if (model.IsEditable(p)) {
                            Place(p);
                        }
                    }

                    if (model.IsEditable(model.cursor)) {
                        Place(model.cursor);
                    }
                }
            }

            if(mouse.left == ClickState.Released) {
                placement = null;
            }
        }
        void Place(Point p) {
            var layer = model.sprite.layers[model.currentLayerIndex];
            var e = new SingleEdit(p, layer, model.brushTile);

            if (e.IsRedundant()) {
                return;
            }
            //Store all of our placements in a compound action
            if (model.Undo.Any() && model.Undo.Last() == placement && placement.layer == layer) {
                placement.Append(e);
                e.Do();
            } else {
                placement = new MultiEdit(layer);
                placement.Append(e);
                model.AddAction(placement);
            }
        }
    }
    [JsonObject(MemberSerialization.Fields)]
    public class FillMode {
        public SpriteModel model;
		[JsonIgnore]
		public MouseWatch mouse = new MouseWatch();
        public FillMode(SpriteModel model) {
            this.model = model;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool IsMouseOver, bool shift, bool ctrl, bool alt) {
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject) {
                if (mouse.leftPressedOnScreen && mouse.left == ClickState.Released) {

                    var layer = model.sprite.layers[model.currentLayerIndex];
                    var start = model.cursor;
                    var t = layer[start];
                    var source = Layer.Triple(t);

                    HashSet<Point> affected = new HashSet<Point>();
                    if (ctrl) {
                        affected = layer.GetGrowFill(start, Layer.Triple(model.brushTile), model.sprite.origin, model.sprite.end);
                    } else if ((model.brush.foreground, model.brush.background, model.brush.glyph) != source) {
                        
                        if (shift) {
                            affected = layer.GetOutlineFill(start, source, model.sprite.origin, model.sprite.end);
                        } else if (alt) {
                            affected = layer.GetGlobalFill(source, model.sprite.origin, model.sprite.end);
                        } else {
                            affected = layer.GetFloodFill(start, source, model.sprite.origin, model.sprite.end);
                        }
                    }

                    if (model.selection.Exists) {
                        var s = model.selection.GetAll();
                        affected.IntersectWith(s);
                    }

                    if (affected.Any()) {
                        model.AddAction(new FillEdit(layer, affected, model.brushTile));
                    }
                }
            }
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    public class LineMode {
        public SpriteModel model;
        public Layer layer;

        public Point? start;
        public Point end;

        public HashSet<Point> GetPoints() {
            var start = this.start.Value;
            var offset = end - start;
            double distance = offset.Length();
            (double dx, double dy) = (offset.X / distance, offset.Y / distance);
            (double x, double y) = (start.X, start.Y);

            HashSet<Point> points = new HashSet<Point>();
            for (int i = 0; i < distance; i++) {
                points.Add(((int)Math.Round(x), (int)Math.Round(y)));
                (x, y) = (x + dx, y + dy);
            }
            return points;
        }
		//public Point moved => start.HasValue ? end - start.Value : new Point(0, 0);
		[JsonIgnore]
		public MouseWatch mouse = new();
        public LineMode(SpriteModel model) {
            this.model = model;
        }
        public void ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, state.IsOnScreenObject);
            if (mouse.left == ClickState.Pressed) {
                if (mouse.leftPressedOnScreen) {
                    //Start moving with the mouse
                    start = model.cursor;
                }
            } else if(mouse.left == ClickState.Held) {
                end = model.cursor;
            } else if (mouse.prevLeft) {
                end = model.cursor;
                if(start == end) {
                    model.AddAction(new SingleEdit(model.cursor, model.sprite.layers[model.currentLayerIndex], model.brushTile));
                } else if(start != null) {
                    Dictionary<(int, int), TileRef> affected = new Dictionary<(int, int), TileRef>();
                    
                    var cell = model.brushTile;

                    var points = GetPoints();
                    if (model.selection.Exists) {
                        points.IntersectWith(model.selection.GetAll());
                    }
                        
                    foreach (var p in points) affected[p] = cell;
                    model.AddAction(new MultiEdit(model.sprite.layers[model.currentLayerIndex], affected));
                }
                start = null;
            }
        }
    }


    [JsonObject(MemberSerialization.Fields)]
    public class PickMode {
        public bool quickPick;
        public SpriteModel model;
		[JsonIgnore]
		public MouseWatch mouse = new MouseWatch();
        public PickMode(SpriteModel model) {
            this.model = model;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool IsMouseOver) {
            mouse ??= new MouseWatch();
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject) {
                if (state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {

                    TileRef t = null;
                    var layer = model.sprite.layers[model.currentLayerIndex];
                    t = layer[model.cursor];

                    foreach(var l in model.sprite.layers) {
                        var lt = l[model.cursor];
                        if (lt != null) {
                            t = lt; 
                        }
                    }
                    if(t == null) {

                    } else if(t is TileIndex ti) {
                        model.tiles.brushIndex = ti.index;
                    } else if(t is TilePalette tp) {
                        model.palette.foregroundIndex = tp.foregroundIndex;
                        model.palette.backgroundIndex = tp.backgroundIndex;
                    }
                    model.brush.foreground = t?.Foreground ?? Color.Transparent;
                    model.brush.background = t?.Background ?? Color.Transparent;
                    model.brush.glyph = t?.Glyph ?? 0;

                    model.brushChanged?.Invoke();
                }
            }
        }
    }
    [JsonObject(MemberSerialization.Fields)]
    public class EraseMode {
        public MultiEdit placement;
        public SpriteModel model;
		[JsonIgnore]
		public MouseWatch mouse = new MouseWatch();
        public EraseMode(SpriteModel model) {
            this.model = model;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool IsMouseOver) {
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject) {
                if (state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                    var prev = model.prevCell;
                    var offset = (model.cursor - prev);
                    var length = offset.Length();
                    var (xNorm, yNorm) = (offset.X / length, offset.Y / length);
                    for (int i = 0; i < length; i++) {
                        var p = prev + new Point((int)(i * xNorm), (int)(i * yNorm));
                        if (model.IsEditable(p)) {
                            Place(p);
                        }
                    }
                    if (model.IsEditable(model.cursor)) {
                        Place(model.cursor);
                    }
                }
            }
        }
        void Place(Point p) {
            var layer = model.sprite.layers[model.currentLayerIndex];
            SingleEdit e = new SingleEdit(p, layer, null);
            if (e.IsRedundant()) {
                return;
            }
            //Store all of our placements in a compound action
            if (model.Undo.Any() && model.Undo.Last() == placement && placement.layer == layer) {
                placement.Append(e);
                e.Do();
            } else {
                placement = new MultiEdit(layer);
                placement.Append(e);
                model.AddAction(placement);
            }
        }
    }
    public class KeyboardMode {
        public SpriteModel model;
        public Point? keyCursor;
        public Point margin;
        public MultiEdit placement;
        public KeyboardMode(SpriteModel model) {
            this.model = model;
            keyCursor = new Point();
        }
        public void ProcessKeyboard(Keyboard info) {
            ref int ticks = ref model.ticks;
            if (info.IsKeyPressed(Right)) {
                keyCursor = (keyCursor ?? model.cursor) + new Point(1, 0);
                ticks = 0;
            }
            if (info.IsKeyPressed(Left)) {
                keyCursor = (keyCursor ?? model.cursor) + new Point(-1, 0);
                ticks = 0;
            }
            if (info.IsKeyPressed(Up)) {
                keyCursor = (keyCursor ?? model.cursor) + new Point(0, -1);
                ticks = 0;
            }
            if (info.IsKeyPressed(Down)) {
                keyCursor = (keyCursor ?? model.cursor) + new Point(0, 1);
                ticks = 0;
            }
            if(info.IsKeyPressed(Enter) && keyCursor.HasValue) {
                keyCursor = new Point(margin.X, keyCursor.Value.Y + 1);
                ticks = 0;
            }
            ref var sprite = ref model.sprite;
            ref var brush = ref model.brush;
            var pressed = info.KeysPressed.Where(k => k.Character != 0);
            if (info.KeysPressed.Select(p => p.Key).Intersect(new Keys[] { Keys.Up, Keys.Left, Keys.Right, Keys.Down }).Any()) {
                pressed = info.KeysDown.Where(k => k.Character != 0);
            }
            bool typewriter = true;
            if (pressed.Any()) {
                char c = pressed.First().Character;
                var p = keyCursor ?? model.cursor;
                if (model.IsEditable(p)) {
                    if (c != 0) {
                        var layer = model.currentLayer;
                        var tile = new TileValue(brush.foreground, brush.background, c);
                        var e = new SingleEdit(p, layer, tile);
                        Add(e);
                        ticks = typewriter ? 0 : 15;
                        keyCursor = keyCursor.Value + new Point(1, 0);
                    }
                }
            } else if (info.IsKeyPressed(Back)) {
                keyCursor = keyCursor.Value - new Point(1, 0);
                var layer = model.currentLayer;
                var p = keyCursor.Value;
                var e = new SingleEdit(p, layer, null);
                Add(e);
                ticks = typewriter ? 0 : 15;
            } else if (info.IsKeyDown(Back)) {
                var layer = model.currentLayer;
                var p = keyCursor ?? model.cursor;
                var e = new SingleEdit(p, layer, null);
                Add(e);
                ticks = typewriter ? 0 : 15;
            }
        }
        public void Add(SingleEdit e) {
            var layer = model.currentLayer;
            //Store all of our placements in a compound action
            if (model.Undo.Any() && model.Undo.Last() == placement && placement.layer == layer) {
                placement.Append(e);
                e.Do();
            } else {
                placement = new MultiEdit(layer);
                placement.Append(e);
                model.AddAction(placement);
            }
        }
        public void ProcessMouse(MouseScreenObjectState state) {
            if(state.IsOnScreenObject) {
                if (state.Mouse.LeftButtonDown) {
                    keyCursor = margin = model.cursor;
                }
            }
        }
    }
    public class MoveMode {
        public SpriteModel model;
        public Selection selection;
        public Layer layer;
        public Point? start;
        public Point? current;
        public Point end;
		//public Point moved => start.HasValue ? end - start.Value : new Point(0, 0);
		[JsonIgnore]
		public MouseWatch mouse = new();
        public MoveMode(SpriteModel model, Selection selection, Layer layer) {
            this.model = model;
            this.selection = selection;
            this.layer = layer;
        }
        public void ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, state.IsOnScreenObject);
            if (mouse.left == ClickState.Pressed) {
                if(mouse.leftPressedOnScreen) {
                    //Start moving with the mouse
                    current = model.cursor;
                    start = current;
                }
            } else if(mouse.prevLeft) {
                //Update the layer's apparent position
                end = model.cursor;
                if(end == current) {
                    return;
                }
                var offset = end - current.Value;
                layer.pos += offset;
                selection.Offset(offset);
                current = model.cursor;
            }
        }
    }

    public class Selection {
        public HashSet<Rectangle> rects;
        public HashSet<Point> points;
        public bool Exists => rects.Any() || points.Any();

        [JsonIgnore]
        public Action selectionChanged;
        public Selection() {
            rects = new HashSet<Rectangle>();
            points = new HashSet<Point>();
        }
        public HashSet<Point> GetAll() {
            HashSet<Point> result = new HashSet<Point>();
            foreach(var rect in rects) {
                result.UnionWith(rect.Positions());
            }
            result.UnionWith(points);
            return result;
        }
        public void UsePointsOnly() {
            points = GetAll();
            rects.Clear();
        }
        public void Offset(Point offset) {
            rects = new HashSet<Rectangle>(rects.Select(r => new Rectangle(r.X + offset.X, r.Y + offset.Y, r.Width, r.Height)));
            points = new HashSet<Point>(points.Select(p => p + offset));
        }
        public void Clear() {
            points.Clear();
            rects.Clear();
            selectionChanged?.Invoke();
        }

    }

    public class SelectOutlineMode {
        public SpriteModel model;
        public Selection selection;
        public HashSet<Point> outline;
        bool prevLeft;
        public SelectOutlineMode(SpriteModel model, Selection selection) {
            this.model = model;
            this.selection = selection;
            outline = new HashSet<Point>();
        }

        public void ProcessMouse(MouseScreenObjectState state, bool ctrl, bool shift, bool alt) {
            if (state.IsOnScreenObject) {
                if(alt) {
                    //Polygon selection mode
                    if (state.Mouse.LeftButtonDown) {
                        if (prevLeft) {
                            //Hold
                            UpdateOutline();
                        } else if (outline.Any()) {
                            //Click
                            if (model.cursor == outline.Last()) {
                                CloseLoop();
                            } else {
                                //Add line
                                UpdateOutline();
                            }
                        } else {
                            //Click
                            //Start a new polygon

                            ClearExistingSelection();
                            BeginOutline();
                        }
                    }
                    prevLeft = state.Mouse.LeftButtonDown;
                } else {
                    //Lasso selection mode
                    if (state.Mouse.LeftButtonDown) {
                        if (prevLeft) {
                            UpdateOutline();
                        } else {
                            ClearExistingSelection();
                            BeginOutline();
                        }
                    } else if (prevLeft) {
                        CloseLoop();
                    } else if(outline.Any()) {
                        //For instance, if we release Alt 
                        UpdateOutline();
                        CloseLoop();
                    }
                    prevLeft = state.Mouse.LeftButtonDown;
                }
                void UpdateOutline() {
                    //Add points to the outline
                    HashSet<Point> affected;
                    if (outline.Any()) {
                        affected = GetLine(outline.Last(), model.cursor);
                    } else {
                        affected = new HashSet<Point>() { model.cursor };
                    }
                    outline.UnionWith(affected);
                }

                void CloseLoop() {
                    //Close the loop
                    var l = GetLine(outline.First(), model.cursor);
                    outline.UnionWith(l);

                    var center = new Point(0, 0);
                    foreach (var p in outline) {
                        center += p;
                    }
                    center /= outline.Count;

                    var affected = model.sprite.layers[model.currentLayerIndex].GetBoundedFill(outline.Select(p => (p.X, p.Y)).ToHashSet());
                    if (shift) {
                        selection.UsePointsOnly();
                        selection.points.ExceptWith(outline);
                        selection.points.ExceptWith(affected);
                    } else {
                        selection.points.UnionWith(outline);
                        selection.points.UnionWith(affected);
                    }

                    outline.Clear();
                    selection.selectionChanged?.Invoke();
                }

                void BeginOutline() {
                    var start = model.cursor;
                    outline.Clear();
                    outline.Add(start);
                }
                void ClearExistingSelection() {
                    if (!ctrl && !shift) {
                        selection.Clear();
                    }
                }
            }
        }
        public HashSet<Point> GetLine(Point start, Point end) {
            var offset = end - start;
            double distance = offset.Length();
            (double dx, double dy) = (offset.X / distance, offset.Y / distance);
            (double x, double y) = (start.X, start.Y);

            HashSet<Point> points = new HashSet<Point>();
            for (int i = 0; i < distance; i++) {
                points.Add(((int)Math.Round(x), (int)Math.Round(y)));
                (x, y) = (x + dx, y + dy);
            }
            return points;
        }
    }
    //Add a menu to set and use pre-selected regions for editing
    public class SelectRectMode {
        public SpriteModel model;
        public Selection selection;
        public Point? start;
        public Point end;
        public bool alt;

        bool prevLeft;

        public SelectRectMode(SpriteModel model, Selection selection) {
            this.model = model;
            this.selection = selection;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool ctrl, bool shift, bool alt) {
            if(state.IsOnScreenObject) {
                if (state.Mouse.LeftButtonDown) {
                    this.alt = alt;
                    if (prevLeft) {
                        if (end != model.cursor) {
                            end = model.cursor;
                            model.ticksSelect = -15;
                        }
                    } else {
                        start = model.cursor;
                        end = model.cursor;

                        if(!ctrl && !shift) {
                            selection.Clear();
                        }
                    }
                } else if (prevLeft) {
                    if (GetRect(out var r)) {
                        if(shift) {
                            selection.UsePointsOnly();
                            selection.points.ExceptWith(r.Positions());
                        } else {
                            selection.rects.Add(r);
                        }

                        selection.selectionChanged?.Invoke();
                    }
                }
                prevLeft = state.Mouse.LeftButtonDown;
            }
            
        }

        private bool GetDimensions(out int left, out int top, out int width, out int height) {
            if (start.HasValue && start != this.end) {
                var end = this.end;
                if (alt) {
                    var offset = end - start.Value;

                    var size = Math.MinMagnitude(offset.X, offset.Y);
                    end = start.Value + new Point(Math.Sign(offset.X), Math.Sign(offset.Y)) * size;
                }

                left = Math.Min(start.Value.X, end.X);
                width = Math.Max(start.Value.X, end.X) - left + 1;
                top = Math.Min(start.Value.Y, end.Y);
                height = Math.Max(start.Value.Y, end.Y) - top + 1;
                return true;
            } else {
                left = 0;
                width = 0;
                top = 0;
                height = 0;
                return false;
            }
        }
        private bool GetEllipse(out Ellipse e) {
            if (GetDimensions(out int left, out int top, out int width, out int height)) {
                e = new Ellipse(left, top, width, height);
                return true;
            } else {
                e = Ellipse.Empty;
                return false;
            }
        }
        public bool GetAdjustedEllipse(out Ellipse e) {
            if (GetEllipse(out e)) {
                e = e.Translate(new Point() - model.camera);
                return true;
            } else {
                return false;
            }
        }
        private bool GetRect(out Rectangle r) {
            if(GetDimensions(out int left, out int top, out int width, out int height)) {
                r = new Rectangle(left, top, width, height);
                return true;
            } else {
                r = Rectangle.Empty;
                return false;
            }
        }
        public bool GetAdjustedRect(out Rectangle r) {
            if(GetRect(out r)) {
                r = r.Translate(new Point() - model.camera);
                return true;
            } else {
                return false;
            }
        }
    }
    public class SelectWandMode {
        public SpriteModel model;
        public Selection selection;
		[JsonIgnore]
		public MouseWatch mouse = new();
        bool prevLeft;

        public SelectWandMode(SpriteModel model, Selection selection) {
            this.model = model;
            this.selection = selection;
        }
        public void ProcessMouse(MouseScreenObjectState state, bool ctrl, bool shift, bool alt) {
            if (state.IsOnScreenObject) {
                if (state.Mouse.LeftButtonDown) {
                    var layer = model.sprite.layers[model.currentLayerIndex];

                    var start = model.cursor;
                    var t = layer[start];
                    var source = t != null ? (t.Foreground, t.Background, t.Glyph) : (Color.Transparent, Color.Transparent, 0);

                    HashSet<Point> affected;
                    if (alt) {
                        affected = layer.GetGlobalFill(source, model.sprite.origin, model.sprite.end);
                    } else {
                        affected = layer.GetFloodFill(start, source, model.sprite.origin, model.sprite.end);
                    }
                    if (affected.Any()) {
                        if(ctrl) {
                            selection.points.UnionWith(affected);
                        } else if(shift) {
                            selection.UsePointsOnly();
                            selection.points.ExceptWith(affected);
                        } else {
                            selection.Clear();
                            selection.points.UnionWith(affected);
                        }
                    }
                }
                prevLeft = state.Mouse.LeftButtonDown;
            }
        }
    }
}
