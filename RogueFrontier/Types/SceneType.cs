using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using Console = SadConsole.Console;
using ArchConsole;
using ASECII;
using System.IO;
using SFML.Audio;
namespace RogueFrontier;

public static class SNav {

    public static NavChoice DockArmorRepair(PlayerShip p, int price) =>
        DockArmorReplacement(p, a => price);
    public static NavChoice DockArmorRepair(PlayerShip p, Func<Armor, int> GetPrice) =>
        new("Service: Armor Repair", prev => SMenu.DockArmorRepair(prev, p, GetPrice, null));
    public static NavChoice DockArmorReplacement(PlayerShip p, int price) =>
        DockArmorReplacement(p, a => price);
    public static NavChoice DockArmorReplacement(PlayerShip p, Func<Armor, int> GetPrice) =>
        new("Service: Armor Replacement", prev => SMenu.DockArmorReplacement(prev, p, GetPrice, null));

    public static NavChoice DockDeviceInstall(PlayerShip p, Func<Device, int> GetPrice) =>
        new("Service: Device Install", prev => SMenu.DockDeviceInstall(prev, p, GetPrice, null));

    public static NavChoice DockDeviceRemoval(PlayerShip p, Func<Device, int> GetPrice) =>
        new("Service: Device Removal", prev => SMenu.DockDeviceRemoval(prev, p, GetPrice, null));
}
public enum NavFlags : long {
    ESC = 0b1,
    ENTER = 0b10
}
public record NavChoice(char key, string name, Func<ScreenSurface, ScreenSurface> next, NavFlags flags = 0, bool enabled = true) {
    public NavChoice() : this('\0', "", null, 0) { }
    public NavChoice(string name) : this(name, null, 0) { }
    public NavChoice(string name, Func<ScreenSurface, ScreenSurface> next, NavFlags flags = 0, bool enabled = true) : this(name.FirstOrDefault(char.IsLetterOrDigit), name, next, flags, enabled) { }
}
public static partial class SMenu {
    public static Dictionary<(int, int), U> Normalize<U>(this Dictionary<(int, int), U> d) {
        int left = int.MaxValue;
        int top = int.MaxValue;
        foreach ((int x, int y) p in d.Keys) {
            left = Math.Min(left, p.x);
            top = Math.Min(top, p.y);
        }
        return d.Translate(new Point(-left, -top));
    }
    public static Dictionary<(int, int), ColoredGlyph> LoadImage(string file) {
        var img = ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(file));

        var result = new Dictionary<(int, int), ColoredGlyph>();
        foreach ((var p, var t) in img) {
            result[p] = t.cg;
        }
        return result;
    }
    public static Dictionary<(int, int), ColoredGlyph> ToImage(this string[] image, Color tint) {
        var result = new Dictionary<(int, int), ColoredGlyph>();
        for (int y = 0; y < image.Length; y++) {
            var line = image[y];
            for (int x = 0; x < line.Length; x++) {
                result[(x, y * 2)] = new ColoredGlyph(tint, Color.Black, line[x]);
                result[(x, y * 2 + 1)] = new ColoredGlyph(tint, Color.Black, line[x]);
            }
        }
        return result;
    }
    public static Dictionary<(int, int), U> Translate<U>(this Dictionary<(int, int), U> image, Point translate) {
        var result = new Dictionary<(int, int), U>();
        foreach (((var x, var y), var u) in image) {
            result[(x + translate.X, y + translate.Y)] = u;
        }
        return result;
    }
    public static Dictionary<(int, int), U> CenterVertical<U>(this Dictionary<(int, int), U> image, Console c, int deltaX = 0) {
        var result = new Dictionary<(int, int), U>();
        int deltaY = (c.Height - (image.Max(pair => pair.Key.Item2) - image.Min(pair => pair.Key.Item2))) / 2;
        foreach (((var x, var y), var u) in image) {
            result[(x + deltaX, y + deltaY)] = u;
        }
        return result;
    }
    public static Dictionary<(int, int), U> Flatten<U>(params Dictionary<(int, int), U>[] images) {
        var result = new Dictionary<(int x, int y), U>();
        foreach (var image in images) {
            foreach (((var x, var y), var u) in image) {
                result[(x, y)] = u;
            }
        }
        return result;
    }
    public static void ProcessMouseTree(this IScreenObject root, Mouse m) {
        List<IScreenObject> s = new List<IScreenObject>();
        AddChildren(root);
        void AddChildren(IScreenObject parent) {
            s.Add(parent);
            foreach (var c in parent.Children) {
                AddChildren(c);
            }
        }
        foreach (var c in s) {
            c.ProcessMouse(new MouseScreenObjectState(c, m));
        }
    }
    public static void RenderBackground(this ScreenSurface c) {
        c.Surface.Fill(Color.Black, Color.Black.SetAlpha(128), ' ');
        /*
        var back = new Console(c.Width, c.Height);

        foreach (var point in new Rectangle(0, 0, c.Width, c.Height).Positions()) {

            var h = point.X % 4 == 0;
            var v = point.Y % 4 == 0;

            var f = new Color(255, 255, 255, 255 * 4 / 8);

            if (h && v) {
                f = new Color(255, 255, 255, 255 * 6 / 8);
            } else if (h || v) {
                f = new Color(255, 255, 255, 255 * 5 / 8);
            }
            back.SetCellAppearance(point.X, point.Y, new ColoredGlyph(f, Color.Black.SetAlpha(102), '.'));
        }
        back.Render(new TimeSpan());
        */
    }
}
public class HeroImageDisplay : Console {
    double time;
    string[] heroImage;
    Color tint;
    public HeroImageDisplay(Console prev, string[] heroImage, Color tint) : base(prev.Width, prev.Height) {
        this.heroImage = heroImage;
        this.tint = tint;
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    public override void Render(TimeSpan delta) {
        int width = heroImage.Max(line => line.Length);
        int height = heroImage.Length;
        int x = 8;
        int y = (Height - height * 2) / 2;
        byte GetAlpha(int x, int y) {
            return (byte)(Math.Sin(time * 1.5 + Math.Sin(x) * 5 + Math.Sin(y) * 5) * 25 + 230);
        }
        int lineY = 0;
        foreach (var line in heroImage) {
            void DrawLine() {
                for (int lineX = 0; lineX < line.Length; lineX++) {
                    var color = tint.SetAlpha(GetAlpha(lineX, lineY));
                    this.SetCellAppearance(x + lineX, y + lineY, new ColoredGlyph(color, Color.Black, line[lineX]));
                }
                lineY++;
            }
            DrawLine();
            DrawLine();
        }
        base.Render(delta);
    }
}
public class Dialog : Console {
    public ColoredString desc;
    public bool charging;
    public int descIndex;
    public int ticks;
    List<NavChoice> navigation;
    public int navIndex = 0;
    double[] charge;
    public Dictionary<(int, int), ColoredGlyph> background = new();
    Dictionary<char, int> keyMap = new();
    bool prevEscape;
    bool allowEnter;
    bool prevEnter;
    bool enter;
    int escapeIndex;
    int descX => Width / 2 - 12;
    int descY => 8;
    public static double maxCharge = 0.5;
    
    private Sound button_press = new(new SoundBuffer("Assets/sounds/button_press.wav")) {
        Volume = 33
    };
    public Dialog(ScreenSurface prev, string desc, List<NavChoice> navigation) : base(prev.Surface.Width, prev.Surface.Height) {
        this.desc = new(desc.Replace("\r", null));

        var quoted = false;
        var highlight = false;
        foreach(var c in this.desc) {


            ((Action)(c.GlyphCharacter switch {
                '"' => () => {
                    quoted = !quoted;
                    c.Foreground = Color.LightBlue;
                },
                '[' or ']' => () => {
                    highlight = !highlight;
                    c.Foreground = Color.Yellow;
                },
                _ => () => {
                    if(c.Foreground == Color.White) {
                        c.Foreground =
                            highlight ?
                                Color.Yellow :
                            quoted ?
                                Color.LightBlue :
                            Color.LightYellow;
                    }
                }
            }))?.Invoke();
        }
        navigation.RemoveAll(s => s == null);
        this.navigation = navigation;
        charge = new double[navigation.Count];
        descIndex = 0;
        ticks = 0;

        escapeIndex = navigation.FindIndex(o => (o.flags & NavFlags.ESC) != 0);
        if (escapeIndex == -1) escapeIndex = navigation.Count-1;

        UseMouse = true;
        UseKeyboard = true;
    }
    int deltaIndex = 2;
    public override void Update(TimeSpan delta) {
        bool f = IsFocused;
        ticks++;
        if (ticks % 2*deltaIndex == 0) {
            if (descIndex < desc.Length - 1) {
                deltaIndex = 1;
                while(descIndex < desc.Length - 1) {
                    descIndex++;
                    deltaIndex++;
                    if(desc[descIndex].GlyphCharacter == ' ') {
                        break;
                    }
                }
            } else if (descIndex < desc.Length) {
                descIndex++;
                int x = descX,
                    y = descY + desc.Count(c => c.GlyphCharacter == '\n') + 3,
                    i = 0;
                foreach (var option in navigation) {
                    int index = i;
                    keyMap[char.ToUpper(option.key)] = index;
                    Children.Add(new LabelButton(option.name) {
                        Position = new(x, y++),
                        leftHold = () => {
                            navIndex = index;
                            charging = true;
                            enter = true;
                        },
                        enabled = option.enabled
                    });
                    i++;
                }
            }
        }
        if (charging && navIndex != -1 && navigation[navIndex].enabled) {
            ref double c = ref charge[navIndex];
            if (c < maxCharge) {
                c += delta.TotalSeconds;
            }
        }
        if (prevEnter && !enter) {
            ref double c = ref charge[navIndex];
            if (c >= maxCharge) {
                //Make sure we aren't sent back to the screen again
                prevEnter = false;
                Transition(navigation[navIndex].next?.Invoke(this));
                c = maxCharge - 0.01;
            }
        } else {
            prevEnter = enter;
        }
        for (int i = 0; i < charge.Length; i++) {
            if(i == navIndex && charging) {
                continue;
            }
            ref double c = ref charge[i];
            if (c > 0) {
                c -= delta.TotalSeconds;
            }
        }
        base.Update(delta);
    }
    public void Transition(ScreenSurface next) {
        button_press.Play();
        var p = Parent;
        var c = Parent.Children;
        c.Remove(this);
        if (next != null) {
            c.Add(next);
            next.Render(new());
            next.IsFocused = true;
        } else {
            p.IsFocused = true;
        }
    }
    public override void Render(TimeSpan delta) {
        this.RenderBackground();
        if(background != null) {
            foreach (((var px, var py), var cg) in background) {
                this.SetCellAppearance(px, py, cg);
            }
        }
        int left = descX;
        int top = descY;
        int y = top;
        int x = left;
        for (int i = 0; i < descIndex; i++) {
            switch (desc[i].GlyphCharacter) {
                case '\n':
                    x = left;
                    y++;
                    break;
                default:
                    this.SetCellAppearance(x, y, desc[i]);
                    x++;
                    break;
            }
        }
        if (descIndex < desc.Length) {
            this.SetCellAppearance(x, y, new ColoredGlyph(Color.LightBlue, Color.Black, '>'));
        } else {
            var barLength = 4;
            var arrow = $"{new string('-', barLength - 1)}>";


            x = descX - barLength;
            y = descY + desc.Count(c => c.GlyphCharacter == '\n') + 3;
            foreach(var (c, i) in charge.Select((c, i) => (c, i))) {
                this.Print(x, y + i, new ColoredString(arrow.Substring(0, (int)(barLength * Math.Clamp(c / maxCharge, 0, 1))), Color.Gray, Color.Black));
            }
            if (navIndex > -1) {
                this.Print(x, y + navIndex, new ColoredString(arrow, Color.Gray, Color.Black));
                var ch = charge[navIndex];
                this.Print(x, y + navIndex, new ColoredString(arrow.Substring(0, (int)(barLength * Math.Clamp(ch / maxCharge, 0, 1))), ch < maxCharge ? Color.Yellow : Color.Orange, Color.Black));
            }
        }
        base.Render(delta);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {

        charging = false;
        if (keyboard.IsKeyPressed(Keys.Escape) || (prevEscape && keyboard.IsKeyDown(Keys.Escape))) {
            if (!prevEscape) {
                button_press.Play();
            }
            navIndex = escapeIndex;
            charging = true;
            enter = true;
            prevEscape = true;
        } else {
            prevEscape = false;


            enter = keyboard.IsKeyDown(Keys.Enter);


            if (enter) {

                if (!prevEnter) {
                    button_press.Play();
                }

                if (descIndex < desc.Length - 1) {
                    descIndex = desc.Length - 1;
                    allowEnter = false;
                } else if (allowEnter) {
                    charging = true;
                }
            } else if (allowEnter) {
                if (keyboard.IsKeyDown(Keys.Right)) {
                    enter = true;
                    charging = true;
                }
            } else {
                allowEnter = true;
            }
            foreach (var c in keyboard.KeysDown.Select(k => k.Character).Where(c => char.IsLetterOrDigit(c)).Select(c => char.ToUpper(c))) {
                if (keyMap.TryGetValue(c, out int index)) {
                    if (!prevEnter) {
                        button_press.Play();
                    }

                    navIndex = index;
                    charging = true;
                    enter = true;
                }
            }
            if (keyboard.IsKeyPressed(Keys.Up)) {
                button_press.Play();
                navIndex = (navIndex - 1 + navigation.Count) % navigation.Count;
            }
            if (keyboard.IsKeyPressed(Keys.Down)) {
                button_press.Play();
                navIndex = (navIndex + 1) % navigation.Count;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        foreach (var c in Children) {
            c.ProcessMouse(state);
        }
        if (state.Mouse.LeftButtonDown) {
            if (descIndex < desc.Length - 1) {
                descIndex = desc.Length - 1;
                allowEnter = false;
            }
        }
        return base.ProcessMouse(state);
    }
}