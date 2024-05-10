using ArchConsole;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using System.IO;
using Common;
using System.Linq;

namespace RogueFrontier;

public class PauseScreen : ISadSurface {
    public Mainframe playerMain;
    public SparkleFilter sparkle;

    public ScreenSurface Surface { get; private init; }
    public bool IsVisible { get => Surface.IsVisible; set => Surface.IsVisible = value; }

    public int Width => Surface.Width;
    public int Height => Surface.Height;
    public PauseScreen(Mainframe playerMain) {
        Surface = new(playerMain.Width, playerMain.Height);

		this.playerMain = playerMain;
        this.sparkle = new SparkleFilter(Width, Height);

        int x = 2;
        int y = 2;

        var fs = Surface.FontSize * 3;

        Surface.Children.Add(new Label("[Paused]") { Position = new Point(x, y++), FontSize = fs });
        y++;
        Surface.Children.Add(new LabelButton("Continue", Continue) { Position = new Point(x, y++), FontSize = fs });
        y++;
        y++;//this.Children.Add(new LabelButton("Save & Continue", SaveContinue) { Position = new Point(x, y++), FontSize = fs });
        y++;
        y++;//this.Children.Add(new LabelButton("Save & Quit", SaveQuit) { Position = new Point(x, y++), FontSize = fs });
        y++;
        y++;
        y++;
        Surface.Children.Add(new LabelButton("Self Destruct", SelfDestruct) { Position = new Point(x, y++), FontSize = fs });
        y++;
        Surface.Children.Add(new LabelButton("Delete & Quit", DeleteQuit) { Position = new Point(x, y++), FontSize = fs });
    }
    public void Update(TimeSpan delta) {
        sparkle.Update();
        Surface.Update(delta);
    }
    public void Render(TimeSpan delta) {
        Surface.Clear();

        var c = new ConsoleComposite(playerMain.back.Surface, playerMain.viewport.Surface);
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var source = c[x, y];
                var cg = source.Gray();
                sparkle.Filter(x, y, ref cg);
                Surface.SetCellAppearance(x, y, cg);
            }
        }

        {
            int x = Width / 2 + 8;
            int y = 6;
            var controls = playerMain.playerShip.person.Settings;
            foreach (var line in controls.GetString().Replace("\r", null).Split('\n')) {
                Surface.Print(x, y++, line.PadRight(Width - x - 4), Color.White, Color.Black);
            }
        }
        Surface.Render(delta);
    }
    public bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            Continue();
        }
        return Surface.ProcessKeyboard(info);
    }
    public void Continue() {
        Surface.IsVisible = false;
    }
    public void Save() {
        var ps = playerMain.playerShip;
        new LiveGame(playerMain.world, ps).Save();
    }
    public void Delete() {
        File.Delete(playerMain.playerShip.person.file);
    }
    public void SaveContinue() {
        //Temporarily PlayerMain events before saving
        var ps = playerMain.playerShip;
        var endgame = ps.onDestroyed.set.Where(c => c is IScreenObject).ToList();
        ps.onDestroyed.set.ExceptWith(endgame);
        Save();
        ps.onDestroyed.set.UnionWith(endgame);
        Continue();
    }
    public void SaveQuit() {
        //Remove PlayerMain events
        playerMain.playerShip.onDestroyed.set.RemoveWhere(d => d is IScreenObject);

        Save();
        Quit();
    }

    public void DeleteQuit() {
        //Remove PlayerMain events
        playerMain.playerShip.onDestroyed.set.RemoveWhere(d => d is IScreenObject);

        Delete();
        Quit();
    }

    public void SelfDestruct() {
        
        
        var p = playerMain.playerShip;
        p.ship.active = false;
        var items = p.cargo
            .Concat(p.devices.Installed.Select(d => d.source).Where(i => i != null))
            .Concat((p.hull as LayeredArmor)?.layers.Select(l => l.source)??new List<Item>());
        Wreck w = new Wreck(p, items);
        playerMain.world.AddEntity(w);

        playerMain.world.RemoveEntity(p);
        playerMain.OnPlayerDestroyed("Self destructed", w);
    }
    public void Quit() {
        var w = playerMain.world;
        GameHost.Instance.Screen = new TitleScreen(playerMain.Width, playerMain.Height, new System(new Universe(w.types, new Rand()))) { IsFocused = true };
    }
}
