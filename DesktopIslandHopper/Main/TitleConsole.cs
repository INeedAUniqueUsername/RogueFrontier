using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.Input;

namespace IslandHopper;

class TitleConsole : ControlsConsole {
    private double time = 0;

    private int titleLines;

    private int waterLevel;
    private const int waterHeight = 10;
    private const double waterLineInterval = 0.2;
    private const double waterLineSpeed = 36.0;
    private List<XY> waterLines = new List<XY>();

    private double waterTrailInterval = 1.0 / waterLineSpeed;
    private const double waterTrailLifespan = 8;
    private List<WaterTrail> waterTrails = new List<WaterTrail>();

    const string PLANE = @"<\_______ " + "\n"
                        + @" \\__>__O\";

    private const double planeInterval = 5;
    private const double planeSpeed = 10;
    private const int planeLevel = 18;
    private List<XY> planes = new List<XY>();

    /*
    const string player = @"/=\" + "\n"
                        + @"@&_";
    */
    const string PLAYER = @" *" + "\n"
                        + @"@&";
    private const double playerInterval = Math.PI / 2.5;
    private const double playerFallSpeed = 2;
    private List<XY> players = new List<XY>();

    private const double landSpeed = 10.0;
    private const double landSpawnTime = 2;
    List<XY> land = new List<XY>();
    bool[,] landGrid;

    private List<ITimer> timers;

    private Random Random = new Random();

    public TitleConsole(int width, int height) : base(width, height) {
        DefaultBackground = Color.Black;

        landGrid = new bool[Width, Height];
        var start = new Button(10, 1) {
            Position = new Point(5, 5),
            Text = "START",
        };
        start.Click += (btn, args) => {
            StartGame();
        };
        this.Controls.Add(start);

        var quit = new Button(10, 1) {
            Position = new Point(5, 6),
            Text = "QUIT",
        };
        quit.Click += (btn, args) => {
            ExitGame();
        };
        this.Controls.Add(quit);

        titleLines = 0;
        waterLevel = Height - 10;

        timers = new List<ITimer> {
                new TimerLimited(0.25, () => {
                    titleLines++;
                }, 25),
                new TimerLimited(5, () => {
                    timers = new List<ITimer> {
                        new Timer(waterLineInterval, () => {
                            waterLines.Add(new XY(0, waterLevel + Random.Next(waterHeight)));
                        }),
                        new Timer(waterTrailInterval, () => {
                            waterLines.ForEach(line => waterTrails.Add(new WaterTrail(line.x, line.y, waterTrailLifespan)));
                        }),
                        new Timer(planeInterval, () => {
                            planes.Add(new XY(0, planeLevel + Random.Next(10)));
                        }),
                        new Timer(playerInterval, () => {
                            planes.ForEach(plane => {
                                if (Main.InRange(plane.x + PLANE.LineLength(), Width/2, 30) && Random.Next(2) < 1)
                                    players.Add(plane.clone + new XY(8, 1));
                            });
                        }),
                        new TimerLimited(0.05, () => {
                            for(int i = 0; i < 25; i++)
                                land.Add(new XY(Width/2 + Random.Amplitude(15) + Random.Amplitude(15) + Random.Amplitude(15), planeLevel));
                        }, (int) (landSpawnTime / 0.05))
                    };
                }),
            };
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            StartGame();
        }
        if (keyboard.IsKeyPressed(Keys.Escape)) {
            ExitGame();
        }
        return base.ProcessKeyboard(keyboard);
    }
    public void ExitGame() {

    }
    public void StartGame() {
        //SadConsole.Game.Instance.Screen = new GameConsole(Width, Height) { IsFocused = true };
        SadConsole.Game.Instance.Screen = new ShopConsole(Width, Height) { IsFocused = true };
    }
    public override void Update(TimeSpan delta) {

        base.Update(delta);
        double sec = delta.TotalSeconds;
        time += sec;
        new List<ITimer>(timers).ForEach(timer => timer.Update(sec));

        waterLines.ForEach(line => line.x += sec * waterLineSpeed);
        waterTrails.ForEach(trail => trail.lifetime -= sec);
        planes.ForEach(plane => plane.x += sec * planeSpeed);
        players.ForEach(player => player.y += sec * playerFallSpeed);


        //Clear grid for collision checking
        Array.Clear(landGrid, 0, landGrid.Length);
        land.ForEach(p => landGrid[p.xi, p.yi] = true);

        //Make the land points fall towards sea level and settle
        land.ForEach(l => {
            if (l.yi < waterLevel) {
                if (!landGrid[l.xi, l.yi + 1]) {
                    l.y += sec * landSpeed;
                }
            }
        });


        waterLines.RemoveAll(line => line.x > Width - 1);
        waterTrails.RemoveAll(trail => trail.lifetime < 0);
        planes.RemoveAll(plane => plane.x + PLANE.LineLength() > Width - 1);
        players.RemoveAll(player => player.y > waterLevel - 5);

    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        PrintTitle();
        PrintWater();
        PrintPlanes();
        PrintLand();
        PrintPlayers();
        base.Render(delta);
    }
    private void PrintTitle() {
        var title = Properties.Resources.Title;
        title = title.Replace("\r\n", "\n");
        var lines = title.Split('\n');

        int titleX = Width / 2 - title.LineLength() / 2;
        int titleY = 0;

        int end = Math.Min(titleLines, lines.Length);
        for (int i = 0; i < end; i++) {
            this.Print(titleX, titleY + i, lines[i], Color.Gold);
        }
        //this.PrintLines(titleX, titleY, title, Color.Gold);
    }
    private void PrintWater() {

        foreach (var trail in waterTrails) {
            this.Print((int)trail.x, (int)trail.y, "=", new Color(Color.Blue, (int)(255 * trail.lifetime / trail.lifespan)));
        }
        foreach (var line in waterLines) {
            this.Print((int)line.x, (int)line.y, "=", Color.Blue);
        }
    }
    private void PrintPlanes() {
        foreach (var p in planes) {
            Color c = Color.Green;
            if (p.xi < 10) {
                c = new Color(c, 255 * p.xi / 10);
            } else if (p.xi > Width - (10 + PLANE.LineLength())) {
                c = new Color(c, 255 * (Width - p.xi - PLANE.LineLength()) / 10);
            }
            this.PrintLines(p.xi, p.yi, PLANE, c);

        }
    }
    private void PrintPlayers() {
        foreach (var p in players) {
            if (p.xi > 0) {
                Color c = Color.White;
                if (p.yi > waterLevel - 10) {
                    c = new Color(c, 255 * (waterLevel - p.yi) / 10);
                }
                this.PrintLines(p.xi, p.yi, PLAYER, c);
            }
        }
    }
    private void PrintLand() {
        land.ForEach(l => this.Print(l.xi, l.yi, "=", Color.Green));
    }
}
class WaterTrail {
    public double x, y;
    public double lifetime;
    public double lifespan { get; private set; }
    public WaterTrail(double x, double y, double lifetime) {
        this.x = x;
        this.y = y;
        this.lifetime = lifetime;
        lifespan = lifetime;
    }
    public void Update(double time) {
        lifetime -= time;
    }
}
interface ITimer {
    void Update(double passed);
}
class TimerLimited : ITimer {
    private Action action;
    private double interval;
    private double time;
    private int timesLeft;
    public TimerLimited(double interval, Action action, int timesLeft = 1) {
        this.action = action;
        this.interval = interval;
        this.time = interval;
        this.timesLeft = timesLeft;
    }
    public void Update(double passed) {
        if (timesLeft == 0)
            return;
        time -= passed;
        if (time < 0) {
            action.Invoke();
            timesLeft--;
            time = interval;
        }
    }
}
class Timer : ITimer {
    private Action action;
    private double interval;
    private double time;
    public Timer(double interval, Action action) {
        this.action = action;
        this.interval = interval;
        this.time = interval;
    }
    public void Update(double passed) {
        time -= passed;
        if (time < 0) {
            time = interval;
            action.Invoke();
        }
    }
}
class RandomTimer {
    private Action action;
    Func<double> intervalSource;
    private double time;
    public RandomTimer(Func<double> intervalSource, Action action) {
        this.action = action;
        this.intervalSource = intervalSource;
        this.time = intervalSource.Invoke();
    }
    public void Update(double passed) {
        time -= passed;
        if (time < 0) {
            time = intervalSource.Invoke();
            action.Invoke();
        }
    }
}
