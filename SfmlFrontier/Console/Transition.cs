
using SadConsole;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using Common;
using SadConsole.Input;
using LibGamer;
using SfmlFrontier;

namespace RogueFrontier;
public class TitleSlideOpening : IScene {
    public Sf sf { get; }
    public int Width => sf.Width;
    public int Height => sf.Height;
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }

	public IScene next;
    public Sf nextSf;
    int x = 0;
    double time = 0;
    double interval;
    bool fast;
    bool updateNext;


    public TitleSlideOpening(IScene next, Sf nextSf, bool updateNext = true) {
        this.sf = new Sf(nextSf.Width, nextSf.Height);
        x = nextSf.Width;
        this.next = next;
        this.nextSf = nextSf;
        this.updateNext = updateNext;
        interval = 4f / Width;

        //Draw one frame now so that we don't cut out for one frame
        next.Update(new TimeSpan());
        Render(new TimeSpan());
    }
    public void Update(TimeSpan delta) {
        if (updateNext) {
            next.Update(delta);
        }
        if (fast) {
            x -= (int)(Width * delta.TotalSeconds);
        }
        time += delta.TotalSeconds;
        while (time > interval) {
            time -= interval;
            if (x > -16) {
                x--;
            } else {
                Go(next);
                return;
            }
        }
    }
    public void Render(TimeSpan delta) {
        next.Render(delta);
        sf.Clear();
        var blank = new ColoredGlyph(Color.Black, Color.Black);
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < this.x; x++) {
                sf.SetTile(x, y, new Tile(0, 0, 0));
            }
            for (int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {

                var glyph = nextSf.GetGlyph(x, y);
                var value = (byte)(255 - 255 / 16 * (x - this.x));

                var fore = nextSf.GetFront(x, y);
                fore = ABGR.Blend(ABGR.Premultiply(fore), ABGR.SetA(ABGR.Black, value));

                var back = nextSf.GetBack(x, y);
                back = ABGR.Blend(ABGR.Premultiply(back), ABGR.SetA(ABGR.Black, value));

                sf.SetTile(x, y, new Tile(fore, back, glyph));
            }
        }
    }
    public void HandleKey(KB kb) {
        if (kb[[KC.Enter, KC.Escape]].Contains(KS.Pressed)) {
            fast = true;
        }
        return;
    }
}
public class TitleSlideOut : IScene {
    public IScene prev, next;
    public Sf sf_prev, sf_next;
    public Sf sf;
    public int Width => sf.Width;
    public int Height => sf.Height;

	public Action<IScene> Go { set; get; } = _ => { };
	public Action<Sf> Draw { set; get; } = _ => { };

	int x = 0;
    double time = 0;
    double interval;
    bool fast;
    public TitleSlideOut(IScene prev, Sf sf_prev, IScene next, Sf sf_next) {
        
        this.prev = prev;
        this.next = next;
        this.sf = new Sf(sf_next.Width, sf_next.Height);
		x = Width;
		interval = 4f / Width;

        //Draw one frame now so that we don't cut out for one frame
        next.Update(new TimeSpan());
        Render(new TimeSpan());
    }
    public void Update(TimeSpan delta) {
        next.Update(delta);

        if (fast) {
            x = -16;
        }

        if (x > -16) {
            x -= (int)(Width * delta.TotalSeconds);
        } else {
            Go(next);
            
            return;
        }
    }
    public void Render(TimeSpan delta) {
        next.Render(delta);
        sf.Clear();
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < this.x; x++) {
                sf.SetTile(x, y, sf_prev.GetTile(x, y));
            }
            for (int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {
                var glyph = sf_next.GetGlyph(x, y);
                var value = (byte)(255 - 255 / 16 * (x - this.x));
                var fore = sf_next.GetFront(x, y);
                fore = ABGR.SetA(ABGR.Premultiply(ABGR.Blend(ABGR.Premultiply(fore), sf_prev.GetFront(x, y))), value);
                var back = sf_next.GetBack(x, y);
                back = ABGR.Blend(ABGR.Premultiply(back), ABGR.SetA(ABGR.Premultiply(sf_prev.GetBack(x, y)), value));
                sf.SetTile(x, y, new Tile(fore, back, glyph));
            }
        }
    }
    public void HandleKey(KB keyboard) {
        if (keyboard.Pressed.Intersect([KC.Enter, KC.Escape]).Any()) {
            fast = true;
        }
    }
}
public class TitleSlideIn : IScene {
    public Console prev;
    public Console next;
    int x = -16;

    Sf sf;
    int Width => sf.Width;
    int Height => sf.Height;

	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }

	public TitleSlideIn(Console prev, Console next) {
        sf = new Sf(prev.Width, prev.Height);
        this.prev = prev;
        this.next = next;
        //Draw one frame now so that we don't cut out for one frame
        Render(new TimeSpan());
    }
    public void ProcessKey(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            //Tones.pressed.Play();
            Next();
        }
    }
    public void Update(TimeSpan delta) {
        prev.Update(delta);
        if (x < Width + 16) {
            x += (int)(Width * delta.TotalSeconds);
        } else {
            Next();
        }
    }
    public void Next() {
        SadConsole.Game.Instance.Screen = next;
        next.IsFocused = true;
    }
    public void Render(TimeSpan delta) {
        next.Render(delta);
        prev.Render(delta);
        var blank = new ColoredGlyph(Color.Black, Color.Black);
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Math.Min(Width, this.x); x++) {
                //this.SetCellAppearance(x, y, blank);
                sf.SetTile(x, y, next.GetCellAppearance(x, y).ToTile());
            }
            //Fading opacity edge
            for (int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {

                var glyph = prev.GetGlyph(x, y);
                var value = 255 - 255 / 16 * (x - this.x);

                var fore = prev.GetForeground(x, y);
                fore = fore.Premultiply().Blend(next.GetForeground(x, y).Premultiply().WithValues(alpha: value));

                var back = prev.GetBackground(x, y);
                back = back.Premultiply().Blend(next.GetBackground(x, y).Premultiply().WithValues(alpha: value));

                sf.SetTile(x, y, new Tile(fore.PackedValue, back.PackedValue, glyph));
            }
        }
    }
}
public class FadeIn : IScene {
    IScene next;
    Sf sf_next;
    Sf sf;
    float alpha;

	public Action<IScene> Go { get; set; }
    public Action<Sf> Draw { get; set; }

	public FadeIn(IScene next, Sf sf_next) {
        this.next = next;
        this.sf_next = sf_next;
        this.sf = new Sf(sf_next.Width, sf_next.Height);
        Render(new TimeSpan());
    }
    public void Update(TimeSpan delta) {
        if (alpha < 1) {
            alpha += (float)(delta.TotalSeconds / 4);
        } else {
            Go(next);
        }
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        var g = new Tile(ABGR.Black, ABGR.RGBA(0, 0, 0, (byte)(255 * (1 - alpha))), 0);
        for (int y = 0; y < sf.Height; y++) {
            for (int x = 0; x < sf.Width; x++) {
                sf.SetTile(x, y, g);
            }
        }
        Draw(sf_next);
        Draw(sf);
    }
}

public class FadeOut : Console {
    Console prev;
    Action next;
    float alpha;
    int time;
    public FadeOut(Console prev, Action next, int time = 4) : base(prev.Width, prev.Height) {
        FontSize = prev.FontSize;
        this.prev = prev;
        this.next = next;
        this.time = time;
        Render(new TimeSpan());
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
        if (alpha < 1) {
            alpha += (float)(delta.TotalSeconds / time);
        } else {
            next();
        }
    }
    public override void Render(TimeSpan delta) {
        this.Clear();
        var g = new ColoredGlyph(Color.Black, new Color(0, 0, 0, alpha));
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                this.SetCellAppearance(x, y, g);
            }
        }
        prev.Render(delta);
        base.Render(delta);
    }
}

public class Pause : IScene {
    Action next;
    Sf background;
    double time;

	public Action<IScene> Go { set; get; }
	public Action<Sf> Draw { set; get; }

	public Pause(Sf background, Action next, double time = 5) {
        this.background = background;
        this.time = time;
        this.next = next;
        Render(new TimeSpan());
    }
    public void ProcessKeyboard(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            //Tones.pressed.Play();
            time = 0;
        }
    }
    public void Update(TimeSpan delta) {
        if (time > 0) {
            time -= delta.TotalSeconds;
        } else {
            next();
        }
    }
    public void Render(TimeSpan delta) {
    }
}
public class PauseTransition : Console {
    Console prev;
    Action next;
    double time;
    public PauseTransition(int Width, int Height, double time, Console prev, Action next) : base(Width, Height) {
        this.FontSize = prev.FontSize;

        this.time = time;
        this.prev = prev;
        this.next = next;
        Render(new TimeSpan());
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
        if (time > 0) {
            time -= delta.TotalSeconds;
        } else {
            next();
        }
    }
    public override void Render(TimeSpan delta) {
        prev.Render(delta);
    }
}
