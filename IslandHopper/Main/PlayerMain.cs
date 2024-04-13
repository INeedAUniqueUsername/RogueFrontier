using Common;
using static SadConsole.Input.Keys;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole.Input;

namespace IslandHopper;

class PlayerMain : ControlsConsole {
    Island World;
    DateTime lastUpdate;
    int ticks;

    Dictionary<(int, int), int> visibleVoxelHeight = new(); //World XY -> World Z

    public PlayerMain(int Width, int Height, Island World) : base(Width, Height) {
        this.World = World;

        DefaultBackground = Color.Black;
        IsFocused = true;
        UseKeyboard = true;
        UseMouse = true;
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
        ticks++;
        World.realTicks++;
        foreach (var e in World.entities.all) {
            e.UpdateRealtime(delta);
        }
        foreach (var e in World.effects.all) {
            e.UpdateRealtime(delta);
        }
        var now = DateTime.Now;
        if (World.player.AllowUpdate() && IsFocused && (now - lastUpdate).TotalSeconds > 1 / 60f) {
            lastUpdate = now;
            this.DebugInfo("Global Update");
            World.gameTicks++;
            World.entities.UpdateSpace();
            foreach (var e in new List<Entity>(World.entities.all)) {
                e.DebugInfo("UpdateStep() by world");
                e.UpdateStep();
            }
            World.effects.UpdateSpace();
            foreach (var e in new List<Effect>(World.effects.all)) {
                e.DebugInfo("UpdateStep() by world");
                e.UpdateStep();
            }
            World.camera = World.player.Position;
        } else {
            //System.Console.WriteLine("not updating");
        }
        var Removed = new List<Entity>();
        World.entities.all.RemoveWhere(e => {
            bool result = !e.Active;
            if (result) {
                Removed.Add(e);
            }
            return result;
        });
        Removed.ForEach(e => e.OnRemoved());
        World.effects.all.RemoveWhere(e => !e.Active);
    }
    private int HalfWidth { get => Width / 2; }
    private int HalfHeight { get => Height / 2; }
    public override void Render(TimeSpan delta) {
        this.DebugInfo($"Draw({delta})");
        this.Clear();

        int HalfViewWidth = Width / 2 - 8;
        int HalfViewHeight = Height / 2 - 4;
        for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
            for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
                XYZ location = World.camera + new XYZ(x, y, 0);

                this.SetCellAppearance(x + HalfWidth, Height - (y + HalfHeight), GetGlyph(location));
            }
        }

        this.Print(1, 1, "" + World.player.Position.z, Color.White);
        this.Print(1, 2, "" + World.camera.z, Color.White);
        var printY = (Height - 4);
        for (int i = 0; i < 30 && i < World.player.HistoryRecent.Count(); i++) {
            var entry = World.player.HistoryRecent[World.player.HistoryRecent.Count() - 1 - i];
            printY--;
            this.Print(1, printY, entry.ScreenTime > 1 ? entry.Desc : entry.Desc.WithOpacity((byte)(255 * entry.ScreenTime)));
        }

        printY = (Height - 3);
        foreach (var action in World.player.Actions) {
            switch (action) {
                case ShootAction fire:
                    var gun = fire.item.Gun;
                    var delay = gun.ReloadTimeLeft > 0 ? gun.ReloadTimeLeft : gun.FireTimeLeft;
                    this.Print(1, printY, fire.Name + new ColoredString(" ")
                        + GetBar(delay));
                    break;
                case ReloadAction reload:
                    this.Print(1, printY, reload.Name + new ColoredString(" ")
                        + GetBar(reload.ticks));
                    break;
                default:
                    this.Print(1, printY, action.Name);
                    break;
            }
            printY++;
        }

        ColoredString GetBar(int ticks) {
            int second = 60;
            int seconds = ticks / second;
            var baseColor = seconds > 3 ? Color.Red : seconds > 2 ? Color.Orange : seconds > 1 ? Color.Yellow : Color.White;
            var s = new ColoredString(new string('>', Math.Min(second, ticks)), baseColor, Color.Black);

            if (seconds > 0) {
                var remainder = ticks % second;
                var remainderColor = seconds > 2 ? Color.Red : seconds > 1 ? Color.Orange : Color.Yellow;
                foreach (var cg in s.Take(remainder)) {
                    cg.Foreground = remainderColor;
                }
            }

            return s;
        }

        int printX;
        (printX, printY) = (Width - 16, Height - 2);
        this.Print(printX, printY, new ColoredString("Body HP:  ") + new ColoredString(World.player.health.bodyHP.ToString(), Color.Red, Color.Black));
        printY++;
        this.Print(printX, printY, new ColoredString("Blood HP: ") + new ColoredString(World.player.health.bloodHP.ToString(), Color.Red, Color.Black));


        int PreviewWidth = 10;
        int PreviewHeight = 10;

        int previewX = Width - PreviewWidth / 2;
        int previewY = PreviewHeight / 2;
        //Draw a border, up/down arrow, and z difference
        foreach (var watching in World.player.Watch) {
            for (int x = -PreviewWidth / 2; x < PreviewWidth / 2; x++) {
                for (int y = -PreviewHeight / 2; y < PreviewHeight / 2; y++) {
                    XYZ location = watching.Position + new XYZ(x, y, 0);
                    this.SetCellAppearance(x + previewX, -y + previewY, GetGlyph(location));
                }
            }
            previewY += PreviewHeight;
        }
        base.Render(delta);
    }
    public ColoredGlyph GetGlyph(XYZ location) {
        var c = new ColoredGlyph(Color.Transparent, Color.Transparent, ' ');
        if (World.voxels.InBounds(location)) {
            var effects = VisibleOnly(World.effects[location].Select(e => e.SymbolCenter)).ToList();
            var items = VisibleOnly(World.entities[location].Where(e => e is Item).Select(e => e.SymbolCenter)).ToList();
            var entities = World.entities[location].Where(e => !(e is Item)).Select(e => e.SymbolCenter).ToList();
            IEnumerable<ColoredGlyph> VisibleOnly(IEnumerable<ColoredGlyph> e) {
                return e.Where(cg => (cg.Glyph != ' ' && cg.Foreground.A != 0) || cg.Background.A != 0);
            }
            if (effects.Any()) {
                int effectIndex = (ticks / 60) % effects.Count();
                c = effects[effectIndex];
            } else if (items.Any() && (!entities.Any() || ticks % 60 >= 30)) {
                int itemIndex = (ticks / 120) % items.Count();
                c = items[itemIndex];
            } else if (entities.Any()) {
                if (entities.Count() > 1) {
                    int entityIndex = (ticks / 120) % entities.Count();
                    int subticks = ticks % 120;
                    switch (subticks) {
                        case 0:
                        case 2:
                        case 3:
                            c = new ColoredGlyph(Color.White, Color.Black, '|');
                            break;
                        case 4:
                        case 5:
                        case 6:
                            c = new ColoredGlyph(Color.White, Color.Black, '\\');
                            break;
                        case 7:
                        case 8:
                        case 9:
                            c = new ColoredGlyph(Color.White, Color.Black, '-');
                            break;
                        case 10:
                        case 11:
                        case 12:
                            c = new ColoredGlyph(Color.White, Color.Black, '/');
                            break;
                        default:
                            c = entities[entityIndex];
                            break;
                    }
                } else {
                    c = entities.First();
                }
            } else if (!(World.voxels[location] is Air)) {
                //See the glyph right here
                c = World.voxels[location].CharCenter;
                //Remember this glyph
                visibleVoxelHeight[location.xy] = location.zi + 1;
            } else {
                location = location.PlusZ(-1);
                if (World.voxels.InBounds(location) && !(World.voxels[location] is Air)) {
                    //See the glyph below us
                    c = World.voxels[location].CharAbove;
                    //Remember this glyph
                    visibleVoxelHeight[location.xy] = location.zi = 1;
                } else if (visibleVoxelHeight.TryGetValue(location.xy, out int z) && z <= location.zi) {
                    var originalZ = location.zi;
                    c = GetFromAbove(originalZ, z);
                } else {
                    var originalZ = location.zi;
                    //Scan for highest voxel below us
                    location = location.PlusZ(-1);
                    while (World.voxels.InBounds(location) && (World.voxels[location] is Air)) {
                        location = location.PlusZ(-1);
                    }
                    if (!(World.voxels[location] is Air)) {
                        //Make sure we get the view from above
                        location = location.PlusZ(1);
                    }
                    visibleVoxelHeight[location.xy] = location.zi;
                    c = GetFromAbove(originalZ, location.zi);
                }

                ColoredGlyph GetFromAbove(int originalZ, int visibleZ) {
                    //var glyph = 177;
                    var glyph = '.';
                    if (visibleZ > -1) {
                        //var Symbol = World.voxels[new XYZ(location.x, location.y, visibleZ)].CharAbove;
                        var Symbol = GetGlyph(new XYZ(location.x, location.y, visibleZ));
                        var heightDiff = originalZ - visibleZ;
                        return new ColoredGlyph(
                            Color.White.Blend(Symbol.Foreground.WithValues(alpha: Symbol.Foreground.A - heightDiff * 2)),
                            Color.White.Blend(Symbol.Background.WithValues(alpha: Symbol.Background.A - heightDiff * 2)),
                            Symbol.Glyph);
                    } else {
                        return new ColoredGlyph(Color.Black, Color.Black);
                    }
                }
            }
        }
        return c;
    }

    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        this.DebugInfo("ProcessKeyboard");
        var player = World.player;

        if (info.IsKeyDown(Keys.Escape) && info.IsKeyDown(Keys.RightShift)) {
            Environment.Exit(0);
            throw new Exception();
        }

        if (info.IsKeyPressed(Keys.Escape)) {
            SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height);
        }

        if (info.IsKeyDown(Keys.Up) || info.IsKeyDown(Keys.Down) || info.IsKeyDown(Keys.Right) || info.IsKeyDown(Keys.Left)) {
            XYZ direction = new();
            if (info.IsKeyDown(Keys.Up)) {
                direction += new XYZ(0, 1);
            }
            if (info.IsKeyDown(Keys.Down)) {
                direction += new XYZ(0, -1);
            }
            if (info.IsKeyDown(Keys.Right)) {
                direction += new XYZ(1, 0);
            }
            if (info.IsKeyDown(Keys.Left)) {
                direction += new XYZ(-1, 0);
            }
            if (direction.Magnitude > 0) {
                direction = direction.Normal;
                if (info.IsKeyDown(Keys.RightControl)) {
                    //Run
                    if (World.player.OnGround() && !World.player.Actions.Any(a => a is Jump)) {
                        double runAccel = 30;
                        int runTime = 15;
                        World.player.Actions.Add(new Jump(World.player, direction * runAccel, runTime));
                        World.player.Actions.Add(new Jump(World.player, new XYZ(0, 0, 2), 5));
                    }
                } else {
                    if (World.player.OnGround() && !World.player.Actions.Any(a => a is WalkAction))
                        World.player.Actions.Add(new WalkAction(World.player, direction));
                }
            }

        }

        //Other actions besides walking/running
        if (info.IsKeyDown(Keys.OemOpenBrackets)) {
            //Go up stairs
        } else if (info.IsKeyDown(Keys.OemCloseBrackets)) {
            //Go down stairs
        } else if (info.IsKeyDown(Keys.J)) {
            //Jump up

            //Running is also a jump action but without z > 0, so we can jump while running
            if (player.OnGround() && !player.Actions.Any(a => a is Jump j && j.z > 0))
                player.Actions.Add(new Jump(player, new XYZ(0, 0, 3), 10));
        } else if (info.IsKeyPressed(Keys.A)) {
            if (info.IsKeyDown(Keys.LeftShift)) {
                Children.Add(new MeleeMenu(Width, Height, player, World.entities.all.Where(e => (e.Position - player.Position).Magnitude < 1.5)) { IsFocused = true });
            } else {
                Children.Add(new HistoryMenu(Width, Height, player.HistoryLog) { IsFocused = true });
            }
        } else if (info.IsKeyPressed(Keys.C)) {
            //
            Children.Add(new ListMenu<EntityAction>(Width, Height, "Select actions to cancel. Press ESC to finish.", player.Actions.Select(Action => new ListAction(Action)), Action => {
                //Just cancel the action immediately for now
                player.Actions.Remove(Action);
                return true;
            }) { IsFocused = true });

        } else if (info.IsKeyPressed(Keys.D)) {
            Children.Add(new ListMenu<IItem>(Width, Height, "Select inventory items to drop. Press ESC to finish.", player.Inventory.Select(Item => new ListItem(Item)), item => {
                //Just drop the item for now
                player.Inventory.Remove(item);
                World.entities.Add(item);

                World.player.AddMessage(new InfoEvent(new ColoredString("You drop: ") + item.Name.WithBackground(Color.Black)));
                return true;
            }) { IsFocused = true });
        } else if (info.IsKeyPressed(E)) {

            Children.Add(new ListMenu<IItem>(Width, Height, "Select inventory items to equip. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {

                if (item.Head != null && player.Equipment.head == null) {
                    player.Inventory.Remove(item);
                    player.Equipment.head = item;
                    World.player.AddMessage(new InfoEvent(new ColoredString("You equip: ") + item.Name.WithBackground(Color.Black)));
                }
                return false;
            }) { IsFocused = true });

        } else if (info.IsKeyPressed(Keys.G)) {
            Children.Add(new ListMenu<IItem>(Width, Height, "Select items to get. Press ESC to finish.", World.entities[World.player.Position].OfType<IItem>().Select(Item => new ListItem(Item)), item => {
                //Just take the item for now
                World.player.Inventory.Add(item);
                World.entities.Remove(item);

                World.player.AddMessage(new InfoEvent(new ColoredString("You get: ") + item.Name.WithBackground(Color.Black)));
                return true;
            }) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.I)) {
            Children.Add(new ListMenu<IItem>(Width, Height, "Select inventory items to examine. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {
                //	Later, we might have a chance of identifying the item upon selecting it in the inventory

                //World.player.Witness(new SelfEvent(new ColoredString("You examine: ") + item.Name.WithBackground(Color.Black)));
                return false;
            }) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.L)) {
            Children.Add(new LookMenu(Width, Height, World) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.R)) {
            Children.Add(new ReloadMenu(Width, Height, World, World.player) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.S)) {
            //TODO: Ask the player if they want to cancel their current ShootAction
            Children.Add(new ShootMenu(Width, Height, World, World.player) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.T)) {
            Children.Add(new ThrowMenu(Width, Height, World, World.player) { IsFocused = true });
        } else if (info.IsKeyPressed(Keys.U)) {
            //World.AddEntity(new ExplosionSource(World, World.player.Position, 10));
            //Use menu
            Children.Add(new ListMenu<IItem>(Width, Height, "Select items to use. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {

                if (item.Grenade?.Armed == false) {
                    item.Grenade.Arm();
                    player.AddMessage(new InfoEvent(new ColoredString("You arm: ") + item.Name.WithBackground(Color.Black)));
                }
                return false;
            }) { IsFocused = true });
        } else if (info.IsKeyDown(Keys.OemPeriod) && info.IsKeyDown(Keys.RightControl)) {
            if (!World.player.Actions.Any(a => a is WaitAction)) {
                World.player.Actions.Add(new WaitAction(1));
            }
        } else if (info.IsKeyPressed(Keys.OemPeriod)) {
            if (!World.player.Actions.Any(a => a is WaitAction)) {
                //Debug.Print("waiting");
                World.player.Actions.Add(new WaitAction(Constants.STEPS_PER_SECOND));
                World.player.AddMessage(new InfoEvent(new ColoredString("You wait")));
            }
        }
        return base.ProcessKeyboard(info);
    }
}
interface GameMenu {
    bool Done { get; }
    void Update(TimeSpan delta);
    void Draw(TimeSpan delta);

    void ProcessKeyboard(SadConsole.Input.Keyboard info);
}
static class Help {
    public static void Transparent(this SadConsole.Console c) {
        c.DefaultBackground = Color.Transparent;
        c.DefaultForeground = Color.Transparent;
        c.Clear();
    }
}
