using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public static class Tones {
	public static SoundDesc pressed = new() {
		data= File.ReadAllBytes("Assets/sounds/button_press.wav"),
		volume = 33
	};
}