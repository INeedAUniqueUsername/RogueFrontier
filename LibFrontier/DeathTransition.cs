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
        view = new Viewport(prev.sf.Width, prev.sf.Height, prev.monitor);
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
	Sf sf;
	IScene prev, next;
    int Width => sf.Width;
    int Height => sf.Height;
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
        this.sf = Sf.From(sf_prev);
        particles = new HashSet<Particle>();
        for (int y = 0; y < Height / 2; y++) {
            for (int x = 0; x < Width; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = -1,
                    destY = y,
                    delay = (1 + Math.Sin(Math.Sin(x) + Math.Sin(y))) * 3 / 2
                });
            }
        }
        for (int y = Height / 2; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = Height,
                    destY = y,
                    delay = (1 + Math.Sin(Math.Sin(x) + Math.Sin(y))) * 3 / 2
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
        if (time < 4) {
            return;
        } else if (time < 9) {
            foreach (var p in particles) {
                if (p.delay > 0) {
                    p.delay -= delta.TotalSeconds / 2;
                } else {
                    var offset = (p.destY - p.y);
                    p.y += Math.MinMagnitude(offset, Math.MaxMagnitude(Math.Sign(offset), offset * delta.TotalSeconds / 2));
                }
            }
        } else {
            Transition();
        }
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        var borderSize = Math.Max((time - 1) * 4, 0);
        var br = (byte)Math.Clamp((time - 1) * 255f, 0, 255);
        var borderColor = ABGR.RGB(br, br, br);
        for (int i = 0; i < borderSize; i++) {
            var d = 1d * i / borderSize;
            d = Math.Pow(d, 1.4);
            byte alpha = (byte)(255 - 255 * d);
            var c = ABGR.SetA(borderColor, alpha);
            var screenPerimeter = new Rect(i, i, Width - i * 2, Height - i * 2);
            foreach (var point in screenPerimeter.Perimeter) {
                //var back = this.GetBackground(point.X, point.Y).Premultiply();
                var (x, y) = point;
                sf.SetBack(x, y, c);
            }
        }
        byte l = 0;
        //int brightness = (int)Math.Min(255, 255 * Math.Max(0, time - 6) / 2);
        foreach (var p in particles) {

            if(sf.IsValid(p.x, (int)p.y)) {
                sf.SetTile(p.x, (int)p.y, new Tile(ABGR.Black, ABGR.RGBA(l, l, l, 255), ' '));
            }
        }
		prev.Render(delta);
		Draw(sf);
    }
}
