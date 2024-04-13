using SadConsole;
using System.Collections.Generic;
using SadRogue.Primitives;
using Console = SadConsole.ScreenSurface;
using System.Linq;

namespace Common;

public class ConsoleComposite {
    List<Console> consoles;
    public ColoredGlyph this[int x, int y] {
        get {

            List<CellDecorator> d = new List<CellDecorator>();
            Color f = Color.Transparent;
            Color b = Color.Transparent;
            int g = 0;
            foreach (var c in consoles) {
                var cg = c.Surface.GetCellAppearance(x, y);
                if (cg.Glyph != 0 && cg.Glyph != ' ' && cg.Foreground.A != 0) {
                    if (g != 0 && g != ' ' && f.A != 0) {
                        d.Add(new CellDecorator(f, g, Mirror.None));
                    }
                    f = cg.Foreground;
                    g = cg.Glyph;
                }
                b = b.Premultiply().Blend(cg.Background);
            }
            if (d.Any()) {
                int i = 0;
            }
            return new ColoredGlyph(f, b, g) { Decorators = d.ToList() };
        }
    }
    public ConsoleComposite(params Console[] consoles) => this.consoles = new List<Console>(consoles);
    public ConsoleComposite(IEnumerable<Console> consoles) => this.consoles = new List<Console>(consoles);
}
