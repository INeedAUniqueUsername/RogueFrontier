using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using Console = SadConsole.Console;
using SadConsole.Input;

namespace RogueFrontier;

public class ExitTransition : Console {
    ScreenSurface prev, next;
    public class Particle {
        public int x, destY;
        public double y, delay;
    }
    HashSet<Particle> particles;
    double time;
    public ExitTransition(ScreenSurface prev, ScreenSurface next) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;
        this.next = next;
        InitParticles();
    }
    public void InitParticles() {
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
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            Transition();
        }
        return base.ProcessKeyboard(keyboard);
    }
    public void Transition() {
        SadConsole.Game.Instance.Screen = next;
        next.IsFocused = true;
    }
    public override void Update(TimeSpan delta) {
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
                    p.y += Math.MinMagnitude(offset, Math.MaxMagnitude(Math.Sign(offset), offset * delta.TotalSeconds / 2));
                }
            }
        } else {
            Transition();
        }
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        prev.Render(delta);
        base.Render(delta);
        this.Clear();
        foreach (var p in particles) {
            this.SetCellAppearance(p.x, (int)p.y, new ColoredGlyph(Color.Black, Color.Black, ' '));
        }
    }
}
