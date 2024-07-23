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
    public Sf sf_img;
    public Sf sf_tile;
    int Width => sf_img.GridWidth;
    int Height => sf_img.GridHeight;

    public Sf sf_icon;
    public Sf sf_ui;
    private IScene prev;
    private Sf sf_prev;
    private ShipSelectorModel context;
    private ShipControls settings;
    private Action<ShipSelectorModel> next;
    private SfLink leftArrow, rightArrow;
    double time = 0;

    public List<SfControl> controls = [];
    public PlayerCreator(IScene prev, Sf sf_prev, System World, ShipControls settings, Action<ShipSelectorModel> next) {
        sf_img = new Sf(sf_prev.GridWidth, sf_prev.GridHeight, Fonts.FONT_8x8);
        sf_ui = new Sf(Width * 4 / 3, Height, Fonts.FONT_6x8);
        sf_icon = new Sf(1, 1, Fonts.FONT_8x8) { scale = 2, pos = (17, 17) };
        this.prev = prev;
        this.sf_prev = sf_prev;
        this.next = next;
        var guessName = Main.GetRandom([
            "Luminous",
        ], World.karma);
		context = new ShipSelectorModel() {
            World = World,
            playable = [..World.types.Get<ShipClass>().Where(sc => sc.playerSettings?.startingClass == true)],
            shipIndex = 0,
            genomes = [..World.types.Get<GenomeType>()],
            genomeIndex = 0,
            playerName = guessName,
            playerGenome = World.types.Get<GenomeType>().First(),
            portrait = new char[8, 8]
        };
        this.settings = settings;

        int x = 2;
        int y = 2;

        //controls.Add(new TextPainter(context.portrait) { Position = (x, y) });

        x = 10;
        var nameField = new LabeledField(sf_ui, (x, y), "Name           ", context.playerName, (e, text) => context.playerName = text);
        controls.Add(nameField);
        y++;
        var identityLabel = new SfLabel(sf_ui, (x,y),"Identity       ");
        controls.Add(identityLabel);

        SfLink identityButton = null;
        double lastClick = 0;
        int fastClickCount = 0;
        identityButton = new SfLink(sf_ui, (x+16,y),playerGenome.name, () => {

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
                        name =              "Human Enby",
                        kind =              "human",
                        gender =            "enby",
                        subjective =        "they",
                        objective =         "them",
                        possessiveAdj =     "their",
                        possessiveNoun =    "theirs",
                        reflexive =         "theirself"
                    };
                    Enumerable.ToList<(string key, string val, Action<string> set)>([
						("Identity       ", playerGenome.name, s => playerGenome.name = s),
						("Kind           ", playerGenome.kind, s => playerGenome.kind = s),
						("Gender         ", playerGenome.gender, s => playerGenome.gender = s),
						("Subjective     ", playerGenome.subjective, s => playerGenome.subjective = s),
						("Objective      ", playerGenome.objective, s => playerGenome.objective = s),
						("Possessive Adj.", playerGenome.possessiveAdj, s => playerGenome.possessiveAdj = s),
						("Possessive Noun", playerGenome.possessiveNoun, s => playerGenome.possessiveNoun = s),
						("Reflexive      ", playerGenome.reflexive, s => playerGenome.reflexive = s)
						]).ForEach(t => {
                            controls.Add(new LabeledField(sf_ui, (x, y++), t.key, t.val, (e, s) => t.set(s)));
                        });
                }
            }
            lastClick = time;
        });
        controls.Add(identityButton); {
            string back = "[Escape] Back";
            controls.Add(new SfLink(sf_ui, (sf_ui.GridWidth - back.Length, 1), back, Back));
        } {
            string start = "[Enter] Start";
            controls.Add(new SfLink(sf_ui, (sf_ui.GridWidth - start.Length, Height - 1), start, Start));
        } PlaceArrows();
    }
    public void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
    }
    public void Render(TimeSpan drawTime) {
        sf_img.Clear();
        sf_ui.Clear();

        var current = playable[index];

        int shipDescY = 12;

        shipDescY++;
        shipDescY++;

        var nameX = Width / 4 - current.name.Length / 2;
        var y = shipDescY;
        sf_ui.Print(nameX, y, current.name);
        //We print each line twice since the art gets flattened by the square font
        //Ideally the art looks like the original with an added 3D effect

        
        Sf.DrawRect(sf_img, 16, y + 2, 4, 4, new());
        //sf_img.Tile[18, y + 3] = current.tile;
        
        sf_icon.Tile[0, 0] = current.tile;

        foreach (var (p,t) in current.playerSettings.heroImage) {

            var pos = (p.X + 2, -p.Y + Height - 36 - 2);
            sf_img.Tile[pos] = t;
        }

        var descX = 48;
        y = shipDescY;
        foreach (var line in current.playerSettings.description.Wrap(Width / 3)) {
            sf_ui.Print(descX, y, line);
            y++;
        }

        y++;

        //Show installed devices on the right pane
        sf_ui.Print(descX, y, "[Devices]");
        y++;
        foreach (var device in current.devices.Generate(World.types)) {
            sf_ui.Print(descX + 4, y, device.source.type.name);
            y++;
        }

        descX = 88;
        y = shipDescY;
        foreach (var line in settings.GetString().Split('\n', '\r')) {
            sf_ui.Print(descX, y++, line);
        }

        for (y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {

                var g = sf_img.GetGlyph(x, y);
                if (g == 0 || g == ' ') {
                    var a = (byte)Clamp(51 * Sin(time * Sin(x - y) + Sin(x) * 5 + Sin(y) * 5), 0, 255);
					sf_img.SetTile(x, y, new Tile(ABGR.SetA(ABGR.White, a), ABGR.Black, '='));
                }
            }
		}

        foreach(var c in controls) c.Render(drawTime);
		Draw?.Invoke(sf_img);
		Draw?.Invoke(sf_icon);
		Draw?.Invoke(sf_ui);
	}

    public bool showRight => index < playable.Count - 1;
    public bool showLeft => index > 0;

    public void HandleMouse(HandState state) {
        foreach(var c in controls.ToList()) {
            c.HandleMouse(state);
        }
    }
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
            int x = sf_ui.GridWidth / 4 - left.Length - 1;
            controls.Add(leftArrow = new SfLink(sf_ui, (x, shipDescY), left, SelectLeft));
        }

        string right = "[Right Arrow] ===>";
        if (showRight) {
            var x = sf_ui.GridWidth * 3 / 4 + 1;
            controls.Add(rightArrow = new SfLink(sf_ui, (x, shipDescY), right, SelectRight));
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
        Go(new TitleSlideOut(this, sf_img, prev, sf_prev));
    }
    public void Start() {
        //Tones.pressed.Play();
        next(context);
    }
}