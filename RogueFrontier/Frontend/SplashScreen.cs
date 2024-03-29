using System;
using SadConsole;
using Console = SadConsole.Console;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;

namespace RogueFrontier;

public class SplashScreen : Console {
    Action next;
    System World;
    public Dictionary<(int, int), ColoredGlyph> tiles;
    XY screenCenter;
    double time = 8;
    public SplashScreen(Action next) : base(Program.Width / 2, Program.Height / 2) {
        this.next = next;
        FontSize = FontSize * 2;
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
                    tile = new ColoredGlyph(Color.LightCyan, Color.Transparent, c),
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
    public override void Update(TimeSpan timeSpan) {
        World.UpdateAdded();
        World.UpdateActive(timeSpan.TotalSeconds);
        World.UpdateRemoved();

        tiles.Clear();
        World.PlaceTiles(tiles);

        time -= timeSpan.TotalSeconds;
        if (time < 0) {
            next();
        }
        base.Update(timeSpan);

    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var g = this.GetGlyph(x, y);

                var location = new XY(x + 0.1, y + 0.1) - screenCenter;

                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    this.SetCellAppearance(x, y, tile);
                }
            }
        }
        base.Render(drawTime);
    }
}
