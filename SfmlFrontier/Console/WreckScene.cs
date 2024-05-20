using LibGamer;
namespace RogueFrontier;
public class WreckScene : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Player player;
    IScene prev;
    Sf sf;

	ListPane<Item> playerPane, dockedPane;

    DescPanel<Item> descPane;

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
        this.player = playerShip.person;
        this.prev = prev;
        sf = new Sf(Program.WIDTH, Program.HEIGHT);

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
    public void Exit() {
        prev.Show();
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
        if (kb[KC.Escape] == KS.Press) {
            Exit();
        } else {
            if(kb[KC.Left] == KS.Press) {
                playerSide = true;
            }
            if(kb[KC.Right] == KS.Press) {
                playerSide = false;
            }
            currentPane.ProcessKeyboard(kb);
        }
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        sf.RenderBackground();
        int y = 4;
        var f = ABGR.White;
        var b = ABGR.Black;
        sf.Print(4, y++, Tile.Arr($"Money: {$"{player.money}".PadLeft(8)}", f, b));
        Draw(sf);
    }
}
