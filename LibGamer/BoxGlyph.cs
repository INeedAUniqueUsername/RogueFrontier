using System;
using System.Collections.Generic;
using System.Text;

namespace LibGamer {
    public enum Line {
        None, Single, Double
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
            Action<int, BoxGlyph> AddPair = IBMCGA.AddPair;
            AddPair(179, new BoxGlyph { n = Line.Single, s = Line.Single });
            AddPair(180, new BoxGlyph { n = Line.Single, s = Line.Single, w = Line.Single });
            AddPair(181, new BoxGlyph { n = Line.Single, s = Line.Single, w = Line.Double });
            AddPair(182, new BoxGlyph { n = Line.Double, s = Line.Double, w = Line.Single });
            AddPair(183, new BoxGlyph { s = Line.Double, w = Line.Single });
            AddPair(184, new BoxGlyph { s = Line.Single, w = Line.Double });
            AddPair(185, new BoxGlyph { n = Line.Double, s = Line.Double, w = Line.Double });
            AddPair(186, new BoxGlyph { n = Line.Double, s = Line.Double });
            AddPair(187, new BoxGlyph { s = Line.Double, w = Line.Double });
            AddPair(188, new BoxGlyph { n = Line.Double, w = Line.Double });
            AddPair(189, new BoxGlyph { n = Line.Double, w = Line.Single });
            AddPair(190, new BoxGlyph { n = Line.Single, w = Line.Double });
            AddPair(191, new BoxGlyph { s = Line.Single, w = Line.Single });
            AddPair(192, new BoxGlyph { n = Line.Single, e = Line.Single });
            AddPair(193, new BoxGlyph { n = Line.Single, e = Line.Single, w = Line.Single });
            AddPair(194, new BoxGlyph { e = Line.Single, s = Line.Single, w = Line.Single });
            AddPair(195, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single });
            AddPair(196, new BoxGlyph { e = Line.Single, w = Line.Single });
            AddPair(197, new BoxGlyph { n = Line.Single, e = Line.Single, s = Line.Single, w = Line.Single });
            AddPair(198, new BoxGlyph { n = Line.Single, e = Line.Double, s = Line.Single });
            AddPair(199, new BoxGlyph { n = Line.Double, e = Line.Single, s = Line.Double });
            AddPair(200, new BoxGlyph { n = Line.Double, e = Line.Double });
            AddPair(201, new BoxGlyph { e = Line.Double, s = Line.Double });
            AddPair(202, new BoxGlyph { n = Line.Double, e = Line.Double, w = Line.Double });
            AddPair(203, new BoxGlyph { e = Line.Double, s = Line.Double, w = Line.Double });
            AddPair(204, new BoxGlyph { n = Line.Double, e = Line.Double, s = Line.Double });
            AddPair(205, new BoxGlyph { e = Line.Double, w = Line.Double });
            AddPair(206, new BoxGlyph { n = Line.Double, e = Line.Double, s = Line.Double, w = Line.Double });
            AddPair(207, new BoxGlyph { n = Line.Single, e = Line.Double, w = Line.Double });
            AddPair(208, new BoxGlyph { n = Line.Double, e = Line.Single, w = Line.Single });
            AddPair(209, new BoxGlyph { e = Line.Double, s = Line.Single, w = Line.Double });
            AddPair(210, new BoxGlyph { e = Line.Single, s = Line.Double, w = Line.Single });
            AddPair(211, new BoxGlyph { n = Line.Double, e = Line.Single });
            AddPair(212, new BoxGlyph { n = Line.Single, e = Line.Double });
            AddPair(213, new BoxGlyph { e = Line.Double, s = Line.Single });
            AddPair(214, new BoxGlyph { e = Line.Single, s = Line.Double });
            AddPair(215, new BoxGlyph { n = Line.Double, e = Line.Single, s = Line.Double, w = Line.Single });
            AddPair(216, new BoxGlyph { n = Line.Single, e = Line.Double, s = Line.Single, w = Line.Double });
            AddPair(217, new BoxGlyph { n = Line.Single, w = Line.Single });
            AddPair(218, new BoxGlyph { e = Line.Single, s = Line.Single });
        }
        void AddPair(int c, BoxGlyph info) {
            glyphToInfo[c] = info;
            glyphFromInfo[info] = c;
        }
    }
}
