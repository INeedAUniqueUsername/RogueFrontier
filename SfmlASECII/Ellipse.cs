using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SadRogue.Primitives;
namespace ASECII {
    public class Ellipse {
        public int X, Y, Width, Height;

        public Ellipse(int X, int Y, int Width, int Height) {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;

        }
        public Point Center => new Point(X + Width / 2, Y + Height / 2);
        public bool IsEmpty => Width == 0 && Height == 0;
        public double Area => Math.PI * Width * Height;
        public Point Position => new Point(X, Y);
        public Point Size => new Point(Width, Height);
        public static Ellipse Empty => new Ellipse(0, 0, 0, 0);
        public Ellipse Translate(Point p) => new Ellipse(X - p.X, Y - p.Y, Width, Height);
        public IEnumerable<Point> Positions() {

            var halfWidth = Width / 2;
            var halfHeight = Height / 2;
            
            return new Rectangle(X, Y, Width, Height).Positions().Where(p => {

                if (p == Center) {
                    return true;
                } else {
                    var offset = p - Center;
                    var angle = Math.Atan2(offset.Y, offset.X);
                    
                    var x = Math.Cos(angle) * halfWidth;
                    var y = Math.Sin(angle) * halfHeight;
                    
                    return Math.Abs(offset.X) <= Math.Abs(x) && Math.Abs(offset.Y) <= Math.Abs(y);
                }
            });
        }

    }
}
