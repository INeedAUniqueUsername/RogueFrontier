using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using ArchConsole;
using System.Linq;

namespace ASECII {
    class LayerMenu : Console {
        SpriteModel model;
        public LayerMenu(int width, int height, SpriteModel model) : base(width, height) {
            this.model = model;
        }
        public void UpdateListing() {
            this.Children.Clear();
            var layers = model.sprite.layers;
            int i = 0;
            foreach(var l in layers) {
                int index = i;
                int x = 0;
                int y = layers.Count - i - 1;
                this.Children.Add(new ColorCellButton(() => l.visible ? Color.White : Color.Black,
                    () => {
                        l.visible = !l.visible;
                    }, '*') {
                    Position = new Point(x, y)
                });
                x++;
                this.Children.Add(new CellButton(() => index > 0,
                    () => {
                        if (model.currentLayerIndex == index) {
                            model.currentLayerIndex--;
                        }

                        layers.RemoveAt(index);
                        layers.Insert(index - 1, l);
                        UpdateListing();
                    }, (char)('9' - 32)) {
                    Position = new Point(x, y)
                });
                x++;
                this.Children.Add(new CellButton(() => index < layers.Count - 1,
                    () => {
                        layers.RemoveAt(index);
                        layers.Insert(index + 1, l);

                        if (model.currentLayerIndex == index) {
                            model.currentLayerIndex++;
                        }

                        UpdateListing();
                    }, (char)('8' - 32)) {
                    Position = new Point(x, y)
                });

                ColorButton nameButton = null;

                string GetLabel() => $">{(l.name.Length > 8 ? l.name.Remove(8) : l.name)}";

                var layerSettings = new LayerSettings(l, () => nameButton.text = GetLabel()){
                    Position = new Point(16, y)
                };

                this.Children.Add(new ColorCellButton(() => !this.Children.Contains(layerSettings) ? Color.White : Color.Black,
                    () => {
                        //Show the LayerSettings console
                        if (this.Children.Contains(layerSettings)) {
                            this.Children.Remove(layerSettings);
                        } else {
                            this.Children.Add(layerSettings);
                        }
                    }, '?') {
                    Position = new Point(3, y)
                });

                nameButton = new ColorButton(GetLabel(),
                    () => model.currentLayerIndex == index ? Color.Yellow : Color.White,
                    () => model.currentLayerIndex = index
                    ) { Position = new Point(4, y) };
                this.Children.Add(nameButton);

                this.Children.Add(new CellButton(() => index > 0,
                    () => {
                        //Change to use Edits
                        var below = layers[index - 1];
                        below.Flatten(l);
                        layers.RemoveAt(index);
                        if (model.currentLayerIndex == index) {
                            model.currentLayerIndex--;
                        }
                        UpdateListing();
                    }, (char)('6' - 32)) {
                    Position = new Point(13, y)
                });
                this.Children.Add(new CellButton(() => true,
                    () => {
                        layers.Insert(index, new(model.currentLayer));
                        UpdateListing();
                    }, '=') {
                    Position = new Point(14, y)
                });
                this.Children.Add(new CellButton(() => model.sprite.layers.Count > 1,
                        () => {
                            layers.RemoveAt(index);
                            if (model.currentLayerIndex == index && index > 0) {
                                model.currentLayerIndex--;
                            }
                            UpdateListing();
                        }, 'X') {
                    Position = new Point(15, y)
                });
                i++;
            }
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }

    class LayerSettings : Console {
        public LayerSettings(Layer layer, Action nameChanged = null) : base(16, 1) {
            int x = 0;
            var nameField = new TextField(12) { text = layer.name };
            nameField.TextChanged += t => UpdateName();
            nameField.EnterPressed += t => UpdateName();

            void UpdateName() {
                layer.name = nameField.text;
                nameChanged?.Invoke();
            }
            x += 12;
            var glyphButton = new ColorCellButton(() => layer.applyGlyph ? Color.White : Color.Black,
                () => {
                    layer.applyGlyph = !layer.applyGlyph;
                }, 'G') {
                Position = new Point(x, 0)
            };
            x++;

            var foregroundButton = new ColorCellButton(() => layer.applyForeground ? Color.White : Color.Black,
                () => {
                    layer.applyForeground = !layer.applyForeground;
                }, 'F') {
                Position = new Point(x, 0)
            };
            x++;

            var backgroundButton = new ColorCellButton(() => layer.applyBackground ? Color.White : Color.Black,
                () => {
                    layer.applyBackground = !layer.applyBackground;
                }, 'B') {
                Position = new Point(x, 0)
            };
            x++;

            Action<Console> add = this.Children.Add;
            add(nameField);
            add(glyphButton);
            add(foregroundButton);
            add(backgroundButton);
        }
    }
}
