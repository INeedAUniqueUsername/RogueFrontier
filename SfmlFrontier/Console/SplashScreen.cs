using Common;
using LibGamer;

namespace RogueFrontier;

public class SplashScreen : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Action next;
    System World;
    public Dictionary<(int, int), Tile> tiles;
    XY screenCenter;
    double time = 8;

    Sf sf;
    int Width => sf.GridWidth;
    int Height => sf.GridHeight;

	public SplashScreen(int Width, int Height, Action next) {
        this.next = next;
        sf = new Sf(Width, Height, Fonts.FONT_8x8);
        var r = new Random(3);
        this.World = new();
        tiles = new();
        screenCenter = new(Width / 2, Height / 2);
        var lines = new string[] {
                @"             /^\             ",
                @"        ___ / | \ ___        ",
                @"       /___/__|__\___\       ",
                @"    ___      | |      ___    ",
                @"   /__ /\    | |    /\ __\   ",
                @"     //  \   | |   /  \\     ",
                @"    //  /^\  | |  /^\  \\    ",
                @"   //    |   | |   |    \\   ",
                @"  // ^   |   | |   |   ^ \\  ",
                @" //  |   |   | |   |   |  \\ ",
                @"||-------------------------||",
                @"||   INeedAUniqueUsername    ||",
                @"||-------------------------||"
            };
        for (int y = 0; y < lines.Length; y++) {
            var s = lines[y];
            var pos = new XY(-s.Length, -lines.Length + y * 2);
            var margin = new AIShip(new(World, ShipClass.empty, pos) { rotationDeg = 90 }, new(), null);
            for (int x = 0; x < s.Length; x++) {
                var c = s[x];
                if (c == ' ')
                    continue;
                var shipClass = new ShipClass() {
                    thrust = 2,
                    maxSpeed = 25,
                    rotationAccel = 8,
                    rotationDecel = 12,
                    rotationMaxSpeed = 10,
                    tile = new Tile(ABGR.LightCyan, ABGR.Transparent, c),
                    devices = new(),
                    damageDesc = ShipClass.empty.damageDesc
                };
                XY p = null;


                switch (r.Next(0, 4)) {
                    case 0:
                        p = new(-Width, r.Next(-Height, Height));
                        break;
                    case 1:
                        p = new(Width, r.Next(-Height, Height));
                        break;
                    case 2:
                        p = new (r.Next(-Width, Width), Height);
                        break;
                    case 3:
                        p = new (r.Next(-Width, Width), -Height);
                        break;
                }
                var ship = new AIShip(new(World, shipClass, p), new(), new FollowShip(margin, new(0, -2 - (x * 2))));
                World.AddEntity(ship);
                //World.AddEffect(new Heading(ship));
            }
        }
    }
    public void Update(TimeSpan timeSpan) {
        World.UpdateAdded();
        World.UpdateActive(timeSpan.TotalSeconds);
        World.UpdateRemoved();

        tiles.Clear();
        World.PlaceTiles(tiles);

        time -= timeSpan.TotalSeconds;
        if (time < 0) {
            next();
        }

    }
    public void Render(TimeSpan drawTime) {
        sf.Clear();

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var g = sf.GetGlyph(x, y);

                var location = new XY(x + 0.1, y + 0.1) - screenCenter;

                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    sf.SetTile(x, y, tile);
                }
            }
        }
        Draw(sf);
    }
}
