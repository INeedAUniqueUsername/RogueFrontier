using Common;
using SadRogue.Primitives;
using SadConsole.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

namespace IslandHopper;

interface ListChoice<T> {
    T Value { get; }
    ColoredGlyph GetSymbolCenter();
    ColoredString GetName();
}
class ListAction : ListChoice<EntityAction> {
    public EntityAction Value { get; }
    public ListAction(EntityAction Value) {
        this.Value = Value;
    }
    public ColoredGlyph GetSymbolCenter() => Value is ShootAction s ? s.item.SymbolCenter : new ColoredGlyph() { Glyph = ' ' };
    public ColoredString GetName() => Value.Name;
}
class ListItem : ListChoice<IItem> {
    public IItem Value { get; }
    public ListItem(IItem Value) {
        this.Value = Value;
    }
    public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
    public ColoredString GetName() => Value.Name;
}
class ListEntity : ListChoice<Entity> {
    public Entity Value { get; }
    public ListEntity(Entity Value) {
        this.Value = Value;
    }
    public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
    public ColoredString GetName() => Value.Name;
}
class ListMenu<T> : Console {
    string hint;
    List<ListChoice<T>> Choices;
    Func<T, bool> select;       //Fires when we select an item. If true, then we remove the item from the selections
    int startIndex;
    public static ListMenu<IItem> itemSelector(int Width, int Height, string hint, IEnumerable<IItem> Items, Func<IItem, bool> select) {
        return new ListMenu<IItem>(Width, Height, hint, Items.Select(item => new ListItem(item)), select);
    }
    public ListMenu(int Width, int Height, string hint, IEnumerable<ListChoice<T>> Choices, Func<T, bool> select) : base(Width, Height) {
        UseKeyboard = true;

        this.hint = hint;
        this.Choices = new List<ListChoice<T>>(Choices);
        this.select = select;
        startIndex = 0;
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        int x = 5;
        int y = 5;

        var printbw = (int x, int y, string s) => this.Print(x, y, s, Color.White, Color.Black);
        printbw(x, y, hint);
        y++;
        if (Choices.Count > 0) {
            string UP = ((char)24).ToString();
            string LEFT = ((char)27).ToString();
            printbw(x, y, "    ");
            if (CanScrollUp) {
                printbw(x, y, UP);
                if (CanPageUp)
                    printbw(x + 2, y, LEFT);
                printbw(x + 4, y, startIndex.ToString());
            } else {
                printbw(x, y, "-");
            }
            y++;

            for (int i = startIndex; i < startIndex + 26; i++) {
                if (i < Choices.Count) {
                    char binding = (char)('a' + (i - startIndex));
                    this.Print(x, y, "" + binding, Color.LimeGreen, Color.Black);
                    this.Print(x + 1, y, " ", Color.Black, Color.Black);
                    this.Print(x + 2, y, Choices[i].GetSymbolCenter().ToColoredString());
                    this.Print(x + 3, y, " ", Color.Black, Color.Black);
                    this.Print(x + 4, y, Choices[i].GetName());
                } else {
                    this.Print(x, y, ".", Color.Gray, Color.Black);
                }
                y++;
            }

            string DOWN = ((char)25).ToString();
            string RIGHT = ((char)26).ToString();
            printbw(x, y, "    ");
            if (CanScrollDown) {
                printbw(x, y, DOWN);
                if (CanPageDown)
                    printbw(x + 2, y, RIGHT);
                printbw(x + 4, y, ((Choices.Count - 26) - startIndex).ToString());
            } else {
                printbw(x, y, "-");
            }

            y++;
        } else {
            this.Print(x, y, "There is nothing here.", Color.Red, Color.Black);
        }

        base.Render(delta);
    }
    private bool CanScrollUp => startIndex > 0;
    private bool CanPageUp => startIndex - 25 > 0;
    private bool CanScrollDown => startIndex + 26 < Choices.Count;
    private bool CanPageDown => startIndex + 26 + 25 < Choices.Count;
    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            Parent.IsFocused = true;
            Parent.Children.Remove(this);
        } else {
            ListControls(info);
        }
        return true;
    }
    public void ListControls(SadConsole.Input.Keyboard info) {
        if (info.IsKeyPressed(Keys.Up)) {
            if (CanScrollUp)
                startIndex--;
        } else if (info.IsKeyPressed(Keys.Down)) {
            if (CanScrollDown)
                startIndex++;
        } else if (info.IsKeyPressed(Keys.Left)) {
            if (CanPageUp)
                startIndex -= 26;
            else
                startIndex = 0;
        } else if (info.IsKeyPressed(Keys.Right)) {
            if (CanPageDown)
                startIndex += 26;
            else
                startIndex = Math.Max(0, Choices.Count - 26);
        } else {
            //If this key represents an item, then we select it
            foreach (var k in info.KeysPressed) {
                var key = k.Key;
                if (Keys.A <= key && key <= Keys.Z) {
                    //A represents the first displayed item (i.e. the one at startIndex). Z represents the last displayed item (startIndex + 25)
                    int index = (key - Keys.A) + startIndex;
                    if (index < Choices.Count) {
                        //Select the item
                        var selected = Choices.ElementAt(index);
                        if (select.Invoke(selected.Value)) {
                            Choices.Remove(selected);

                            //If we're at the bottom of the menu and we're removing an item here, move the list view up so that we don't have empty slots
                            if (Choices.Count > 25 && !CanPageDown) {
                                startIndex = Choices.Count - 26;
                            }
                        }

                    }
                    break;
                }
            }
        }
    }
}
class ReloadMenu : Console {
    Island w;
    Player p;
    ListMenu<IItem> itemSelector, ammoSelector;

    public ReloadMenu(int width, int height, Island w, Player p) : base(width, height) {
        UseKeyboard = true;

        this.w = w;
        this.p = p;
    }
    public override void Update(TimeSpan time) {
        base.Update(time);
        if (itemSelector == null) {
            UpdateItemSelector();
        }
        ((Console)ammoSelector ?? itemSelector).Update(time);
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        base.Render(drawTime);
        ((Console)ammoSelector ?? itemSelector).Render(drawTime);
    }
    public void UpdateItemSelector() {
        itemSelector = new(Width, Height, "Select item to reload. ESC to cancel.", p.Inventory.Where(Item => Item.Gun != null).Select(Item => new ListItem(Item)), target => {
            //Remove the item selector and add a location selector
            ammoSelector = new(Width, Height, "Select item to consume. ESC to cancel.",
                p.Inventory.Where(i => i.Type.ammo != null).Select(i => new ListItem(i)), ammo => {
                    var g = target.Gun;
                    p.Actions.Add(new ReloadAction(g, ammo.Type.ammo.amount));
                    p.Inventory.Remove(ammo);
                    Close();
                    return true;
                });
            return false;
        });
    }
    public void Close() {
        Parent.IsFocused = true;
        Parent.Children.Remove(this);
    }
    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            if (ammoSelector != null) {
                ammoSelector = null;
                //UpdateItemSelector();
            } else {
                Close();
            }
            return true;
        } else {
            return ((Console)ammoSelector ?? itemSelector).ProcessKeyboard(info);
        }
    }
    class FireMenu : Console {
        Player p;
        Gun g;
        Entity target;
        public FireMenu(int Width, int Height, Player p, Gun g, Entity target) : base(Width, Height) {
            this.p = p;
            this.g = g;
            this.target = target;
        }
    }
}
class ShootMenu : Console {
    Island w;
    Player p;
    ListMenu<IItem> itemSelector;
    LookMenu targetSelector;

    public ShootMenu(int width, int height, Island w, Player p) : base(width, height) {
        UseKeyboard = true;

        this.w = w;
        this.p = p;
    }
    public override void Update(TimeSpan time) {
        base.Update(time);
        if (itemSelector == null) {
            UpdateItemSelector();
        }
        ((Console)targetSelector ?? itemSelector).Update(time);
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        base.Render(drawTime);
        ((Console)targetSelector ?? itemSelector).Render(drawTime);
    }
    public void UpdateItemSelector() {
        itemSelector = new(Width, Height, "Select item to shoot with. ESC to cancel.", p.Inventory.Where(Item => Item.Gun != null).Select(Item => new ListItem(Item)), item => {
            //Remove the item selector and add a location selector
            targetSelector = new(Width, Height, w, "Select target to shoot at. Enter to select a general location. ESC to cancel.", target => {
                p.Actions.Add(new ShootAction(p, item, new TargetEntity(target)));
                Close();
                return false;
            }, xyz => {
                p.Actions.Add(new ShootAction(p, item, new TargetPoint(xyz)));
                Close();
            });
            return false;
        });
    }
    public void Close() {
        Parent.IsFocused = true;
        Parent.Children.Remove(this);
    }
    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            if (targetSelector != null) {
                targetSelector = null;
                //UpdateItemSelector();
            } else {
                Close();
            }
            return true;
        } else {
            return ((Console)targetSelector ?? itemSelector).ProcessKeyboard(info);
        }
    }
    class FireMenu : Console {
        Player p;
        Gun g;
        Entity target;
        public FireMenu(int Width, int Height, Player p, Gun g, Entity target) : base(Width, Height) {
            this.p = p;
            this.g = g;
            this.target = target;
        }
    }
}
class ThrowMenu : Console {
    Island w;
    Player p;
    ListMenu<IItem> itemSelector;
    LookMenu targetSelector;
    public ThrowMenu(int width, int height, Island w, Player p) : base(width, height) {
        this.w = w;
        this.p = p;

        this.Transparent();

        UpdateItemSelector();
    }
    public override void Update(TimeSpan time) {
        base.Update(time);
        /*
        if (itemSelector == null) {
            UpdateItemSelector();
        }
        */
        if (targetSelector != null) {
            targetSelector.Update(time);
        } else {
            itemSelector.Update(time);
        }
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        base.Render(drawTime);
        ((Console)targetSelector ?? itemSelector).Render(drawTime);
    }
    public void UpdateItemSelector() {
        itemSelector = new(Width, Height, "Select item to throw. ESC to cancel.", p.Inventory.Select(Item => new ListItem(Item)), item => {
            targetSelector = new(Width, Height, w, "Select target to throw item at. Enter to select a general location. ESC to cancel.", target => {
                ThrowItem(target, item);
                Close();
                return false;
            }, point => {
                ThrowItem(point, item);
                Close();
            }) { IsFocused = false };
            return false;
        }) { IsFocused = false };
    }
    public void ThrowItem(Entity target, IItem item) {
        if (Main.CalcAim2(target.Position - p.Position, 60, out XYZ lower, out XYZ _)) {
            item.Velocity = lower / Constants.STEPS_PER_SECOND;
            //Remove the item from the player's inventory and create a thrown item in the world
            p.Inventory.Remove(item);
            var t = new ThrownItem(p, item);
            w.AddEntity(t);
            //Track this on the player
            p.Watch.Add(t);
            p.AddMessage(new InfoEvent(new ColoredString("You throw: ") + item.Name.WithBackground(Color.Black) + new ColoredString(" | at: ") + target.Name.WithBackground(Color.Black)));
        }
    }
    public void ThrowItem(XYZ target, IItem item) {
        if (Main.CalcAim2(target - p.Position, 60, out XYZ lower, out XYZ _)) {
            item.Velocity = lower / Constants.STEPS_PER_SECOND;
            //Remove the item from the player's inventory and create a thrown item in the world
            p.Inventory.Remove(item);
            var t = new ThrownItem(p, item);
            w.AddEntity(t);
            //Track this on the player
            p.Watch.Add(t);
            p.AddMessage(new InfoEvent(new ColoredString("You throw: ") + item.Name.WithBackground(Color.Black)));
        }
    }
    public void Close() {
        Parent.IsFocused = true;
        Parent.Children.Remove(this);
    }
    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            if (targetSelector != null) {
                targetSelector = null;
                //UpdateItemSelector();
            } else {
                Close();
            }
            return true;
        } else {
            return ((Console)targetSelector ?? itemSelector).ProcessKeyboard(info);
        }
    }
}
class LookMenu : Console {
    Island world;

    string hint;
    Func<Entity, bool> select;
    Action<XYZ> selectAt;

    Timer cursorBlink;
    bool cursorVisible;

    ListMenu<Entity> examineMenu;

    readonly ColoredGlyph cursor = new ColoredGlyph(Color.Yellow, Color.Black, '?');

    public LookMenu(int width, int height, Island world, string hint = null, Func<Entity, bool> select = null, Action<XYZ> selectAt = null) : base(width, height) {
        UseKeyboard = true;

        this.world = world;
        this.hint = hint ?? "Select an entity to examine";
        this.select = select ?? (e => false);
        this.selectAt = selectAt ?? (xyz => { });
        cursorVisible = true;
        cursorBlink = new Timer(0.4, () => {
            cursorVisible = !cursorVisible;
        });
        UpdateExamine();
    }

    public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
        int delta = 1;    //Distance moved by camera
        if (info.IsKeyDown(Keys.RightControl)) {
            delta *= 8;
        }
        /*
        if (info.IsKeyDown(Keys.RightControl)) {
            examineMenu.ListControls(info);
        } else 
        */
        if (info.IsKeyPressed(Keys.Up)) {
            if (info.IsKeyDown(Keys.RightShift)) {
                world.camera += new XYZ(0, 0, delta);
            } else {
                world.camera += new XYZ(0, delta);
            }
            UpdateExamine();
        } else if (info.IsKeyPressed(Keys.Down)) {
            if (info.IsKeyDown(Keys.RightShift)) {
                world.camera += new XYZ(0, 0, -delta);
            } else {
                world.camera += new XYZ(0, -delta);
            }
            UpdateExamine();
        } else if (info.IsKeyPressed(Keys.Left)) {
            world.camera += new XYZ(-delta, 0);
            UpdateExamine();
        } else if (info.IsKeyPressed(Keys.Right)) {
            world.camera += new XYZ(delta, 0);
            UpdateExamine();
        } else if (info.IsKeyPressed(Keys.Escape)) {
            world.camera = world.player.Position;

            Parent.IsFocused = true;
            Parent.Children.Remove(this);
        } else if (info.IsKeyPressed(Keys.Enter)) {
            selectAt(world.camera);
        } else {
            examineMenu.ListControls(info);
        }
        return true;
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        if (cursorVisible) {
            this.DebugInfo($"Draw Cursor @ ({Width / 2}, {Height / 2})");
            this.SetCellAppearance(Width / 2, Height / 2, cursor);
        }
        base.Render(delta);
        examineMenu?.Render(delta);
    }
    public override void Update(TimeSpan delta) {
        examineMenu?.Update(delta);
        cursorBlink.Update(delta.TotalSeconds);
        base.Update(delta);
    }
    public void UpdateExamine() {
        var ent = world.entities[world.camera];
        if (ent != null) {
            examineMenu = new(Width, Height, hint, ent.Select(e => new ListEntity(e)), select) {
                IsVisible = true,
            };
        }
    }
}
class MeleeMenu : Console {
    Player p;
    ListMenu<Entity> targetSelector;
    ListMenu<IItem> weaponSelector;
    public MeleeMenu(int Width, int Height, Player player, IEnumerable<Entity> targets) : base(Width, Height) {
        this.p = player;
        this.Transparent();

        targetSelector = new(Width, Height, "Select an entity to attack", targets.Select(t => new ListEntity(t)), target => {
            weaponSelector = new(Width, Height, "Select an item to attack with", player.Inventory.Select(i => new ListItem(i)), weapon => {
                if (!player.Actions.OfType<AttackAction>().Any()) {
                    player.Actions.Add(new AttackAction(player, target, weapon));
                    Close();
                } else {
                    //To do: prevent multiple attacks
                }
                return false;
            });
            return false;
        });
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
        ((Console)weaponSelector ?? targetSelector).Update(delta);
    }
    public override void Render(TimeSpan delta) {
        base.Render(delta);
        ((Console)weaponSelector ?? targetSelector).Render(delta);
    }
    public void Close() {

        Parent.IsFocused = true;
        Parent.Children.Remove(this);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Escape)) {
            if (weaponSelector != null) {
                weaponSelector = null;
            } else {
                Close();
            }
        } else {
            ((Console)weaponSelector ?? targetSelector).ProcessKeyboard(keyboard);
        }
        return base.ProcessKeyboard(keyboard);
    }
}
class HistoryMenu : Console {
    List<HistoryEntry> history;
    int bottomIndex;
    public HistoryMenu(int Width, int Height, List<HistoryEntry> history) : base(Width, Height) {
        this.history = history;
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        int x = 1;
        int y = Height - 5;
        for (int i = history.Count - bottomIndex - 1; i > -1; i--) {
            this.Print(x, y, history[i].Desc);
            y--;
        }
        base.Render(delta);
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            Parent.IsFocused = true;
            Parent.Children.Remove(this);
        }
        int delta = 0;
        if (info.IsKeyPressed(Keys.Up)) {
            delta = 1;
        }
        if (info.IsKeyPressed(Keys.PageUp)) {
            delta = 8;
        }
        if (info.IsKeyPressed(Keys.Down)) {
            delta = -1;
        }
        if (info.IsKeyPressed(Keys.PageDown)) {
            delta = -8;
        }
        if (delta != 0) {
            bottomIndex = Math.Max(0, Math.Min(bottomIndex + delta, history.Count - Height));
        }
        return base.ProcessKeyboard(info);
    }
}
