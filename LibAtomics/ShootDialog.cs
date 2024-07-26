using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAtomics;
public class ShootDialog : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	public ShootDialog(int Width, int Height, Assets assets, Player player) {
		this.Width = Width;
		this.Height = Height;
		this.assets = assets;
		this.player= player;
		this.sf = new Sf(Width, Height, assets.IBMCGA_6x8) { scale = 1 }; ;

		title = new SfPane(sf, new Rect(32, 30, 32, 3), new() {
			f = ABGR.DeepPink,
			b = ABGR.Black
		});
		weapons = new SfPane(sf, new Rect(32, 32, 32, 26), new() {
			connectAbove = true,
			f = ABGR.DeepPink,
			b = ABGR.Black
		});
	}
	int Width;
	int Height;
	Assets assets;
	Player player;

	public Sf sf;

	SfPane title;
	SfPane weapons;
	void IScene.Update(System.TimeSpan delta) {

	}
	void IScene.Render(System.TimeSpan delta) {
		sf.Clear();

		title.Render(delta);
		title.Print(0, Tile.Arr("Shoot Weapon", ABGR.White, ABGR.Black));
		weapons.Render(delta);
		Draw?.Invoke(sf);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
		if(kb.IsDown(KC.Escape)) {
			Go?.Invoke(null);
		}
	}
	void IScene.HandleMouse(LibGamer.HandState mouse) {
	}
}
