using SadConsole;
using Console = SadConsole.Console;
using System.Collections.Generic;
using SadRogue.Primitives;
using System.Linq;
using SadConsole.Input;
using System;
namespace Life;

public class Program {
    public static int TICKS_PER_SECOND = 60;
    static Program() {
        Height = 60;
        Width = Height * 5 / 3;
    }
    public static int Width, Height;
    public static string font = "IBMCGA.font";
    static void Main(string[] args) {
        // Setup the engine and create the main window.
        Game.Create(Width, Height, font, (sender, e)=> Start());
        // Hook the start event so we can add consoles to the system.
        //Game.Instance.OnStart = Start;
        // Start the game.
        Game.Instance.Run();
        Game.Instance.Dispose();
    }
    public static void Start() {
        Game.Instance.Screen = new Life(Width, Height) { IsFocused = true };
    }
}
public class Grid {
    public HashSet<(int, int)> live = new HashSet<(int, int)>();
    public bool this[int x, int y] {
        get => live.Contains((x, y));
        set {
            if (value) {
                live.Add((x, y));
            } else {
                live.Remove((x, y));
            }
        }
    }
}
public class Life : Console {
    (int x, int y) camera;
    Grid cells, next;
    Dictionary<(int x, int y), int> sinceDeath;
    Dictionary<(int x, int y), int> deathTick;
    bool autoUpdate;
    int ticks;
    public Life(int Width, int Height) : base(Width, Height) {
        camera = (0, 0);
        cells = new Grid();
        next = new Grid();
        sinceDeath = new Dictionary<(int x, int y), int>();
        deathTick = new Dictionary<(int x, int y), int>();
        UseKeyboard = true;
    }
    public void UpdateLife() {
        cells.live.Clear();
        cells.live.UnionWith(next.live);
        UpdateNext();
    }
    public void UpdateNext() {
        HashSet<(int, int)> points = new HashSet<(int, int)>();
        foreach ((int x, int y) in cells.live) {
            sinceDeath[(x, y)] = 0;
            deathTick[(x, y)] = ticks;

            (int, int)[] add = {
                    (x - 1, y + 1), (x, y + 1), (x + 1, y + 1),
                    (x - 1, y), (x, y), (x + 1, y),
                    (x - 1, y - 1), (x, y - 1), (x + 1, y - 1),
                };
            points.UnionWith(add);
        }

        foreach (var p in sinceDeath.Keys) {
            sinceDeath[p]++;
        }

        foreach ((int x, int y) in points) {
            int c = Count(
                    cells[x - 1, y + 1], cells[x, y + 1], cells[x + 1, y + 1],
                    cells[x - 1, y], cells[x + 1, y],
                    cells[x - 1, y - 1], cells[x, y - 1], cells[x + 1, y - 1]);
            next[x, y] = (c == 3) || (cells[x, y] && c >= 2 && c <= 3);
        }
    }
    public override void Update(TimeSpan delta) {
        if (autoUpdate) {
            ticks++;
            if (ticks % 3 == 0) {
                UpdateLife();
            }
        }
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        foreach (var x in Enumerable.Range(0, Width)) {
            foreach (var y in Enumerable.Range(0, Height)) {
                if (cells[x + camera.x, y + camera.y]) {
                    this.SetCellAppearance(x, y, new ColoredGlyph(
                        Color.White,
                        next[x + camera.x, y + camera.y] ? Color.DarkSlateGray : Color.Black, '*'));
                } else {
                    void set(Color b) {
                        this.SetCellAppearance(x, y, new ColoredGlyph(
                            Color.White,
                            b,
                            ' '));
                    }
                    if (next[x + camera.x, y + camera.y]) {
                        set(Color.DarkSlateGray);
                    } else if (sinceDeath.TryGetValue((x + camera.x, y + camera.y), out var since)) {
                        var tick = deathTick[(x + camera.x, y + camera.y)];
                        set(Color.FromHSL(tick, 0.2f, Math.Min(1f, since / 300f)));
                    } else {
                        set(Color.Black);
                    }
                }
            }
        }
        base.Render(delta);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (!autoUpdate) {
            if (keyboard.IsKeyPressed(Keys.OemPeriod)) {
                UpdateLife();
            }
        }
        if (keyboard.IsKeyPressed(Keys.Space)) {
            autoUpdate = !autoUpdate;
        }

        if (keyboard.IsKeyDown(Keys.Down)) {
            camera.y++;
        }
        if (keyboard.IsKeyDown(Keys.Up)) {
            camera.y--;
        }
        if (keyboard.IsKeyDown(Keys.Left)) {
            camera.x--;
        }
        if (keyboard.IsKeyDown(Keys.Right)) {
            camera.x++;
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        if (!autoUpdate) {
            if (state.Mouse.IsOnScreen && state.Mouse.LeftClicked) {
                var p = state.SurfaceCellPosition + camera;
                cells[p.X, p.Y] = !cells[p.X, p.Y];
                UpdateNext();
            }
        }

        return base.ProcessMouse(state);
    }
    public int Count(params bool[] bb) => bb.Count(b=>b);
}
