using LibGamer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public static class Fonts {
	public static string IBMCGA_8X8_FONT { get; } = $"{Assets.ROOT}/font/IBMCGA+_8x8.font";
	public static string IBMCGA_6X8_FONT { get; } = $"{Assets.ROOT}/font/IBMCGA+_6x8.font";
	public static string IBMCGA_8X8_PNG { get; } = $"{Assets.ROOT}/font/IBMCGA+_8x8.png";
	public static string IBMCGA_6X8_PNG { get; } = $"{Assets.ROOT}/font/IBMCGA+_6x8.png";
	public static Tf FONT_8x8 { get; } = new Tf(File.ReadAllBytes(IBMCGA_8X8_PNG), "IBMCGA+_8x8", 8, 8, 256 / 8, 176 / 8, 219);
	public static Tf FONT_6x8 { get; } = new Tf(File.ReadAllBytes(IBMCGA_6X8_PNG), "IBMCGA+_6x8", 6, 8, 192 / 6, 64 / 8, 219);
}
