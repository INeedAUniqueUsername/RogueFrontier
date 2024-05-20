using Common;
using SadRogue.Primitives;
using Label = LibSadConsole.Label;
using LibSadConsole;
using LibGamer;
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
    private ref System World => ref context.World;
    private ref List<ShipClass> playable => ref context.playable;
    private ref int index => ref context.shipIndex;
    private ref List<GenomeType> genomes => ref context.genomes;
    private ref int genomeIndex => ref context.genomeIndex;
    private ref GenomeType playerGenome => ref context.playerGenome;
    public List<object> Children = new();
    Sf sf; int Width => sf.Width; int Height => sf.Height;
    private IScene prev;
    private Sf sf_prev;
    private ShipSelectorModel context;
    private ShipControls settings;
    private Action<ShipSelectorModel> next;
    private LabelButton leftArrow, rightArrow;
    double time = 0;
    public PlayerCreator(IScene prev, Sf sf_prev, System World, ShipControls settings, Action<ShipSelectorModel> next) {
        sf = new Sf(sf_prev.Width, sf_prev.Height);
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

        Children.Add(new TextPainter(context.portrait) { Position = (x, y) });

        x = 10;

        var nameField = new LabeledField("Name           ", context.playerName, (e, text) => context.playerName = text) { Position = (x, y) };
        this.Children.Add(nameField);

        y++;

        Label identityLabel = new Label("Identity       ") { Position = (x, y) };
        this.Children.Add(identityLabel);

        LabelButton identityButton = null;
        double lastClick = 0;
        int fastClickCount = 0;
        identityButton = new LabelButton(playerGenome.name, () => {

            //Tones.pressed.Play();

            if (time - lastClick > 0.5) {
                genomeIndex = (genomeIndex + 1) % genomes.Count;
                playerGenome = genomes[genomeIndex];
                identityButton.text = playerGenome.name;
                fastClickCount = 0;
            } else {
                fastClickCount++;
                if (fastClickCount == 2) {
                    this.Children.Remove(identityLabel);
                    this.Children.Remove(identityButton);

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
                    this.Children.Add(new LabeledField("Identity       ", playerGenome.name, (e, s) => playerGenome.name = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Species        ", playerGenome.species, (e, s) => playerGenome.species = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Gender         ", playerGenome.gender, (e, s) => playerGenome.gender = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Subjective     ", playerGenome.subjective, (e, s) => playerGenome.subjective = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Objective      ", playerGenome.objective, (e, s) => playerGenome.objective = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Possessive Adj.", playerGenome.possessiveAdj, (e, s) => playerGenome.possessiveAdj = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Possessive Noun", playerGenome.possessiveNoun, (e, s) => playerGenome.possessiveNoun = s) { Position = (x, y++) });
                    this.Children.Add(new LabeledField("Reflexive      ", playerGenome.reflexive, (e, s) => playerGenome.reflexive = s) { Position = (x, y++) });
                }
            }
            lastClick = time;
        }) { Position = new Point(x + 16, y) };
        Children.Add(identityButton);

        string back = "[Escape] Back";
        Children.Add(new LabelButton(back, Back) {
            Position = new Point(Width - back.Length, 1)
        });

        string start = "[Enter] Start";
        Children.Add(new LabelButton(start, Start) {
            Position = new Point(Width - start.Length, Height - 1)
        });
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

	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }

	public void HandleKey(KB kb) {
        if (kb[KC.Right] == KS.Pressed && showRight) {
            SelectRight();
        }
        if (kb[KC.Left] == KS.Pressed && showLeft) {
            SelectLeft();
        }
        if (kb[KC.Escape] == KS.Pressed) {
            Back();
        }
        if (kb[KC.Enter] == KS.Pressed) {
            Start();
        }
    }
    public void UpdateArrows() {
        if (leftArrow != null) {
            Children.Remove(leftArrow);
        }
        if (rightArrow != null) {
            Children.Remove(rightArrow);
        }
        PlaceArrows();
    }
    public void PlaceArrows() {
        int shipDescY = 12;

        string left = "<===  [Left Arrow]";
        if (showLeft) {
            int x = Width / 4 - left.Length - 1;
            Children.Add(leftArrow = new LabelButton(left, SelectLeft) {
                Position = new Point(x, shipDescY)
            });
        }

        string right = "[Right Arrow] ===>";
        if (showRight) {
            var x = Width * 3 / 4 + 1;
            Children.Add(rightArrow = new LabelButton(right, SelectRight) {
                Position = new Point(x, shipDescY)
            });
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