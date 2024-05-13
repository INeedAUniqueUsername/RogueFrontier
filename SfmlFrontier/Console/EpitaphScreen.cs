
using ArchConsole;
using LibGamer;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Linq;

namespace RogueFrontier;

public class EpitaphScreen : IScene {
    Mainframe playerMain;
    Epitaph epitaph;

    Sf Surface;
    public Action<IScene> Go { set; get; } = _ => { };
    public Action<Sf> Draw { set; get; } = _ => { };

	public EpitaphScreen(Mainframe playerMain, Epitaph epitaph) {
        this.playerMain = playerMain;
        this.epitaph = epitaph;
        this.Surface = new Sf(playerMain.sf.Width, playerMain.sf.Height);
#if false
        this.Children.Add(new LabelButton("Resurrect", Resurrect) {
            Position = new Point(1, Height / 2 - 4), FontSize = playerMain.FontSize * 2
        });

        this.Children.Add(new LabelButton("Title Screen", Exit) { Position = new Point(1, Height / 2 - 2), FontSize = playerMain.FontSize * 2 });
#endif
    }
    public void Resurrect() {
        var playerShip = playerMain.playerShip;
        var world = playerShip.world;

        //Restore mortality chances
        playerShip.mortalChances = 3;
        
        //To do: Restore player HP
        playerShip.ship.damageSystem.Restore();

        playerShip.powers.ForEach(p=>p.cooldownLeft=0);

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

        Go(new FadeIn(new Pause(playerMain.sf, Resume, 2), playerMain.sf));
        void Resume() {
            Go(playerMain);
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
            var ts = new TitleScreen(Surface.Width, Surface.Height, new System(playerMain.world.universe));
            Go(new TitleSlideOpening(ts, ts.sf));
        }
    }
    public void Render(TimeSpan delta) {
        Surface.Clear();
        var str = playerMain.playerShip.GetMemorial(epitaph.desc);
        int y = 2;
        foreach (var line in str.Replace("\r", "").Split('\n')) {
            Surface.Print(2, y++, Tile.Arr(line));
        }
        if (epitaph.deathFrame != null) {
            var size = epitaph.deathFrame.GetLength(0);
            for (y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Surface.Tile[Surface.Width - x - 2, y + 1] = epitaph.deathFrame[x, y];
                }
            }
        }
    }
}
public class IntermissionScreen : IScene {
    Mainframe playerMain;
    LiveGame game;
    string desc;

    public Action<IScene> Go { set; get; } = _ => { };
    public Action<Sf> Draw { get; set; } = _ => { };
    Sf Surface;

	public IntermissionScreen(Mainframe playerMain, LiveGame game, string desc) {
        this.Surface = new Sf(playerMain.sf.Width, playerMain.sf.Height);
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
        game.OnLoad(playerMain);

        Go(new TitleSlideOpening(new Pause(playerMain.sf, Resume, 4), playerMain.sf));
        void Resume() {
            Go(playerMain);
            playerMain.ShowUI();
        }
    }
    public void Exit() {
        game.Save();
        var ts = new TitleScreen(Surface.Width, Surface.Height, new System(playerMain.world.universe));
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
    Sf Surface;
    Mainframe playerMain;

    public Action<IScene> Go { set; get; } = _ => { };
    public Action<Sf> Draw { set; get; } = _ => { };

	public IdentityScreen(Mainframe playerMain) {
        this.Surface = new Sf(Program.WIDTH, Program.HEIGHT);
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
