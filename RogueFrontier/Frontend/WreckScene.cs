using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using Console = SadConsole.Console;
using Common;
namespace RogueFrontier;
public class WreckScene : Console {
    Player player;
    ScreenSurface prev;

    ListPane<Item> playerPane, dockedPane;

    DescPanel<Item> descPane;

    private void SetDesc(Item i) {
        if (i == null) {
            descPane.SetInfo("", new());
        } else {
            descPane.SetInfo(i.name, i.type.desc.SplitLine(descPane.Surface.Width).Select(line => new ColoredString(line, Color.White, Color.Black)).ToList());
        }
    }
    public WreckScene(ScreenSurface prev, PlayerShip playerShip, Wreck docked) : base(prev.Surface.Width, prev.Surface.Height) {
        this.player = playerShip.person;
        this.prev = prev;

        descPane = new DescPanel<Item>(40, 26) { Position = new(24, 4) };
        
        playerPane = new(playerShip.name, playerShip.cargo, i => i.name, SetDesc) {
            Position = new(4, 16),
            active = false,
            invoke = i => {
                playerShip.cargo.Remove(i);
                docked.cargo.Add(i);
                dockedPane.UpdateIndex();
            },
        };
        dockedPane = new(docked.name, docked.cargo, i => i.name, SetDesc) {
            Position = new(47, 16),
            active = true,
            invoke = i => {
                playerShip.cargo.Add(i);
                docked.cargo.Remove(i);
                playerPane.UpdateIndex();
            },
        };
        Children.Add(descPane);
        Children.Add(playerPane);
        Children.Add(dockedPane);
    }
    public void Exit() {
        Parent.Children.Remove(this);
        prev.IsFocused = true;
    }

    bool playerSide {
        set {
            dockedPane.active = !(playerPane.active = value);
            SetDesc(currentPane.currentItem);
        }
        get => playerPane.active;
    }
    ListPane<Item> currentPane => playerSide ? playerPane : dockedPane;
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Escape)) {
            Exit();
        } else {
            if (keyboard.IsKeyPressed(Keys.Left)) {
                playerSide = true;
            }
            if (keyboard.IsKeyPressed(Keys.Right)) {
                playerSide = false;
            }
            currentPane.ProcessKeyboard(keyboard);
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        return base.ProcessMouse(state);
    }
    public override void Update(TimeSpan delta) {
        
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        Surface.Clear();
        this.RenderBackground();
        int y = 4;
        var f = Color.White;
        var b = Color.Black;
        this.Print(4, y++, $"Money: {$"{player.money}".PadLeft(8)}", f, b);
        
        base.Render(delta);
    }
}
