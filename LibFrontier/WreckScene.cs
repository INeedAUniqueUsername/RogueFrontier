using System.Linq;
using System;
using LibGamer;
namespace RogueFrontier;
public class WreckScene : IScene {
    Player player;

    ListPane<Item> playerPane, dockedPane;

    DescPanel<Item> descPane;

    IScene prev;
    public Action<IScene> Go { set; get; }
    private void SetDesc(Item i) {
        if (i == null) {
            descPane.SetInfo("", []);
        } else {
            descPane.SetInfo(i.name, [
                [..i.type.desc.Select(line => new Tile(line, ABGR.White, ABGR.Black))]
                ]);
        }
    }
    public WreckScene(IScene prev, PlayerShip playerShip, Wreck docked) {
        this.prev = prev;
        this.player = playerShip.person;

        descPane = new DescPanel<Item>();
        
        playerPane = new(playerShip.name, playerShip.cargo, i => i.name, SetDesc) {
            active = false,
            invoke = i => {
                playerShip.cargo.Remove(i);
                docked.cargo.Add(i);
                dockedPane.UpdateIndex();
            },
        };
        dockedPane = new(docked.name, docked.cargo, i => i.name, SetDesc) {
            active = true,
            invoke = i => {
                playerShip.cargo.Add(i);
                docked.cargo.Remove(i);
                playerPane.UpdateIndex();
            },
        };
    }

    bool playerSide {
        set {
            dockedPane.active = !(playerPane.active = value);
            SetDesc(currentPane.currentItem);
        }
        get => playerPane.active;
    }
    ListPane<Item> currentPane => playerSide ? playerPane : dockedPane;

	public void ProcessKeyboard(KB kb) {
        if (kb[KC.Escape] == KS.Pressed) {
            prev.Show();
        } else {
            if(kb[KC.Left] == KS.Pressed) {
                playerSide = true;
            }
            if(kb[KC.Right] == KS.Pressed) {
                playerSide = false;
            }
            currentPane.ProcessKeyboard(kb);
        }
    }
    public void Render(ISurf Surface, TimeSpan delta) {
        Surface.Clear();
        Surface.RenderBackground();
        int y = 4;
        var f = ABGR.White;
        var b = ABGR.Black;
        Surface.Print(4, y++, Tile.Arr($"Money: {$"{player.money}".PadLeft(8)}", f, b));
    }
}
