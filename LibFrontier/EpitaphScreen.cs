using LibGamer;
using System;
using System.Collections.Generic;

namespace RogueFrontier;

public class EpitaphScreen : IScene {
	public Action<IScene> Go { set; get; }
	public Action<Sf> Draw { set; get; }
	public Action<SoundCtx> PlaySound { get; set; }
	Mainframe playerMain;
    Epitaph epitaph;
    Sf sf_ui;
    Sf sf_img;
    public HashSet<SfControl> controls = [];
	public EpitaphScreen(Mainframe playerMain, Epitaph epitaph) {
        this.playerMain = playerMain;
        this.epitaph = epitaph;
        this.sf_ui = new Sf(playerMain.sf.GridWidth, playerMain.sf.GridHeight, Fonts.FONT_6x8);
		controls.Add(new SfLink(sf_ui, (1, sf_ui.GridHeight * 3 / 4 - 4), "Resurrect", Resurrect));
		controls.Add(new SfLink(sf_ui, (1, sf_ui.GridHeight * 3 / 4 - 2), "TitleScreen", Exit));

		var size = epitaph.deathFrame.GetLength(0);
        sf_img = new Sf(size, size, Fonts.FONT_8x8) { pos = (playerMain.sf.GridWidth - size, 0) };
	}
    public void Resurrect() {
        var playerShip = playerMain.playerShip;
        var world = playerShip.world;
        //Restore mortality chances
        playerShip.mortalChances = 3;
        //To do: Restore player HP
        playerShip.ship.damageSystem.Restore();
        playerShip.powers.ForEach(p => p.cooldownLeft = 0);
        playerShip.devices.Reactor.ForEach(r => r.energy = r.desc.capacity);
        //Resurrect the player; remove wreck and restore ship + heading
        var wreck = epitaph.wreck;
        if (wreck != null) {
            wreck.Destroy(null);
            world.RemoveEntity(wreck);
            world.entities.all.Remove(wreck);
        }
        playerShip.ship.active = true;
        playerShip.AddMessage(new Message("A vision of disaster flashes before your eyes"));
        world.AddEntity(playerShip);
        world.AddEffect(new Heading(playerShip));

        playerMain.silenceSystem.AddEntity(playerShip);

        PausePrev p = null;
        p = new PausePrev(playerMain, Resume, 2);
		Go(new FadeIn(p, playerMain.sf));
        void Resume() {
            p.Go(playerMain);
            playerMain.ShowUI();
        }
    }
    public void Exit() {
        var profile = playerMain.profile;
        /*
        if (profile != null) {
            var unlocked = SAchievements.GetAchievements(profile, playerMain.playerShip)
                .Except(profile.achievements);
            if (unlocked.Any()) {
                Console c = new Console(Width, Height) { FontSize = playerMain.FontSize, IsFocused = true };

                int x = 1;
                int y = 1;
                var fs = playerMain.FontSize * 2;
                c.Children.Add(new Label("Achievement Unlocked") {
                    Position = new Point(x, y++),
                    FontSize = fs
                });
                y++;
                foreach (var a in unlocked) {
                    c.Children.Add(new Label(SAchievements.names[a]) {
                        Position = new Point(x, y++),
                        FontSize = fs
                    });
                }

                c.Children.Add(new LabelButton("Continue", TitleScreen) {
                    Position = new Point(x, c.Height / 2 - 2),
                    FontSize = fs
                });

                SadConsole.Game.Instance.Screen = c;
                profile.achievements.UnionWith(unlocked);
                profile.Save();
                return;
            }

        }
        */
        TitleScreen();
        void TitleScreen() {
            var ts = new TitleScreen(sf_ui.GridWidth, sf_ui.GridHeight, new System(playerMain.world.universe));
            Go(new TitleSlideOpening(ts, ts.sf));
        }
    }
    public void Render(TimeSpan delta) {
        sf_ui.Clear();
        var str = playerMain.playerShip.GetMemorial(epitaph.desc);
        int y = 2;
        foreach (var line in str.Replace("\r", "").Split('\n')) {
            sf_ui.Print(2, y++, Tile.Arr(line));
		}
		foreach(var c in controls) c.Render(delta);
		Draw(sf_ui);
		if (epitaph.deathFrame != null) {
            var size = epitaph.deathFrame.GetLength(0);
            for (y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    sf_img.Tile[x, y] = epitaph.deathFrame[x, y];
                }
            }
        }
        Draw(sf_img);

    }
    void IScene.HandleMouse(LibGamer.HandState mouse) {
        foreach(var c in controls) c.HandleMouse(mouse);
    }
}
public class IntermissionScreen : IScene {
    Mainframe playerMain;
    LiveGame game;
    string desc;

    public Action<IScene> Go { set; get; }
    public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Sf Surface;

	public IntermissionScreen(Mainframe playerMain, LiveGame game, string desc) {
        this.Surface = new Sf(playerMain.sf.GridWidth, playerMain.sf.GridHeight, Fonts.FONT_8x8);
        this.playerMain = playerMain;
        this.game = game;
        this.desc = desc;
#if false
        Children.Add(new LabelButton("Save & Continue", Continue) {
            Position = new Point(1, Surface.Height / 2 - 4), FontSize = playerMain.FontSize * 2
        });
        Children.Add(new LabelButton("Save & Quit", Exit) {
            Position = new Point(1, Surface.Height / 2 - 2), FontSize = playerMain.FontSize * 2
        });
#endif
    }
    public void Continue() {
        game.Save();
        //game.OnLoad(playerMain);

        Go(new TitleSlideOpening(new PausePrev(playerMain, Resume, 4), playerMain.sf));
        void Resume() {
            Go(playerMain);
            playerMain.ShowUI();
        }
    }
    public void Exit() {
        game.Save();
        var ts = new TitleScreen(Surface.GridWidth, Surface.GridHeight, new System(playerMain.world.universe));
		Go(new TitleSlideOpening(ts, ts.sf));
    }
    public void Render(TimeSpan delta) {
        Surface.Clear();
        var str = playerMain.playerShip.GetMemorial(desc);
        int y = 2;
        foreach (var line in str.Replace("\r", "").Split('\n')) {
            Surface.Print(2, y++, Tile.Arr(line));
        }
    }
}

public class IdentityScreen : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Sf Surface;
    Mainframe playerMain;
	public IdentityScreen(int Width, int Height, Mainframe playerMain) {
        this.Surface = new Sf(Width, Height, Fonts.FONT_8x8);
        this.playerMain = playerMain;
#if false
        Children.Add(new LabelButton("Continue", Continue) {
            Position = new Point(1, Surface.Height / 2 - 4), FontSize = playerMain.FontSize * 2
        });
#endif
        /*
        Children.Add(new LabelButton("Save & Quit", Exit) {
            Position = new Point(1, Height / 2 - 2), FontSize = playerMain.FontSize * 2
        });
        */
    }
    public void Continue() {
        Go(playerMain);
    }
    public void Render(TimeSpan delta) {
        Surface.Clear();
        var str = playerMain.playerShip.GetMemorial("Alive");
        int y = 2;
        foreach (var line in str.Replace("\r", "").Split('\n')) {
            Surface.Print(2, y++, line);
        }
    }
    public void ProcessKey(KB kb) {
    }
}
