using Common;
using LibGamer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace RogueFrontier;
public class PauseScreen : IScene {
    public Mainframe playerMain;
    public SparkleFilter sparkle;
    public Sf sf;
    public bool visible;
    public int Width => sf.Width;
    public int Height => sf.Height;
	public Action<IScene> Go { get;set; }
	public Action<Sf> Draw { get;set; }
	public Action<SoundCtx> PlaySound { get; set; }

	public PauseScreen(Mainframe playerMain) {
        sf = new(playerMain.Width, playerMain.Height, Fonts.FONT_8x8);
		this.playerMain = playerMain;
        this.sparkle = new SparkleFilter(Width, Height);
        int x = 2;
        int y = 2;
        var fs = 3;
#if false
        Children.Add(new Label("[Paused]") { Position = new Point(x, y++), FontSize = fs });
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
#endif
    }
    public void Update(TimeSpan delta) {
        sparkle.Update();
    }
    public void Render(TimeSpan delta) {
        sf.Clear();

        var c = new SfList();//(playerMain.back.Surface, playerMain.viewport.sf);
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var source = c[x, y];
                var cg = source.Gray;
                sparkle.Filter(x, y, ref cg);
                sf.SetTile(x, y, cg);
            }
        }
        {
            int x = Width / 2 + 8;
            int y = 6;
            var controls = playerMain.Settings;
            foreach (var line in controls.GetString().Replace("\r", null).Split('\n')) {
                sf.Print(x, y++, line.PadRight(Width - x - 4), ABGR.White, ABGR.Black);
            }
        }
        Draw(sf);
    }
    public void HandleKey(KB kb) {
        if (kb.IsPress(KC.Escape)) {
            Continue();
        }
    }
    public void HandleMouse(HandState state) {
        
    }
    public void Continue() {
        visible = false;
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
        var endgame = ps.onDestroyed.set.Where(c => c is IScene).ToList();
        ps.onDestroyed.set.ExceptWith(endgame);
        Save();
        ps.onDestroyed.set.UnionWith(endgame);
        Continue();
    }
    public void SaveQuit() {
        //Remove PlayerMain events
        playerMain.playerShip.onDestroyed.set.RemoveWhere(d => d is IScene);
        Save();
        Quit();
    }
    public void DeleteQuit() {
        //Remove PlayerMain events
        playerMain.playerShip.onDestroyed.set.RemoveWhere(d => d is IScene);
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
        Go(new TitleScreen(playerMain.Width, playerMain.Height, new System(new Universe(w.types, new Rand()))));
    }
}
