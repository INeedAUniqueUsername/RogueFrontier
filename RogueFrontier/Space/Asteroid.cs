using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Console = SadConsole.Console;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;
namespace RogueFrontier;
public class Asteroid : Entity {
    public ulong id { get; set; }
    public XY position { get; set; }
    public bool active { get; set; }
    public ColoredGlyph tile => new(Color.Gray, Color.Transparent, '%');

    public Asteroid(System world, XY pos) {
        this.id = world.nextId++;
        this.position = pos;
        this.active = true;
    }
    public void Update(double delta) {

    }
}
