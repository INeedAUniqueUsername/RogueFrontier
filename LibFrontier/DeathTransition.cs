using Common;
using LibGamer;
using System;
using System.Collections.Generic;

namespace RogueFrontier;

public class DeathPause : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Mainframe prev;
    DeathTransition next;
    //Sf sf;
    //int Width => sf.Width;
    //int Height => sf.Height;
    public double time;
    public bool done;
    Viewport view;
	public DeathPause(Mainframe prev, DeathTransition next) {
        this.prev = prev;
        this.next = next;
        //this.sf = new Sf(prev.sf.Width, prev.sf.Height, Fonts.FONT_8x8);
        view = new Viewport(prev.sf.GridWidth, prev.sf.GridHeight, prev.monitor);
        view.Update(new());
        view.Draw += sf => Draw?.Invoke(sf);

    }
    public void Update(TimeSpan delta) {
        time += delta.TotalSeconds / 4;
        if (time < 2 && !done) {
            return;
        }
        Go(next);
    }
    public void Render(TimeSpan delta) {
        view.Render(delta);
    }
}
public class DeathTransition : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	Sf sf_vignette;
    Sf sf_particles;

	IScene prev, next;
    int Width => sf_vignette.GridWidth;
    int Height => sf_vignette.GridHeight;
	public class Particle {
        public int x, destY;
        public double y, delay;
    }
    HashSet<Particle> particles;
    double time;
    public DeathTransition(IScene prev, Sf sf_prev, IScene next) {
        this.prev = prev;
        this.next = next;
        prev.Draw += sf => Draw?.Invoke(sf);
        this.sf_vignette = Sf.From(sf_prev);

        sf_particles = Sf.From(sf_prev);
        particles = new HashSet<Particle>();
        for (int y = 0; y < Height / 2; y++) {
            for (int x = 0; x < Width; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = -1,
                    destY = y,
                    delay = (1 + Sin(Sin(x) + Sin(y)))
                });
            }
        }
        for (int y = Height / 2; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = Height,
                    destY = y,
                    delay = (1 + Sin(Sin(x) + Sin(y)))
                });
            }
        }
    }
    public void HandleKey(KB keyboard) {
        if (keyboard.IsPress(KC.Enter)) {
            Transition();
        }
    }
    public void Transition() {
        Go(next);
    }
    public void Update(TimeSpan delta) {
        time += delta.TotalSeconds / 2;
        prev.Update(delta);
        if (time < 2) {
            return;
        } else if (time < 7) {
            foreach (var p in particles) {
                if (p.delay > 0) {
                    p.delay -= delta.TotalSeconds / 2;
                } else {
                    var offset = (p.destY - p.y);
                    p.y += MinMagnitude(offset, MaxMagnitude(Sign(offset), offset * delta.TotalSeconds / 2));
                }
            }
        } else {
            Transition();
        }
    }
    public void Render(TimeSpan delta) {

		prev.Render(delta);

		sf_vignette.Clear();
        var borderSize = Max((time - 0.5) * 8, 0);
        var br = (byte)Clamp((time - 0.5) * 255f, 0, 255);
        var borderColor = ABGR.RGB(br, br, br);
        for(var i = 0; i < borderSize; i++) {
            var d = 1d * i / borderSize;
            d = Pow(d, 1.4);
            byte alpha = (byte)(255 - 255 * d);
            var c = ABGR.SetA(borderColor, alpha);
            var screenPerimeter = new Rect(i, i, Width - i * 2, Height - i * 2);
            foreach (var(x,y) in screenPerimeter.Perimeter) {
                //var back = this.GetBackground(point.X, point.Y).Premultiply();
                sf_vignette.SetBack(x, y, c);
            }
        }
		Draw(sf_vignette);
        sf_particles.Clear();
		byte l = 0;
        //int brightness = (int)Math.Min(255, 255 * Math.Max(0, time - 6) / 2);
        foreach (var p in particles) {
            if(sf_particles.IsValid(p.x, (int)p.y)) {
                var alpha = (byte)Main.Lerp(Abs(p.y - p.destY), 0, 20, 255, 0, 1);
				sf_particles.SetTile(p.x, (int)p.y, new Tile(ABGR.Black, ABGR.SetA(ABGR.Black, alpha), ' '));
            }
        }
        Draw(sf_particles);

    }
}
