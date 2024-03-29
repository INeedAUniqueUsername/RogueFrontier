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

namespace RogueFrontier;

class IntroCrawl : Console {
    private readonly ColorImage[] images = {
            new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/sprites/NewEra.cg"))),
            new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/sprites/PillarsOfCreation.cg")))
        };
    private readonly string[] text = new[] {
@"In a vision I could see
the words of someone speaking
The Dictator who decreed
an ultimate power for the seeking.",

@"As mankind fired their engines,
and prospered throughout the stars,
the warlords readied their cannons:
""The universe shall be ours!""",

@"In a future, I could see
the threat of forever wars
But then The Orator mentioned
a chance for a journey of yours",

@"""Dream upon my echo.
Listen for my vow.
I know that you are out there.
Do you hear me now?""

""There shall be an answer,
if you heed my call -
Travel to the fortress,
where the galaxies revolve.""",

@"At last, there is my mission
to walk about the divine halls.
And it seems to me, this vision
Was more than a dream after all." }.Select(line => line.Replace("\r", "")).ToArray();
    //to do: portraits
    //to do: crawl images
    public Func<Console> next;
    private int lines;
    int sectionNumber;
    int sectionIndex;
    int tick;

    int backgroundSlideX;

    bool speedUp;

    //LoadingSymbol spinner;

    ColoredString[] effect;

    List<CloudParticle> clouds;

    Random random = new Random();
    public IntroCrawl(int width, int height, Func<Console> next) : base(width, height) {
        this.next = next;
        backgroundSlideX = Width;
        lines = text.Sum(line => line.Count(c => c == '\n')) + text.Length * 2;
        sectionIndex = 0;
        tick = 0;
        int effectWidth = Width * 3 / 5;
        int effectHeight = Height * 3 / 5;
        effect = new ColoredString[effectHeight];
        for (int y = 0; y < effectHeight; y++) {
            effect[y] = new ColoredString(effectWidth);
            for (int x = 0; x < effectWidth; x++) {
                effect[y][x] = GetGlyph(x, y);
            }
        }
        //spinner = new LoadingSymbol(16);
        clouds = new List<CloudParticle>();
        Color Front(int value) {
            return new Color(255 - value / 2, 255 - value, 255, 255 - value / 4);
            //return new Color(128 + value / 2, 128 + value/4, 255);
        }
        Color Back(int value) {
            //return new Color(255 - value, 255 - value, 255 - value).Noise(r, 0.3).Round(17).Subtract(25);
            return new Color(204 - value, 204 - value, 255 - value).Noise(random, 0.3).Round(17).Subtract(25);
        }
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
    public override void Update(TimeSpan time) {
        if (backgroundSlideX < Width) {
            tick++;
            if (tick % 2 == 0) {
                backgroundSlideX++;
            }
            UpdateClouds();
        } else if (sectionNumber < text.Length) {
            if (sectionIndex < text[sectionNumber].Length) {
                tick++;
                //Scroll text
                if (speedUp || tick % 4 == 0) {
                    sectionIndex++;
                }
                UpdateClouds();
            } else {
                sectionNumber++;
                sectionIndex = 0;
                backgroundSlideX = 0;
            }
        } else {
            var c = next();
            if (c != null) {
                GameHost.Instance.Screen = c;
                c.IsFocused = true;
            }
        }

        void UpdateClouds() {
            //Update clouds
            if (tick % 8 == 0) {
                clouds.ForEach(c => c.Update(random));
            }
            //Spawn cloud
            if (tick % 64 == 0) {

                int effectMinY = Height / 5;
                int effectMaxY = 4 * Height / 5;

                CloudParticle.CreateClouds(effectMinY, effectMaxY, clouds, random);
            }
        }
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();

        int topEdge = Height / 5;
        int bottomEdge = 4 * Height / 5;
        switch (sectionNumber) {
            case 0: {
                    //Print background
                    int effectY = topEdge;
                    foreach (var line in effect) {
                        this.Print(0, effectY, line);
                        effectY++;
                    }
                    break;
                }
            case 1: {
                    foreach ((var p, var t) in images[0].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        this.SetCellAppearance(p.x, p.y, t);
                    }

                    int effectY = topEdge;
                    foreach (var line in effect
                        .Where(l => l.Count() > backgroundSlideX)
                        .Select(l => l.SubString(backgroundSlideX))) {

                        this.Print(backgroundSlideX, effectY, line);
                        effectY++;
                    }

                    break;
                }
            case 2: {
                    if (backgroundSlideX < Width) {
                        foreach ((var p, var t) in images[0].Sprite
                            .Where(p => p.Key.x >= backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                            this.SetCellAppearance(p.x, p.y, t);
                        }
                    }

                    foreach ((var p, var t) in images[1].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        this.SetCellAppearance(p.x, p.y, t);
                    }
                    break;
                }
            case 3: {
                    if (backgroundSlideX < Width) {
                        foreach ((var p, var t) in images[1].Sprite
                            .Where(p => p.Key.x >= backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                            this.SetCellAppearance(p.x, p.y, t);
                        }
                    }
                    var b = new ColoredGlyph(Color.Black, Color.Black, 0);
                    foreach ((var p, var t) in images[1].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        this.SetCellAppearance(p.x, p.y, b);
                    }
                    break;
                }
        }

        var top = Height - 1;
        foreach (var cloud in clouds) {
            var (x, y) = cloud.pos;
            this.SetForeground(x, top - y, cloud.symbol.Foreground);
            this.SetGlyph(x, top - y, cloud.symbol.Glyph);
        }


        //Print text
        int ViewWidth = Width;
        int ViewHeight = Height;

        //int leftMargin = (ViewWidth) / 2;
        int leftMargin = Width * 2 / 5;
        int topMargin = (ViewHeight / 2) - lines / 2;
        int textX = leftMargin;
        int textY = topMargin;

        for (int i = 0; i < sectionNumber; i++) {
            PrintSection(text[i]);
            textX = leftMargin;
            textY++;
            textY++;
        }
        if (sectionNumber < text.Length) {
            PrintSubSection(text[sectionNumber], sectionIndex);
        }
        void PrintSubSection(string section, int index) {
            for (int i = 0; i < index; i++) {
                char c = section[i];
                if (c == '\n') {
                    textX = leftMargin;
                    textY++;
                } else {
                    if (c != ' ') {
                        this.Print(textX, textY, "" + c, Color.White, this.GetBackground(textX, textY));
                    }
                    textX++;
                }
            }
        }
        void PrintSection(string section) {
            for (int i = 0; i < section.Length; i++) {
                char c = section[i];
                if (c == '\n') {
                    textX = leftMargin;
                    textY++;
                } else {
                    if (c != ' ') {
                        this.Print(textX, textY, "" + c, Color.White, this.GetBackground(textX, textY));
                    }
                    textX++;
                }
            }
        }
        /*
        {
            var symbol = spinner.Draw();
            int symbolX = Width - symbol[0].Count;
            int symbolY = Height - symbol.Length;
            foreach (var line in symbol) {
                this.Print(symbolX, symbolY, line);
                symbolY++;
            }
        }
        */
        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(SadConsole.Input.Keys.Enter)) {
            if (speedUp) {
                sectionNumber = text.Length;
            } else {
                speedUp = true;
            }
        }

        return base.ProcessKeyboard(info);
    }
}
