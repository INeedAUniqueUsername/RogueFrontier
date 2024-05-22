using LibSadConsole;
using LibGamer;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using LabelButton = LibSadConsole.LabelButton;
using Console = SadConsole.Console;

namespace RogueFrontier;

class LoadPane : IScene {
    Profile profile;
    Sf sf;
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	public List<object> Children = new();

	public LoadPane(int Width, int Height, Profile profile) {
        sf = new Sf(Width, Height, Fonts.FONT_6x8);
        this.profile = profile;
        Init();
    }
    public void Reset() {
        Children.Clear();
        Init();
    }
    public void Init() {
        int x = 2;
        int y = 0;

        var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.*");
        if (files.Any()) {
            var dir = Path.GetFullPath(".");
            foreach (var file in files) {

                var b = new LabelButton(file.Replace(dir, null), () => {
                    var t = File.ReadAllText(file);
                    var loaded = SaveGame.Deserialize(t);

                    var s = (Console)GameHost.Instance.Screen;
                    int Width = s.Width, Height = s.Height;

                    switch (loaded) {
                        case LiveGame live: {
                                var playerMain = new Mainframe(Width, Height, profile, live.playerShip);
                                //live.playerShip.player.Settings;

                                live.playerShip.onDestroyed += playerMain;
                                Go(playerMain);
                                

                                //If we have any load hooks, trigger them now
                                //live.hook?.Value(playerMain);
                                break;
                            }
                        case DeadGame dead: {
                                var playerMain = new Mainframe(Width, Height, profile, dead.playerShip);
                                playerMain.camera.position = dead.playerShip.position;
                                playerMain.PlaceTiles(new());
                                var deathScreen = new EpitaphScreen(playerMain, dead.epitaph);
                                dead.playerShip.onDestroyed +=playerMain;
                                Go(deathScreen);
                                break;
                            }
                    }
                }) { Position = new Point(x, y++), /*FontSize = FontSize*/ };
                Children.Add(b);
            }
        } else {
            Children.Add(new Label("No save files found") { Position = new Point(x, y++), /*FontSize = FontSize*/ });
        }
    }

    public void HandleKey(Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            Go(null);
        }
    }
    public void HandleMouse(MouseScreenObjectState state) {
    }
}
