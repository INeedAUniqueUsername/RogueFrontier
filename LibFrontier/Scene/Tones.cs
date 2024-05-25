using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGamer;
using RogueFrontier;
namespace RogueFrontier;
public static class Tones {
	public static SoundCtx pressed = new SoundCtx(File.ReadAllBytes($"{Assets.ROOT}/sounds/button_press.wav"), 33);
}