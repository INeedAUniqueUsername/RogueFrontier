using CloudJumper;
using LibGamer;
using System;
using System.Collections.Generic;

namespace RogueFrontier;

class OutroCrawl : IScene {
	public Action<IScene> Go { set; get; }
	public Action<Sf> Draw { set; get; }
	public Action<SoundCtx> PlaySound { get; set; }

	//public static readonly SoundBuffer music = new SoundBuffer("Assets/music/IntroductionToTheSnow.wav");
    //public Sound bgm = new Sound() { Volume = 50, SoundBuffer = music };
    
    int tick;
    double time;
    //LoadingSymbol spinner;

    //ColoredString[] effect;

    List<CloudParticle> clouds;

    Random random = new Random();

    Sf cloudLayer;

    Sf sf;
    public int Width => sf.GridWidth;
    public int Height => sf.GridHeight;


	private IScene sub;
	public OutroCrawl(int width, int height, Action next) {
#if false
        sf = new Sf(width, height);
        var frame1 = new ImageDisplay(width, height, TileImage.FromFile("Assets/epilogue_1.asc.cg"), new());

        //Children.Add(cloudLayer = new(width, height));

        Console Scale (int s) => new Console(Width / s, Height / s);// { FontSize = FontSize * s };
        ScreenSurface Pane(string headingStr, string subheadingStr) {
            var pane = new Console(Width, Height);


            var heading = Scale(4);
            heading.PrintCenter(heading.Height / 2, new ColoredString(headingStr, Color.White, new(0, 0, 0, 153)));

            pane.Children.Add(heading);

            var subheading = Scale(2);
            var y = subheading.Height / 2 + 2;
            foreach(var line in subheadingStr.Replace("\r","").Split("\n")) {
                subheading.PrintCenter(y++, new ColoredString(line, Color.White, new(0, 0, 0, 153)));
            }
            pane.Children.Add(subheading);
            return pane;
        }
        Sf Empty() => new Sf(Width, Height);
        EndCrawl();
        void EndCrawl() {
            MinimalCrawlScreen mc = null;
            sub = mc = new MinimalCrawlScreen(sf, "You have left Human Space.\n\n", () => {
                Pause(mc.sf, () => Pause(Empty(), BeginCredits, 2), 3);
            });
            
            //{ Position = new(Surface.Width / 4, 8), IsFocused = true };
            //Children.Add(ds);
        }
        void Pause(Sf background, Action next, double time) {
            Pause p = null;
            sub = p = new Pause(background, () => {
                next();
            }, time);
        }
        void BeginCredits() {
            bgm.Play();

            var parts = new[] {
                ("Rogue Frontier",  "An adventure by INeedAUniqueUsername"),
                ("Inspired by", "Transcendence by George Moromisato\n" +
                                "Dwarf Fortress by Tarn Adams\n"),
                ("Made with",   "C Sharp + SadConsole + SFML\n" +
                                "ASECII (sprites) + Transgenesis (data)\n" +
                                "MuseScore (music) + Chiptone (sfx)\n"),
                ("Music Used",  "\"Introduction to the Snow\" by Miracle Musical"),
                ("Thank you", "for playing Rogue Frontier"),
            };

            ImageDisplay prevFrame = null;
            Show();
            void Show(int i = 0) {

                if (prevFrame != null) {
                    //Children.Remove(prevFrame);
                }
                ImageDisplay frame = i == 0 ? frame1 : null;
                Slide slide = null;
                if(frame != prevFrame) {
                    slide = new Slide(prevFrame, frame, () => { });
                    //Children.Insert(0, slide);

                    prevFrame = frame;
                }


                var (h, h2) = parts[i];
                double textTime = 4.2, emptyTime = 0.3;
                Pause(Pane(h, h2), () => Pause(Empty(), () => {

                    if (slide != null) {
                        //Children.Remove(slide);
                    }
                    if (frame != null) {
                        //Children.Insert(0, frame);
                    }
                    if(i < parts.Length - 1) {
                        Show(i + 1);
                    } else {
                        next();
                    }
                }, emptyTime), textTime);


            }
        }
        int effectWidth = Width * 3 / 5;
        int effectHeight = Height * 3 / 5;
        effect = new ColoredString[effectHeight];
        for (int y = 0; y < effectHeight; y++) {
            effect[y] = new ColoredString(effectWidth);
            for (int x = 0; x < effectWidth; x++) {
                effect[y][x] = GetGlyph(x, y);
            }
        }
        clouds = new List<CloudParticle>();
        Color Front(int value) =>
            new Color(255 - value / 2, 255 - value, 255, 255 - value / 4);
        Color Back(int value)
            => new Color(204 - value, 204 - value, 255 - value).Noise(random, 0.3).Round(17).Subtract(25);
        ColoredGlyphAndEffect GetGlyph(int x, int y) {
            Color front = Front(255 * x / effectWidth);
            Color back = Back(255 * x / effectWidth);
            char c;
            if (random.Next(x) < 5
                || (effect[y][x - 1].GlyphCharacter != ' ' && random.Next(x) < 10)
                ) {
                const string vwls = "?&%~=+;";
                c = vwls[random.Next(vwls.Length)];
            } else {
                c = ' ';
            }


            return new ColoredGlyphAndEffect() { Foreground = front, Background = back, Glyph = c };
        }
#endif
    }
    public void Update(TimeSpan delta) {
        this.time += delta.TotalSeconds;
        tick++;
        //Update clouds
        if (tick % 8 == 0) {
            clouds.ForEach(c => c.Update(random));
        }

        if(time > 16) {
            return;
        }
        //Spawn cloud
        if (tick % 64 == 0) {

            int effectMinY = Height / 5;
            int effectMaxY = 4 * Height / 5;

            CloudParticle.CreateClouds(effectMinY, effectMaxY, clouds, random);
        }
    }
    public void Render(TimeSpan drawTime) {
        cloudLayer.Clear();
        var top = Height - 1;
        foreach (var cloud in clouds) {
            var (x, y) = cloud.pos;
            cloudLayer.SetFront(x, top - y, cloud.symbol.Foreground);
            cloudLayer.SetGlyph(x, top - y, cloud.symbol.Glyph);
        }
    }
}
public class Slide : IScene {
    public Action<IScene> Go { get; set; }
    public Action<Sf> Draw { set; get; }
    public Action<SoundCtx> PlaySound {set; get;}
    public Sf prev, next;
    Sf sf;
    int Width => sf.GridWidth;
    int Height => sf.GridHeight;
    public Action done;
    int x = 0;
    public Slide(Sf prev, Sf next, Action done) {
        this.prev = prev;
        this.next = next;
        this.sf = new Sf((prev ?? next).GridWidth, (prev ?? next).GridHeight, Fonts.FONT_8x8);
        this.done = done;
    }
    public void Update(TimeSpan delta) {
        if (x < Width) {
            x += (int)(Width * delta.TotalSeconds);
        } else {
            done();
        }
    }
    public void Render(TimeSpan delta) {
        sf.Clear();

        var blank = new Tile(ABGR.Black, ABGR.Black, ' ');
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var cell =
                    x < this.x ?
                        next?.GetTile(x, y) ?? blank :
                        prev?.GetTile(x, y) ?? blank;
                sf.SetTile(x, y, cell);
            }
        }
        Draw(sf);
    }
}