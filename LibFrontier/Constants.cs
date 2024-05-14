using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public static class Constants {

	public static int TICKS_PER_SECOND = 60;
	public static byte[] LoadAudio (string s) => File.ReadAllBytes(s);
}