using System.Linq;
using System;
using LibGamer;
using System.Collections.Generic;
using Common;
namespace RogueFrontier;
public class WreckScene : IScene {
    Player player;

    ListControl<Item> playerPane, dockedPane;

    DescPane<Item> descPane;

    IScene prev;
    Sf sf;
	public Action<IScene> Go { set; get; }
	public Action<Sf> Draw { set; get; }
	public Action<SoundCtx> PlaySound { get; set; }
	private void SetDesc(Item i) {
        if (i == null) {
            descPane.Clear();
        } else {

            List<Tile[]> desc = [..i.type.desc.SplitLine(42).Select(line => Tile.Arr(line, ABGR.White, ABGR.Black))];


			descPane.SetEntry(i.name, desc);
        }
    }
    public WreckScene(SceneCtx ctx, Wreck docked) {
        this.prev = ctx.prev;
        this.sf = new Sf(ctx.Width * 4 / 3, ctx.Height, Fonts.FONT_6x8);
        this.player = ctx.playerShip.person;
        descPane = new DescPane<Item>(sf) { pos = (45, 2) };
        playerPane = new(sf, (2, 2), ctx.playerShip.name, ctx.playerShip.cargo, i => i.name, SetDesc) {
            active = false,
            invoke = i => {
                ctx.playerShip.cargo.Remove(i);
                docked.cargo.Add(i);
                dockedPane.UpdateIndex();
            },
        };
        dockedPane = new(sf, (81,2), docked.name, docked.cargo, i => i.name, SetDesc) {
            active = true,
            invoke = i => {
                ctx.playerShip.cargo.Add(i);
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
    ListControl<Item> currentPane => playerSide ? playerPane : dockedPane;
    void IScene.Update(TimeSpan delta) {
        playerPane.Update(delta);
        dockedPane.Update(delta);
        descPane.Update(delta);
    }
	public void HandleKey(KB kb) {
        if (kb[KC.Escape] == KS.Press) {
            Go(prev);
        } else {
            if(kb[KC.Left] == KS.Press) {
                playerSide = true;
            }
            if(kb[KC.Right] == KS.Press) {
                playerSide = false;
            }
            currentPane.HandleKey(kb);
        }
    }
    void IScene.HandleMouse(LibGamer.HandState mouse) {
        playerPane.HandleMouse(mouse);
        dockedPane.HandleMouse(mouse);
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        sf.RenderBackground();

        int y = 4;
        var f = ABGR.White;
        var b = ABGR.Black;
        sf.Print(4, y++, Tile.Arr($"Money: {$"{player.money}".PadLeft(8)}", f, b));

        dockedPane.Render(delta);
		playerPane.Render(delta);
		descPane.Render(delta);
		Draw?.Invoke(sf);
    }
}
