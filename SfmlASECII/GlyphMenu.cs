using ArchConsole;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SadRogue.Primitives;

namespace ASECII {
    class GlyphMenu : SadConsole.Console {
        SpriteModel spriteModel;
        MouseWatch mouse;
        Action brushChanged;

        public GlyphMenu(int width, int height, SpriteModel spriteModel, Action brushChanged) : base(width, height) {
            this.spriteModel = spriteModel;
            this.mouse = new MouseWatch();
            this.brushChanged = brushChanged;
        }

        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            if (state.IsOnScreenObject && state.Mouse.LeftButtonDown && mouse.leftPressedOnScreen) {
                int index = (state.SurfaceCellPosition.X) + (state.SurfaceCellPosition.Y * Width);
                if (index < Font.TotalGlyphs && spriteModel.brush.glyph != index) {
                    spriteModel.brush.glyph = (char)index;
                    brushChanged?.Invoke();
                }
            }

            return base.ProcessMouse(state);
        }
        public override void Render(TimeSpan timeElapsed) {

            Color f, b;
            switch(spriteModel.colorMode) {
                case ColorMode.RGB:
                    f = spriteModel.brush.foreground;
                    b = spriteModel.brush.background;
                    break;
                case ColorMode.Grayscale:
                    f = spriteModel.brush.foreground.ToGray();
                    b = spriteModel.brush.background.ToGray();
                    break;
                case ColorMode.Notepad:
                default:
                    f = Color.Black;
                    b = Color.White;
                    break;
            }


            var brushX = spriteModel.brush.glyph % Width;
            var brushY = spriteModel.brush.glyph / Width;

            for (int i = 0; i < Font.TotalGlyphs; i++) {
                string s = ((char)i).ToString();

                int x = i % Width;
                int y = i / Width;
                var cs = ((x == brushX) == (y == brushY)) ? new ColoredString(s, f, b) : new ColoredString(s, b, f);
                this.Print(x, y, cs);
            }
            /*
            var c = this.GetCellAppearance(brushX, brushY);
            this.SetCellAppearance(brushX, brushY, new ColoredGlyph(c.Background, c.Foreground, c.Glyph));
            */

            base.Render(timeElapsed);
        }
    }
}
