using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Main;
using Newtonsoft.Json;

namespace RogueFrontier;

class Waves : Event {
    public PlayerShip playerShip;
    public int ticks;
    [JsonIgnore]
    public bool active => true;
    public int difficulty = 90;
    public static Dictionary<string, int> map = new Dictionary<string, int> {
                {"ship_amethyst_i", 120},
                {"ship_beowulf", 180 },
                {"ship_toxin", 150 },
                {"ship_constellation_marksman", 150 },
                {"ship_constellation_marshal", 210 },
                {"ship_errant", 180 },
                {"ship_vassal", 270 },
                {"ship_flawless", 240 },
                {"ship_iron_missileship", 300 },
                {"ship_iron_gunboat", 180 },
                {"ship_laser_drone", 30 },
                {"ship_orion_raider", 90 },
                {"ship_orion_huntsman", 150 },
                {"ship_royal_guard", 450 },
            };
    public Waves(PlayerShip playerShip) {
        this.playerShip = playerShip;
        ships = new();
    }
    public void Validate() {
        map.Keys.ToList().Select(playerShip.world.types.Lookup<ShipClass>);
    }
    HashSet<AIShip> ships;
    public void CreateWave() {

        ships = new HashSet<AIShip>();
        System world = playerShip.world;
        difficulty += 90;


        int difficultyLeft = difficulty;
        List<string> shipList = new List<string>();

    AddShip:
        var shuffled = map.Keys.OrderBy(k => Guid.NewGuid()).ToList();
        var ship = shuffled.FirstOrDefault(s => map[s] < difficultyLeft);
        if (ship != null) {
            shipList.Add(ship);
            difficultyLeft -= map[ship];
            if (difficultyLeft > 0) {
                goto AddShip;
            }
        }

        int i = 0;
        AIShip leader = null;
        shipList.OrderByDescending(s => map[s]).Select(world.types.Lookup<ShipClass>).ToList().ForEach(createShip);
        void createShip(ShipClass shipClass) {

            CompoundOrder behavior = new(new AttackTarget(playerShip));

            AIShip create() =>
                new AIShip(new BaseShip(world,
                    shipClass,
                    playerShip.position + XY.Polar(world.karma.NextDouble(0, 2 * Math.PI), 200)),
                    Sovereign.Gladiator,
                    behavior
                    );
            AIShip ship;


            if (leader == null || i % 4 == 0) {
                ship = create();
                leader = ship;
            } else {
                behavior.orders.Add(new EscortShip(leader, XY.Polar(world.karma.NextDouble(0, 2 * Math.PI), 10)));
                ship = create();
            }
            i++;

            string[] choices = new string[] {
                        "item_simple_fuel_rod",
                        "item_armor_repair_patch",
                        null
                    };
            Func<int, int, int> r = world.karma.NextInteger;
            ship.cargo.UnionWith(Enumerable.Range(0, r(3, 12))
                .Select(i => choices.GetRandom(world.karma))
                .Where(t => t != null)
                .Select(t => new Item(world.types.Lookup<ItemType>(t))));

            world.AddEntity(ship);
            world.AddEffect(new Heading(ship));
            ships.Add(ship);
        }



        playerShip.AddMessage(new Message("Wave incoming!"));
        var f = ships.First();
        playerShip.AddMessage(new Transmission(f, $"{f.name} detected!"));
    }
    public void Update(double delta) {
        ticks++;
        if (ticks % 150 == 0) {
            if (ships.Any(s => s.active)) {
                return;
            }

            CreateWave();
        }
    }
}
