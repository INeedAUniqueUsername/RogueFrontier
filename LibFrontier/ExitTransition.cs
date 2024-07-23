
using System;
using System.Linq;
using System.Collections.Generic;
using LibGamer;
namespace RogueFrontier;

public class ExitTransition : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	IScene prev;
    Action next;
    Sf sf_prev;
    Sf sf;
    public class Particle {
        public int x, destY;
        public double y, delay;
    }
    HashSet<Particle> particles;
    double time;


	public ExitTransition(IScene prev, Sf sf_prev, Action next) {
        this.prev = prev;
        this.sf_prev = sf_prev;
        this.next = next;
        this.sf = new Sf(sf_prev.GridWidth, sf_prev.GridHeight, Fonts.FONT_8x8);
        InitParticles();
    }
    public void InitParticles() {
        particles = new HashSet<Particle>();
        for (int y = 0; y < sf.GridHeight / 2; y++) {
            for (int x = 0; x < sf.GridWidth; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = -1,
                    destY = y,
                    delay = (1 + Sin(Sin(x) + Sin(y))) * 3 / 2
                });
            }
        }
        for (int y = sf.GridHeight / 2; y < sf.GridHeight; y++) {
            for (int x = 0; x < sf.GridWidth; x++) {
                particles.Add(new Particle() {
                    x = x,
                    y = sf.GridHeight,
                    destY = y,
                    delay = (1 + Sin(Sin(x) + Sin(y))) * 3 / 2
                });
            }
        }
    }
    public void HandleKey(KB kb) {
        if (kb[KC.Enter] == KS.Press) {
            Transition();
        }
    }
    public void Transition() {
        next();
    }
    public void Update(TimeSpan delta) {
        prev.Update(delta);
        time += delta.TotalSeconds / 2;
        if (time < 2) {
            return;
        } else if (time < 6) {
            foreach (var p in particles) {
                if (p.delay > 0) {
                    p.delay -= delta.TotalSeconds * 2 / 3;
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
        Draw(sf_prev);
        sf.Clear();
        foreach (var p in particles) {
            sf.SetTile(p.x, (int)p.y, new Tile(ABGR.Black, ABGR.Black, ' '));
        }
        Draw(sf);
    }
}
