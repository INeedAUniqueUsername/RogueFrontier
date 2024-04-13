using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using Con = SadConsole.ScreenSurface;
using SadConsole.Input;
using ASECII;
using Common;

namespace RogueFrontier;

public class DeathPause : Con {
    Mainframe prev;
    DeathTransition next;

    public double time;
    public bool done;
    Viewport view;
    public DeathPause(Mainframe prev, DeathTransition next) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;
        this.next = next;
        view = new Viewport(prev.monitor);
        view.Update(new());
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds / 4;
        if (time < 2 && !done) {
            return;
        }
        SadConsole.Game.Instance.Screen = next;
        next.IsFocused = true;

        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        view.Render(delta);
        base.Render(delta);
    }
}
public class DeathTransition : Con
{

	Con Surface, prev, next;
    int Width => Surface.Width;
    int Height => Surface.Height;
    public class Particle {
        public int x, destY;
        public double y, delay;
    }
    HashSet<Particle> particles;
    double time;
    public DeathTransition(Con prev, Con next) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;
        this.next = next;
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
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        prev.Render(delta);
        base.Render(delta);
        Surface.Clear();

        var borderSize = Math.Max((time - 1) * 4, 0);

        var br = (int)Math.Clamp((time - 1) * 255f, 0, 255);
        var borderColor = new Color(br, br, br);
        for (int i = 0; i < borderSize; i++) {
            var d = 1d * i / borderSize;
            d = Math.Pow(d, 1.4);
            byte alpha = (byte)(255 - 255 * d);
            var c = borderColor.SetAlpha(alpha);
            var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
            foreach (var point in screenPerimeter.PerimeterPositions()) {
                //var back = this.GetBackground(point.X, point.Y).Premultiply();
                var (x, y) = point;
                Surface.SetBackground(x, y, c);
            }
        }
        int brightness = 0;
        //int brightness = (int)Math.Min(255, 255 * Math.Max(0, time - 6) / 2);
        foreach (var p in particles) {
            Surface.SetCellAppearance(p.x, (int)p.y, new ColoredGlyph(Color.Black, new(brightness, brightness, brightness, 255), ' '));
        }
    }
}
