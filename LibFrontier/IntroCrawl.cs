using Common;
using CloudJumper;
using LibGamer;

using TileTuple = (uint Foreground, uint Background, int Glyph);
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
namespace RogueFrontier;

class IntroCrawl : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	private static readonly TileImage[] images = [
        new TileImage(ImageLoader.ReadTile(Assets.GetSprite("crawl_new_era.dat"))),
        new TileImage(ImageLoader.ReadTile(Assets.GetSprite("crawl_pillars_of_creation.dat")))
    ];
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
Was more than a dream after all." }.Select((line,i) => line.Replace("\r", "")).ToArray();
    //to do: portraits
    //to do: crawl images
    public Action next;
    private int lines;
    int sectionNumber;
    int sectionIndex;
    int tick;

    int backgroundSlideX;

    bool speedUp;

    int Width, Height;

    //LoadingSymbol spinner;

    Tile[,] effect;

	int effectWidth => Width * 3/5;
	int effectHeight => Height * 3 / 5;

	List<CloudParticle> clouds;

    Random random = new Random();

    public Sf sf;


	public IntroCrawl(int Width, int Height, Action next) {
        this.sf = new Sf(Width, Height, Fonts.FONT_8x8);
        this.Width = Width;
        this.Height = Height;
        this.next = next;
        backgroundSlideX = Width;
        lines = text.Sum(line => line.Count(c => c == '\n')) + text.Length * 2;
        sectionIndex = 0;
        tick = 0;
        effect = new Tile[effectWidth, effectHeight];
        for (int y = 0; y < effectHeight; y++) {
            for (int x = 0; x < effectWidth; x++) {
                effect[x,y] = GetTile(x, y);
            }
        }
        //spinner = new LoadingSymbol(16);
        clouds = [];

        byte b (int value) => (byte)Clamp(value, 0, 255);
        uint Front(int value) {
            return ABGR.RGBA(b(255 - value / 2), b(255 - value), 255, b(255 - value / 4));
            //return new Color(128 + value / 2, 128 + value/4, 255);
        }
        uint Back(int value) {
            //return new Color(255 - value, 255 - value, 255 - value).Noise(r, 0.3).Round(17).Subtract(25);
            return ABGR.RGB(b(204 - value), b(204 - value), b(255 - value));//.Noise(random, 0.3).Round(17).Subtract(25);
        }
        Tile GetTile(int x, int y) {
            uint front = Front(255 * x / effectWidth);
            uint back = Back(255 * x / effectWidth);
            char c;
            if (random.Next(x) < 5
                || (effect[x - 1,y].Glyph != ' ' && random.Next(x) < 10)
                ) {
                const string vwls = "?&%~=+;";
                c = vwls[random.Next(vwls.Length)];
            } else {
                c = ' ';
            }


            return new Tile(front, back, c);
        }
    }
    public void Update(TimeSpan time) {
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
            next();
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
    public void Render(TimeSpan drawTime) {
        sf.Clear();

        int topEdge = Height / 5;
        int bottomEdge = 4 * Height / 5;
        switch (sectionNumber) {
            case 0: {
                    //Print background
					for(int y = 0; y < effectHeight; y++) {
						for(int x = 0; x < effectWidth; x++) {
                            sf.Tile[x, y + topEdge] = effect[x, y];
						}
					}
					break;
                }
            case 1: {
                    foreach ((var p, var t) in images[0].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        sf.SetTile(p.x, p.y, t);
                    }

                    int effectY = topEdge;
                    IEnumerable<Tile[]> GetRows() {
                        for(int y = 0; y < effect.GetLength(1); y++) {
                            IEnumerable<Tile> GetItems () {
                                for(int x = 0; x < effect.GetLength(0); x++) {
                                    yield return effect[x, y];
                                }
                            }
                            yield return [..GetItems()];
                        }
                    }
                    foreach (var line in GetRows()
                        .Where(l => l.Count() > backgroundSlideX)
                        .Select((l, i) => l[backgroundSlideX..])) {
                        sf.Print(backgroundSlideX, effectY, line);
                        effectY++;
                    }

                    break;
                }
            case 2: {
                    if (backgroundSlideX < Width) {
                        foreach ((var p, var t) in images[0].Sprite
                            .Where(p => p.Key.x >= backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                            sf.SetTile(p.x, p.y, t);
                        }
                    }

                    foreach ((var p, var t) in images[1].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        sf.SetTile(p.x, p.y, t);
                    }
                    break;
                }
            case 3: {
                    if (backgroundSlideX < Width) {
                        foreach ((var p, var t) in images[1].Sprite
                            .Where(p => p.Key.x >= backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                            sf.SetTile(p.x, p.y, t);
                        }
                    }
                    var b = new Tile(ABGR.Black, ABGR.Black, 0);
                    foreach ((var p, var t) in images[1].Sprite
                        .Where(p => p.Key.x < backgroundSlideX && p.Key.y > topEdge && p.Key.y < bottomEdge)) {

                        sf.SetTile(p.x, p.y, b);
                    }
                    break;
                }
        }

        var top = Height - 1;
        foreach (var cloud in clouds) {
            var (x, y) = cloud.pos;
            sf.SetFront(x, top - y, cloud.symbol.Foreground);
            sf.SetGlyph(x, top - y, cloud.symbol.Glyph);
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
                        sf.Print(textX, textY, "" + c, ABGR.White, sf.GetBack(textX, textY));
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
                        sf.Print(textX, textY, "" + c, ABGR.White, sf.GetBack(textX, textY));
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
        Draw?.Invoke(sf);
    }
    public void HandleKey (KB info) {
        if (info.IsPress(KC.Enter)) {
            if (speedUp) {
                sectionNumber = text.Length;
            } else {
                speedUp = true;
            }
        }
    }
}
