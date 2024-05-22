using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public class SoundCtx {
	public bool playing;
	public (float x, float y) pos;
	public byte[] data;
	public float volume;
	public SoundCtx (byte[] data, int volume) {
		this.data = data;
		this.volume = volume;
	}
}
