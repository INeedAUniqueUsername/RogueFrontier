using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public class GalaxyMap {
    public Action<Sf> Draw { set; get; }
    Universe univ;
    public XY camera=new();
    Hand mouse=new();
    private XY center;
    public Sf sf;
    int Width => sf.Width;
    int Height => sf.Height;

    public bool visible;
    public GalaxyMap(Mainframe prev) {
        univ = prev.world.universe;
        center = new XY(prev.Width, prev.Height) / 2;
        sf = new Sf(prev.Width, prev.Height, Fonts.FONT_8x8);
    }
    public void Render(TimeSpan drawTime) {
        if(!visible) {
            return;
        }
        sf.Clear();
        var tiles = univ.grid.Select(pair => (id: univ.systems[pair.Key], pos: pair.Value - camera + center))
            .Where(pair => true);
        foreach((var system, var p) in tiles) {
            (var x, var y) = p;
            sf.SetTile(x, Height - y, new Tile(ABGR.White, ABGR.Transparent, '*'));
        }
        Draw(sf);
    }
    public void HandleKey(KB kb) {

        foreach (var pressed in kb.Down) {
            var delta = 1 / 3f;
            switch (pressed) {
                case KC.Up:
                    camera += new XY(0, delta);
                    break;
                case KC.Down:
                    camera += new XY(0, -delta);
                    break;
                case KC.Right:
                    camera += new XY(delta, 0);
                    break;
                case KC.Left:
                    camera += new XY(-delta, 0);
                    break;
                case KC.Escape:
                    //IsVisible = false;
                    break;
            }
        }
    }
    public void HandleMouse(HandState state) {
        mouse.Update(state with { pos = (mouse.nowPos.x, Height - mouse.nowPos.y) });
        if (mouse.left == Pressing.Down) {
            camera += (XY)mouse.prevPos - (XY)mouse.nowPos;
        }
    }
}
