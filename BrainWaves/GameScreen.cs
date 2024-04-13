
using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using Console = SadConsole.Console;
using SadConsole.Input;
using System.Linq;

namespace BrainWaves;

public class GameScreen : Console {
    World World;
    Player Player;
    WorldScreen WorldScreen;
    MessageScreen MessageScreen;
    public GameScreen(int Width, int Height) : base(Width, Height) {
        World = new World();
        new WorldBuilder(World).Build();

        World.AddEntity(new Guard(World, new XY(-2, 0)));

        Player = new Player(World, new XY(0, 0));
        World.AddEntity(Player);
        World.UpdatePresent();
        World.UpdateSpace();

        World.brightness.Clear();
        foreach (var e in World.entities.all.OfType<Light>()) {
            e.UpdateLight();
        }

        WorldScreen = new WorldScreen(Width, Height - 10, World, Player);
        MessageScreen = new MessageScreen(Width, 10, Player) { Position = new Point(0, Height - 10) };

        Children.Add(WorldScreen);
        Children.Add(MessageScreen);
    }

    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var k in keyboard.KeysPressed) {
            var p = Player.Position;
            switch (k.Key) {
                case Keys.Up:
                    p += new XY(0, 1);
                    Move(p);
                    break;
                case Keys.Down:
                    p += new XY(0, -1);
                    Move(p);
                    break;
                case Keys.Right:
                    p += new XY(1, 0);
                    Move(p);
                    break;
                case Keys.Left:
                    p += new XY(-1, 0);
                    Move(p);
                    break;
            }
            void Move(XY p) {
                Player.Move(p);
                World.UpdatePresent();
                World.UpdateActive();
                World.UpdateSpace();
                WorldScreen.camera = Player.Position;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }
}
public class WorldScreen : Console {
    public World World;
    public XY camera;
    public Player Player;
    public WorldScreen(int Width, int Height, World World, Player Player) : base(Width, Height) {
        this.World = World;
        this.Player = Player;
        camera = new XY();
    }
    public override void Update(TimeSpan delta) {
        foreach (var e in World.entities.all) {
            e.UpdateRealtime();
        }
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        DrawWorld();

        this.Print(0, 0, $"{Player.Position.xi}", Color.Red);
        this.Print(0, 1, $"{Player.Position.yi}", Color.Red);

        base.Render(delta);
    }
    public void DrawWorld() {
        int ViewWidth = Width;
        int ViewHeight = Height;
        int HalfViewWidth = ViewWidth / 2;
        int HalfViewHeight = ViewHeight / 2;

        for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
            for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
                XY location = camera + new XY(x, y);

                var xScreen = x + HalfViewWidth;
                var yScreen = HalfViewHeight - y;

                var b = World.brightness[location];
                var back = new Color(b, b, b);

                var fore = Color.Black;
                if (b < 128) {
                    fore = Color.White;
                }
                ColoredGlyph Glyph(int c) => new ColoredGlyph(fore, back, c);
                void SetCell(ColoredGlyph cg) => this.SetCellAppearance(xScreen, yScreen, cg);
                void Print(int c) => this.SetCellAppearance(xScreen, yScreen, Glyph(c));

                if (Player.visible.Contains(location)) {
                    var entities = World.entities[location.roundAway];

                    ColoredGlyph cg;
                    if (entities.Any()) {
                        cg = new ColoredGlyph(fore, back, entities.First().Tile.Glyph);
                    } else {
                        var v = World.voxels.Get(location.xi, location.yi);
                        cg = Glyph(v.Symbol);
                    }

                    if (Player.apparentEnemyVision.Contains(location)) {
                        cg.Background = cg.Background.Blend(new Color(255, 0, 0, 51));
                        cg.Foreground = cg.Foreground.Blend(new Color(255, 0, 0, 51));
                    }
                    SetCell(cg);
                } else {
                    var v = World.voxels.Get(location.xi, location.yi);
                    if (v == null) {
                        back = Color.White;
                        Print(0);
                    } else if (World.entities[location.roundAway].OfType<Guard>().Any(g => (g.tick % 60) < 10)) {
                        SetCell(new ColoredGlyph(Color.Red, back, 3));
                    } else if (Player.seen.Contains(location)) {
                        Print(v.Symbol);
                    } else {
                        Print(0);
                    }
                }
            }
        }
    }
}
class MessageScreen : Console {
    Player Player;
    public MessageScreen(int Width, int Height, Player Player) : base(Width, Height) {
        this.Player = Player;
    }
    public override void Render(TimeSpan delta) {
        int y = 0;
        foreach (var (s, count) in Player.messages.dict) {
            this.Print(0, y++, $"{s,-32} (x{count})", Color.Black, Color.White);
        }
        base.Render(delta);
    }
}
