using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using SadConsole.Input;
using Console = SadConsole.Console;
using ArchConsole;
using static IslandHopper.ItemType.GunDesc;

namespace IslandHopper;

public class ShopConsole : Console {
    ItemType preview;
    DictCounter<ItemType> items = new();
    public ShopConsole(int Width, int Height) : base(Width, Height) {
        DefaultBackground = Color.Black;
        DefaultForeground = Color.White;

        int x = 4;
        int y = 4;

        Children.Add(new Label("[Shop]") { Position = new Point(x, y) });
        y += 2;
        foreach (var s in StandardTypes.stdWeapons) {

            var it = s;
            var name = s.name;

            var label = new Label(name) { Position = new(x, y) };

            label.MouseEnter += (o, e) => preview = it;
            Children.Add(label);

            int x2 = 24;
            Label count = null;
            Children.Add(new LabelButton("-", () => {
                items.Decrement(it);
                UpdateCount();
            }) { Position = new(x2, y) });

            count = new Label("0") { Position = new(x2 + 2, y) };
            Children.Add(count);

            Children.Add(new LabelButton("+", () => {
                items.Increment(it);
                UpdateCount();
            }) { Position = new(x2 + 2 + 2 + 2, y) });
            void UpdateCount() => count.text = new ColoredString(items[it].ToString(), Color.White, Color.Black);
            y++;
        }
        y++;
        Children.Add(new LabelButton("Start", () => {
            //TO DO: Create a multi-tiled plane structure to introduce the player
            int size = 128;
            int height = 30;
            var World = new Island() {
                karma = new(0),
                entities = new(new EntityPosition()),
                effects = new(new EffectPosition()),
                voxels = new(size, size, height, new Air()),
                camera = new(0, 0, 0),
                types = null
                //types = new TypeCollection(XElement.Parse(File.ReadAllText("IslandHopperContent/Items.xml")))
            };
            var player = new Player(World, new XYZ(28.5, 29.5, 1));

            foreach ((var w, var c) in items.dict) {
                for (int i = 0; i < c; i++) {
                    player.Inventory.Add(new Item(w, World, player.Position));
                }
            }
            World.player = player;

            //World.AddEntity(new Player(World, new Point3(85, 85, 20)));

            for (int x = 0; x < World.voxels.Width; x++) {
                for (int y = 0; y < World.voxels.Height; y++) {
                    World.voxels[new XYZ(x, y, 0)] = new Grass(World);
                }
            }
            var r = World.karma;

            for (int i = 0; i < 50; i++) {
                //World.entities.Place(World.types.Lookup<ItemType>("itHotRod").GetItem(World, new XYZ(28.5, 29.5, 1)));
                //World.entities.Place(StandardTypes.itStoppedClock.GetItem(World, new XYZ(28.5, 29.5, 1)));
                var s = StandardTypes.stdWeapons;

                Func<int, int> next = r.NextInteger;

                World.entities.Add(new Item(s[r.NextInteger(s.Length)], World, new XYZ(next(World.voxels.Width), next(World.voxels.Height), 1)));
                World.entities.Add(new Enemy(World, new XYZ(next(World.voxels.Width), next(World.voxels.Height), 1)));
            }
            World.entities.Add(World.player);
            //            World.entities.Place(new Enemy(World, new XYZ(35, 35, 1)));

            var plane = new Plane(World, new XYZ(60, 20, 1), new XYZ(0, 0, 0));
            World.AddEntity(plane);
            plane.OnAdded();

            GameHost.Instance.Screen = new PlayerMain(Width, Height, World) { IsFocused = true };
        }) { Position = new Point(x, y++) });
        y++;
        y += 8;

        var str =
@"[Controls]

[A] Announcements
[C] Cancel action
[D] Drop item
[E] Equip item
[G] Get item
[I] Inventory
[L] Look around
[S] Shoot item
[T] Throw item
[U] Use an item
[.] Wait";
        foreach (var l in str.Replace("\r", "").Split("\n")) {
            Children.Add(new Label(l + " ") { Position = new Point(x, y++) });
        }
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        if (preview != null) {
            int x = 32;
            int y = 0;

            preview.image?.Render(this, new Point(x, 0));

            void Print(string s) => this.Print(x, y++, new ColoredString(s, Color.White, Color.Black));

            Print(preview.name);
            y++;
            if (preview.ammo != null) {
                Print($"[Ammo]");
                Print($"Amount: {preview.ammo.amount}");
                y++;
            }
            if (preview.gun != null) {
                Print($"[Gun]");
                switch (preview.gun.projectile) {
                    case BulletDesc b:
                        Print($"Projectile:  Bullet");
                        Print($"Speed:       {b.speed}");
                        Print($"Damage:      {b.damage}");
                        Print($"Lifetime:    {b.lifetime}");
                        break;
                    case FlameDesc f:
                        Print($"Projectile:  Flame");
                        Print($"Speed:       {f.speed}");
                        Print($"Damage:      {f.damage}");
                        Print($"Lifetime:    {f.lifetime}");
                        break;
                    case GrenadeDesc g:
                        Print($"Projectile:  Grenade");
                        Print($"Speed:       {g.speed}");
                        Print($"Fuse time:   {g.grenadeType.fuseTime}");
                        Print($"Blast Radius:{g.grenadeType.explosionRadius}");
                        Print($"Blast Power: {g.grenadeType.explosionDamage}");
                        break;
                }
                y = 8;
                Print($"Clip size:   {preview.gun.clipSize}");
                Print($"Max ammo:    {preview.gun.maxAmmo}");
                Print($"Fire time:   {preview.gun.fireTime}");
                Print($"Reload time: {preview.gun.reloadTime}");
                y++;
            }
            preview.desc.Replace("\r", null).SplitLine(32).ForEach(Print);


        }
        base.Render(delta);
    }
}
