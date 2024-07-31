using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;
using LibGamer;
namespace RogueFrontier;
public class Asteroid : Entity {
    public ulong id { get; set; }
    public XY position { get; set; }
    public bool active { get; set; }
    public Tile tile => (ABGR.Gray, ABGR.Transparent, '%');

    public Asteroid(World world, XY pos) {
        this.id = world.nextId++;
        this.position = pos;
        this.active = true;
    }
    public void Update(double delta) {

    }
}
