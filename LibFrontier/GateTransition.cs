using LibGamer;
using RogueFrontier;
using System;
using System.Collections.Generic;
using System.Linq;
namespace RogueFrontier;

public class GateTransition : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	Viewport prevView, nextView;
    double amount;
    Rect rect;
    public Action next;

    Sf compositeView;int Width => compositeView.Width;int Height => compositeView.Height;

    class Particle {
        public int lifetime;
        public (int X, int Y) pos;
        public Particle(int lifetime, (int X, int Y) pos) {
            this.lifetime = lifetime;
            this.pos = pos;
        }
    }
    private List<Particle> particles = new();
	public GateTransition(Viewport prevView, Viewport nextView, Action next) {
        this.prevView = prevView;
        this.nextView = nextView;
        this.compositeView = Sf.From(prevView.sf);
        rect = new Rect(Width, Height, 0, 0);
        this.next = next;
    }
    public void HandleKey(KB kb) {
        if (kb.IsPress(KC.Enter)) {
            next();
        }
    }
    public void Update(TimeSpan delta) {
        prevView.Update(delta);
        amount += delta.TotalSeconds * 1;

        if (amount < 1) {
            var w = (int)(amount * Width);
            var h = (int)(amount * Height);
			rect = new Rect(Width / 2 - w/2, Height / 2 - h/2, w, h);
            //particles.AddRange(rect.Perimeter.Select(p => new Particle(0, p)));
            //particles.ForEach(p => p.lifetime--);
            //particles.RemoveAll(p => p.lifetime < 1);
        } else if(particles.Any()) {
            particles.ForEach(p => p.lifetime--);
            particles.RemoveAll(p => p.lifetime < 1);
        } else {
            next();
        }
    }
    public void Render(TimeSpan delta) {
        compositeView.Clear();
        var particleLayer = new Sf(Width, Height, Fonts.FONT_8x8);
        particles.ForEach(p => {
            particleLayer.Back[p.pos] = ABGR.RGBA(255, 255, 255, (byte)(p.lifetime * 255 / 15));
        });
        var compositeBack = new Sf(Width, Height, Fonts.FONT_8x8);
        if (nextView != null) {
            BackdropConsole prevBack = new(prevView);
            BackdropConsole nextBack = new(nextView);
            foreach (var y in (0..Height)) {
                foreach (var x in (0..Width)) {
                    var p = (x, y);
                    (var view, var back) = rect.Contains(p) ? (nextView, nextBack) : (prevView, prevBack);
                    compositeBack.Tile[x, y] = back.GetTile(x, y);
                    //var g = (rect.Contains(p) ? next : prev).GetCellAppearance(x, y);
                    compositeView.Tile[x, Height - y - 1] = view.GetTile(x, y);
                }
            }
        } else {
            var prevBack = new BackdropConsole(prevView);
            foreach (var y in (0..Height)) {
                foreach (var x in (0..Width)) {
                    var p = (x, y);
                    if (rect.Contains(p)) {
                        compositeView.Tile[x, Height - y - 1] = (ABGR.Black, ABGR.Black, 0);
                    } else {
                        (var view, var back) = (prevView, prevBack);
                        compositeBack.Tile[x, y] = back.GetTile(x, y);
                        compositeView.Tile[x, Height - y - 1] = view.GetTile(x, y);
                    }
                }
            }
        }
        Draw?.Invoke(compositeBack);
        Draw?.Invoke(compositeView);
        Draw?.Invoke(particleLayer);
    }
}
