using Common;
using LibGamer;
using System.Collections.Generic;
using System;
using System.Linq;
namespace RogueFrontier;

class ShipSelectorModel {
    public System World;
    public List<ShipClass> playable;
    public int shipIndex;

    public List<GenomeType> genomes;
    public int genomeIndex;

    public string playerName;
    public GenomeType playerGenome;

    public char[,] portrait;
}
class PlayerCreator : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }


	private ref System World => ref context.World;
    private ref List<ShipClass> playable => ref context.playable;
    private ref int index => ref context.shipIndex;
    private ref List<GenomeType> genomes => ref context.genomes;
    private ref int genomeIndex => ref context.genomeIndex;
    private ref GenomeType playerGenome => ref context.playerGenome;
    public Sf sf;
    int Width => sf.Width;
    int Height => sf.Height;
    private IScene prev;
    private Sf sf_prev;
    private ShipSelectorModel context;
    private ShipControls settings;
    private Action<ShipSelectorModel> next;
    private SfLink leftArrow, rightArrow;
    double time = 0;

    public List<SfControl> controls = [];
    public PlayerCreator(IScene prev, Sf sf_prev, System World, ShipControls settings, Action<ShipSelectorModel> next) {
        sf = new Sf(sf_prev.Width, sf_prev.Height, Fonts.FONT_8x8);
        this.prev = prev;
        this.sf_prev = sf_prev;
        this.next = next;

        context = new ShipSelectorModel() {
            World = World,
            playable = World.types.Get<ShipClass>().Where(sc => sc.playerSettings?.startingClass == true).ToList(),
            shipIndex = 0,
            genomes = World.types.Get<GenomeType>().ToList(),
            genomeIndex = 0,
            playerName = "Luminous",
            playerGenome = World.types.Get<GenomeType>().First(),
            portrait = new char[8, 8]
        };
        this.settings = settings;

        int x = 2;
        int y = 2;

        //controls.Add(new TextPainter(context.portrait) { Position = (x, y) });

        x = 10;

        var nameField = new LabeledField(sf, (x, y), "Name           ", context.playerName, (e, text) => context.playerName = text);
        controls.Add(nameField);

        y++;

        var identityLabel = new SfText(sf, (x,y),"Identity       ");
        controls.Add(identityLabel);

        SfLink identityButton = null;
        double lastClick = 0;
        int fastClickCount = 0;
        identityButton = new SfLink(sf, (x+16,y),playerGenome.name, () => {

            //Tones.pressed.Play();

            if (time - lastClick > 0.5) {
                genomeIndex = (genomeIndex + 1) % genomes.Count;
                playerGenome = genomes[genomeIndex];
                identityButton.text = playerGenome.name;
                fastClickCount = 0;
            } else {
                fastClickCount++;
                if (fastClickCount == 2) {
                    controls.Remove(identityLabel);
                    controls.Remove(identityButton);

                    context.playerGenome = new GenomeType() {
                        name = "Human Variant",
                        species = "human",
                        gender = "variant",
                        subjective = "they",
                        objective = "them",
                        possessiveAdj = "their",
                        possessiveNoun = "theirs",
                        reflexive = "theirself"
                    };
                    Enumerable.ToList<(string key, string val, Action<string> set)>([
						("Identity       ", playerGenome.name, (s) => playerGenome.name = s),
						("Species        ", playerGenome.species, (s) => playerGenome.species = s),
						("Gender         ", playerGenome.gender, s => playerGenome.gender = s),
						("Subjective     ", playerGenome.subjective, (s) => playerGenome.subjective = s),
						("Objective      ", playerGenome.objective, (s) => playerGenome.objective = s),
						("Possessive Adj.", playerGenome.possessiveAdj, (s) => playerGenome.possessiveAdj = s),
						("Possessive Noun", playerGenome.possessiveNoun, (s) => playerGenome.possessiveNoun = s),
						("Reflexive      ", playerGenome.reflexive, (s) => playerGenome.reflexive = s)

						]).ForEach(t => {
                            controls.Add(new LabeledField(sf, (x, y++), t.key, t.val, (e, s) => t.set(s)));
                        });
                }
            }
            lastClick = time;
        });
        controls.Add(identityButton);

        string back = "[Escape] Back";
        controls.Add(new SfLink(sf, (Width - back.Length, 1), back, Back));

        string start = "[Enter] Start";
        controls.Add(new SfLink(sf, (Width - start.Length, Height - 1), start, Start));
        PlaceArrows();
    }
    public void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    public void Render(TimeSpan drawTime) {
        sf.Clear();

        var current = playable[index];

        int shipDescY = 12;

        shipDescY++;
        shipDescY++;

        var nameX = Width / 4 - current.name.Length / 2;
        var y = shipDescY;
        sf.Print(nameX, y, current.name);

        var map = current.playerSettings.map;
        var mapWidth = map.Select(line => line.Length).Max();
        var mapX = Width / 4 - mapWidth / 2;
        y++;
        //We print each line twice since the art gets flattened by the square font
        //Ideally the art looks like the original with an added 3D effect
        foreach (var line in current.playerSettings.map) {
            for (int i = 0; i < line.Length; i++) {
                sf.SetTile(mapX + i, y, new Tile(ABGR.RGBA(255, 255, 255, (byte)(230 + Math.Sin(time * 1.5 + Math.Sin(i) * 5 + Math.Sin(y) * 5) * 25)), ABGR.Black, line[i]));
            }
            y++;
            for (int i = 0; i < line.Length; i++) {
                sf.SetTile(mapX + i, y, new Tile(ABGR.RGBA(255, 255, 255, (byte)(230 + Math.Sin(time * 1.5 + Math.Sin(i) * 5 + Math.Sin(y) * 5) * 25)), ABGR.Black, line[i]));
            }
            y++;
        }

        string s = "[Image is for promotional use only]";
        var strX = Width / 4 - s.Length / 2;
        sf.Print(strX, y, s);

        var descX = Width * 2 / 4;
        y = shipDescY;
        foreach (var line in current.playerSettings.description.Wrap(Width / 3)) {
            sf.Print(descX, y, line);
            y++;
        }

        y++;

        //Show installed devices on the right pane
        sf.Print(descX, y, "[Devices]");
        y++;
        foreach (var device in current.devices.Generate(World.types)) {
            sf.Print(descX + 4, y, device.source.type.name);
            y++;
        }
        y += 2;
        foreach (var line in settings.GetString().Split('\n', '\r')) {
            sf.Print(descX, y++, line);
        }

        for (y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {

                var g = sf.GetGlyph(x, y);
                if (g == 0 || g == ' ') {
                    sf.SetTile(x, y, new Tile(
                        ABGR.RGBA(255, 255, 255, (byte)(51 * Math.Sin(time * Math.Sin(x - y) + Math.Sin(x) * 5 + Math.Sin(y) * 5))),
                        ABGR.Black,
                        '='));
                }
            }
        }
        Draw(sf);
    }

    public bool showRight => index < playable.Count - 1;
    public bool showLeft => index > 0;


	public void HandleKey(KB kb) {
        if (kb[KC.Right] == KS.Press && showRight) {
            SelectRight();
        }
        if (kb[KC.Left] == KS.Press && showLeft) {
            SelectLeft();
        }
        if (kb[KC.Escape] == KS.Press) {
            Back();
        }
        if (kb[KC.Enter] == KS.Press) {
            Start();
        }
    }
    public void UpdateArrows() {
        if (leftArrow != null) {
            controls.Remove(leftArrow);
        }
        if (rightArrow != null) {
            controls.Remove(rightArrow);
        }
        PlaceArrows();
    }
    public void PlaceArrows() {
        int shipDescY = 12;

        string left = "<===  [Left Arrow]";
        if (showLeft) {
            int x = Width / 4 - left.Length - 1;
            controls.Add(leftArrow = new SfLink(sf, (x, shipDescY), left, SelectLeft));
        }

        string right = "[Right Arrow] ===>";
        if (showRight) {
            var x = Width * 3 / 4 + 1;
            controls.Add(rightArrow = new SfLink(sf, (x, shipDescY), right, SelectRight));
        }
    }

    public void SelectLeft() {
        //Tones.pressed.Play();
        index = (playable.Count + index - 1) % playable.Count;
        UpdateArrows();
    }
    public void SelectRight() {
        //Tones.pressed.Play();
        index = (index + 1) % playable.Count;
        UpdateArrows();
    }

    public void Back() {
        //Tones.pressed.Play();
        Go(new TitleSlideOut(this, sf, prev, sf_prev));
    }
    public void Start() {
        //Tones.pressed.Play();
        next(context);
    }
}