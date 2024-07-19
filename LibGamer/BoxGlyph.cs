using System;
using System.Collections.Generic;
using System.Text;

namespace LibGamer {
    public enum Line {
        None = 0, Single = 1, Double = 2
    }
    public struct BoxGlyph {
        public Line n, e, s, w;
        public BoxGlyph(Line n = Line.None, Line e = Line.None, Line s = Line.None, Line w = Line.None) {
            this.n = n;
            this.e = e;
            this.s = s;
            this.w = w;
        }
    }
    public enum BoxLines {
        n = 1, nn = 2,
        e = 4, ee = 8,
        s = 16, ss = 32,
        w = 64, ww = 128
    }
    public class BoxInfo {
        public Dictionary<int, BoxGlyph> glyphToInfo = new Dictionary<int, BoxGlyph>();
        public Dictionary<BoxGlyph, int> glyphFromInfo = new Dictionary<BoxGlyph, int>();
        public static BoxInfo IBMCGA;
        static BoxInfo() {
            IBMCGA = new BoxInfo();
            var AddPair = IBMCGA.AddPair;
			
			(int, BoxGlyph)[] pairs = [
				(179, new() { n = Line.Single, s = Line.Single }),
				(180, new BoxGlyph { n = Line.Single, s = Line.Single, w = Line.Single }),

				(181, new BoxGlyph { n = Line.Single, s = Line.Single, w = Line.Double }),
				(182, new BoxGlyph { n = Line.Double, s = Line.Double, w = Line.Single }),
				(183, new BoxGlyph { s = Line.Double, w = Line.Single }),
				(184, new BoxGlyph { s = Line.Single, w = Line.Double }),
				(185, new BoxGlyph { n = Line.Double, s = Line.Double, w = Line.Double }),
				(186, new BoxGlyph { n = Line.Double, s = Line.Double }),
				(187, new BoxGlyph { s = Line.Double, w = Line.Double }),
				(188, new BoxGlyph { n = Line.Double, w = Line.Double }),
				(189, new BoxGlyph { n = Line.Double, w = Line.Single }),
				(190, new BoxGlyph { n = Line.Single, w = Line.Double }),
				(191, new BoxGlyph { s = Line.Single, w = Line.Single }),
				(192, new BoxGlyph { n = Line.Single, e = Line.Single }),
				(193, new BoxGlyph { n = Line.Single, e = Line.Single, w = Line.Single }),
				(194, new BoxGlyph { e = Line.Single, s = Line.Single, w = Line.Single }),
				(195, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single }),
				(196, new BoxGlyph { e = Line.Single, w = Line.Single }),
				(197, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single, w = Line.Single }),
				(198, new BoxGlyph { n = Line.Single, e = Line.Double, s = Line.Single }),
				(199, new BoxGlyph { n = Line.Double, e = Line.Single, s = Line.Double }),
				(200, new BoxGlyph { n = Line.Double, e = Line.Double }),
				(201, new BoxGlyph { e = Line.Double, s = Line.Double }),
				(202, new BoxGlyph { n = Line.Double, e = Line.Double, w = Line.Double }),
				(203, new BoxGlyph { e = Line.Double, s = Line.Double, w = Line.Double }),
				(204, new BoxGlyph { n = Line.Double, e = Line.Double, s = Line.Double }),
				(205, new BoxGlyph { e = Line.Double, w = Line.Double }),
				(206, new BoxGlyph { n = Line.Double, e = Line.Double, s = Line.Double, w = Line.Double }),
				(207, new BoxGlyph { n = Line.Single, e = Line.Double, w = Line.Double }),
				(208, new BoxGlyph { n = Line.Double, e = Line.Single, w = Line.Single }),
				(209, new BoxGlyph { e = Line.Double, s = Line.Single, w = Line.Double }),
				(210, new BoxGlyph { e = Line.Single, s = Line.Double, w = Line.Single }),
				(211, new BoxGlyph { n = Line.Double, e = Line.Single }),
				(212, new BoxGlyph { n = Line.Single, e = Line.Double }),
				(213, new BoxGlyph { e = Line.Double, s = Line.Single }),
				(214, new BoxGlyph { e = Line.Single, s = Line.Double }),
				(215, new BoxGlyph { n = Line.Double, e = Line.Single, s = Line.Double, w = Line.Single }),
				(216, new BoxGlyph { n = Line.Single, e = Line.Double, s = Line.Single, w = Line.Double }),
				(217, new BoxGlyph { n = Line.Single, w = Line.Single }),
				(218, new BoxGlyph { e = Line.Single, s = Line.Single }),
				];
			foreach(var p in pairs) AddPair(p.Item1, p.Item2);
        }
        void AddPair(int c, BoxGlyph info) {
            glyphToInfo[c] = info;
            glyphFromInfo[info] = c;
        }
    }
}
