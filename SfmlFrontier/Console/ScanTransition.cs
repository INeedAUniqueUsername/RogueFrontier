using LibGamer;
using SadConsole.Input;
namespace RogueFrontier;

public class ScanTransition : IScene {
    public Action<Sf> Draw { set; get; }
	public Action<IScene> Go { get; set; }

	IScene next;
    Sf sf;
    public int Width => sf.Width;
    public int Height => sf.Height;
    double y;
    public ScanTransition(IScene next) {
        y = 0;
        this.next = next;
        next.Render(new TimeSpan());
    }
    public void HandleKey(Keyboard keyboard) {
        if (keyboard.KeysPressed.Count > 0) {
            Transition();
            //next.ProcessKeyboard(keyboard);
        }
    }
    public void Update(TimeSpan delta) {
        if (y < sf.Height) {
            y += delta.TotalSeconds * sf.Height * 3;
        } else {
            Transition();
        }
    }
    public void Transition() {
        Go(next);
    }
    public void Render(TimeSpan delta) {
		Sf sf_next = null;

        void SetSf(Sf sf) {
            sf_next = sf;
        }
        next.Draw += SetSf;
        next.Render(delta);
        next.Draw -= SetSf;

		sf.Clear();
        var last = (int)Math.Min(this.y - 1, Height - 1);

        int y;
        for (y = 0; y < last; y++) {
            for (int x = 0; x < Width; x++) {
                sf.SetTile(x, y, sf_next.GetTile(x, y));
            }
        }
        y = last;
        for (int x = 0; x < Width; x++) {
            sf.SetTile(x, y, new Tile(ABGR.Transparent, ABGR.SetA(ABGR.White,128), 0));
        }
        Tile[] empty = Tile.Arr(new string(' ', Width), ABGR.Transparent, ABGR.Transparent);
        for (y = last + 1; y < Height; y++) {
            sf.Print(0, y, empty);
        }
        Draw(sf);
        //            next.Render(delta);
    }
}
