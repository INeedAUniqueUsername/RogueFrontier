using SadRogue.Primitives;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchConsole;

namespace ASECII {
    public class PaletteMenu : SadConsole.Console {
        SpriteModel spriteModel;
        MouseWatch mouse;
        PaletteModel paletteModel;
        public event Action brushChanged;

        public PaletteMenu(int width, int height, SpriteModel spriteModel, PaletteModel paletteModel, Action brushChanged) : base(width, height) {
            this.spriteModel = spriteModel;
            this.mouse = new MouseWatch();
            this.paletteModel = paletteModel;
            this.brushChanged = brushChanged;
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if(state.IsOnScreenObject) {
                int index = (state.SurfaceCellPosition.X) + (state.SurfaceCellPosition.Y * Width);
                if (index > -1 && index < paletteModel.palette.Count) {
                    var left = mouse.leftPressedOnScreen && mouse.nowLeft;
                    var right = mouse.rightPressedOnScreen && mouse.nowRight;
                    if (left || right) {
                        if (left) {
                            if (paletteModel.foregroundIndex != index) {
                                //Don't want the color to flash between index and transparent
                                if(mouse.left == ClickState.Pressed || (mouse.left == ClickState.Held && paletteModel.foregroundIndex != null)) {
                                    paletteModel.foregroundIndex = index;
                                    spriteModel.brush.foreground = paletteModel.palette[index];
                                }

                            } else if(mouse.left == ClickState.Pressed) {
                                //Press the same color to deselect
                                paletteModel.foregroundIndex = null;
                                spriteModel.brush.foreground = Color.Transparent;
                            }
                        }
                        if (right) {
                            if (paletteModel.backgroundIndex != index) {
                                //Don't want the color to flash between index and transparent
                                if (mouse.right == ClickState.Pressed || (mouse.right == ClickState.Held && paletteModel.backgroundIndex != null)) {
                                    paletteModel.backgroundIndex = index;
                                    spriteModel.brush.background = paletteModel.palette[index];
                                }

                            } else if (mouse.right == ClickState.Pressed) {
                                //Press the same color to deselect
                                paletteModel.backgroundIndex = null;
                                spriteModel.brush.background = Color.Transparent;
                            }
                        }

                        brushChanged?.Invoke();
                    }
                    
                }
            }

            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {
            this.Clear();
            for(int i = 0; i < paletteModel.palette.Count; i++) {
                this.SetCellAppearance(i % Width, i / Width, new ColoredGlyph(Color.Transparent, paletteModel.palette[i]));
            }
            if(paletteModel.foregroundIndex != null || paletteModel.backgroundIndex != null) {
                if(paletteModel.foregroundIndex == paletteModel.backgroundIndex) {
                    var i = paletteModel.foregroundIndex.Value;
                    int x = i % Width;
                    int y = i / Width;

                    this.SetCellAppearance(x, y, new ColoredGlyph(paletteModel.palette[i].GetTextColor(), paletteModel.palette[i], 'X'));
                } else {
                    if(paletteModel.foregroundIndex != null) {
                        var i = paletteModel.foregroundIndex.Value;
                        int x = i % Width;
                        int y = i / Width;
                        this.SetCellAppearance(x, y, new ColoredGlyph(paletteModel.palette[i].GetTextColor(), paletteModel.palette[i], 'F'));
                    }
                    if(paletteModel.backgroundIndex != null) {
                        var i = paletteModel.backgroundIndex.Value;
                        int x = i % Width;
                        int y = i / Width;

                        this.SetCellAppearance(x, y, new ColoredGlyph(paletteModel.palette[i].GetLuma() > 0.5 ? Color.Black : Color.White, paletteModel.palette[i], 'B'));
                    }
                }
            }

            base.Render(timeElapsed);
        }
    }
    public class PaletteModel {
        public List<Color> palette = new List<Color>();
        public HashSet<Color> paletteSet = new HashSet<Color>();
        public int? foregroundIndex;
        public int? backgroundIndex;

        public void AddColor(Color c) {
            palette.Add(c);
            paletteSet.Add(c);
        }
        public void RemoveColor(Color c) {
            palette.Remove(c);
            paletteSet.Remove(c);
        }
        public void UpdateIndexes(SpriteModel spriteModel) {
            var p = palette;
            foregroundIndex = p.IndexOf(spriteModel.brush.foreground);
            if (foregroundIndex == -1) {
                foregroundIndex = null;
            }

            backgroundIndex = p.IndexOf(spriteModel.brush.background);
            if (backgroundIndex == -1) {
                backgroundIndex = null;
            }
        }
    }
}
