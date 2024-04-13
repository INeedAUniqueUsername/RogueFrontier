using SadConsole.Input;
using System;
using System.Collections.Generic;
using SadRogue.Primitives;
using Common;
using SadConsole;
using Console = SadConsole.Console;
using System.Linq;
using static SadConsole.Input.Keys;
using ArchConsole;
using static RogueFrontier.PlayerShip;
using System.Text.Json.Serialization;

namespace RogueFrontier;

public class GalaxyMap : Console {
    Universe univ;
    public XY camera=new();
    MouseWatch mouse=new();
    private XY center;
    public GalaxyMap(Mainframe prev) : base(prev.Width, prev.Height) {
        univ = prev.world.universe;
        center = new XY(prev.Width, prev.Height) / 2;
    }
    public override void Update(TimeSpan timeSpan) {
        base.Update(timeSpan);
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        var visible = univ.grid.Select(pair => (id: univ.systems[pair.Key], pos: pair.Value - camera + center))
            .Where(pair => true);
        foreach((var system, var p) in visible) {
            (var x, var y) = p;
            this.SetCellAppearance(x, Height - y, new ColoredGlyph(Color.White, Color.Transparent, '*'));

        }
        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {

        foreach (var pressed in info.KeysDown) {
            var delta = 1 / 3f;
            switch (pressed.Key) {
                case Keys.Up:
                    camera += new XY(0, delta);
                    break;
                case Keys.Down:
                    camera += new XY(0, -delta);
                    break;
                case Keys.Right:
                    camera += new XY(delta, 0);
                    break;
                case Keys.Left:
                    camera += new XY(-delta, 0);
                    break;
                case Escape:
                    IsVisible = false;
                    break;
            }
        }
        Done:
        return base.ProcessKeyboard(info);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        mouse.Update(state, IsMouseOver);
        mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
        if (mouse.left == ClickState.Held) {
            camera += new XY(mouse.prevPos - mouse.nowPos);
        }

        return base.ProcessMouse(state);
    }
}
