using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAtomics;
public class AimDialog(Player player) : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	void IScene.Update(System.TimeSpan delta) {

	}
	void IScene.Render(System.TimeSpan delta) {

	}
	void IScene.HandleKey(LibGamer.KB kb) {
		if(kb.IsDown(KC.Escape)) {
			Go?.Invoke(null);
		}
	}
	void IScene.HandleMouse(LibGamer.HandState mouse) {
	}
}
