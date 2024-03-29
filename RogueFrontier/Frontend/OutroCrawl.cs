
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using static SadConsole.ColoredString;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using System.IO;
using Common;
using ASECII;
using CloudJumper;
using SFML.Audio;

namespace RogueFrontier;

class OutroCrawl : Console {

    public static readonly SoundBuffer music = new SoundBuffer("RogueFrontierContent/music/IntroductionToTheSnow.wav");
    public Sound bgm = new Sound() { Volume = 50, SoundBuffer = music };
    
    int tick;
    double time;
    //LoadingSymbol spinner;

    ColoredString[] effect;

    List<CloudParticle> clouds;

    Random random = new Random();

    Console cloudLayer;
    public OutroCrawl(int width, int height, Action next) : base(width, height) {
        var frame1 = new ImageDisplay(width, height, ColorImage.FromFile("RogueFrontierContent/epilogue_1.asc.cg"), new());

        Children.Add(cloudLayer = new(width, height));

        Console Scale(int s) => new Console(Width / s, Height / s) { FontSize = FontSize * s };
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
        Console Empty() => new Console(Width, Height);
        EndCrawl();
        void EndCrawl() {
            MinimalCrawlScreen ds = null;
            ds = new("You have left Human Space.\n\n", () => {
                Children.Remove(ds);
                Pause(ds, () => Pause(Empty(), BeginCredits, 2), 3);
            }) { Position = new(Surface.Width / 4, 8), IsFocused = true };
            Children.Add(ds);
        }
        void Pause(ScreenSurface background, Action next, double time) {
            Pause p = null;
            p = new Pause(background, () => {
                Children.Remove(p);
                next();
            }, time);
            Children.Add(p);
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
                    Children.Remove(prevFrame);
                }
                ImageDisplay frame = i == 0 ? frame1 : null;
                Slide slide = null;
                if(frame != prevFrame) {
                    slide = new Slide(prevFrame, frame, () => { });
                    Children.Insert(0, slide);

                    prevFrame = frame;
                }


                var (h, h2) = parts[i];
                double textTime = 4.2, emptyTime = 0.3;
                Pause(Pane(h, h2), () => Pause(Empty(), () => {

                    if (slide != null) {
                        Children.Remove(slide);
                    }
                    if (frame != null) {
                        Children.Insert(0, frame);
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
    }
    public override void Update(TimeSpan delta) {

        base.Update(delta);
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
    public override void Render(TimeSpan drawTime) {
        cloudLayer.Clear();

        var top = Height - 1;
        foreach (var cloud in clouds) {
            var (x, y) = cloud.pos;
            cloudLayer.SetForeground(x, top - y, cloud.symbol.Foreground);
            cloudLayer.SetGlyph(x, top - y, cloud.symbol.Glyph);
        }
        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {
        

        return base.ProcessKeyboard(info);
    }
}


public class Slide : Console {
    public Console prev, next;
    public Action done;
    int x = 0;
    public Slide(Console prev, Console next, Action done) : base((prev??next).Width, (prev??next).Height) {
        this.prev = prev;
        this.next = next;
        this.done = done;
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);

        if (x < Width) {
            x += (int)(Width * delta.TotalSeconds);
        } else {
            done();
        }
    }
    public override void Render(TimeSpan delta) {
        base.Render(delta);
        this.Clear();

        var blank = new ColoredGlyph(Color.Black, Color.Black);
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var cell =
                    x < this.x ?
                        next?.GetCellAppearance(x, y) ?? blank :
                        prev?.GetCellAppearance(x, y) ?? blank;
                this.SetCellAppearance(x, y, cell);
            }
        }
    }
}