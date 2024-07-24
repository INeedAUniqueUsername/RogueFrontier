using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using LibGamer;
using Common;

namespace ASECII {
    public class Sprite {
        public List<Layer> layers = new();
        public Point origin;
        public Point end;
        public Dictionary<(int X, int Y), TileValue> preview = new();

        
		[IgnoreDataMember]
        public HashSet<string> exportDataGd {
            get {
				string ToLine (KeyValuePair<(int X, int Y), TileValue> pair) {

					string Hex (uint abgr) => ABGR.ToRGBA(abgr).ToString("X8");
					var (X, Y) = pair.Key;
					var (F, B, G) = pair.Value.Data;
					return string.Join(' ', [X, Y, Hex(F), Hex(B), G]);
				}
                return [.. from p in preview select ToLine(p)];
			}
        }

        public static TileValue empty => new TileValue(Color.Transparent, Color.Transparent, 0);
        public Sprite() {}
        public bool InRect(Point pos) => pos.X >= origin.X && pos.Y >= origin.Y && pos.X <= end.X && pos.Y <= end.Y;
        public void UpdatePreview() {
            preview.Clear();
            foreach(var layer in layers) {
                if (!layer.visible) {
                    continue;
                }
                Action<TileRef, TileValue> modifier = null;
                if(layer.applyBackground) {
                    modifier += (t, current) => {
                        if (t.Background.A != 0) {
                            current.Background = t.Background.Blend(current.Background);
                        }
                    };
                }
                if (layer.applyForeground) {
                    modifier += (t, current) => {
                        if (t.Foreground.A != 0) {
                            current.Foreground = t.Foreground.Blend(current.Foreground);
                        }
                    };
                }
                if(layer.applyGlyph) {
                    modifier += (t, current) => {
                        if (t.Glyph != 0) {
                            current.Glyph = t.Glyph;
                        }
                    };
                }
                if(modifier == null) {
                    continue;
                }
                foreach(var (point, tile) in layer.cells) {
                    var (x, y) = point + layer.pos;
                    if (!preview.TryGetValue((x, y), out var current)) {
                        current = preview[(x, y)] = new TileValue(Color.Transparent, Color.Transparent, 0);
                    }
                    //TileValue t = new TileValue(tile.Foreground, tile.Background, tile.Glyph);
                    var t = tile;
                    modifier?.Invoke(t, current);
                }
            }
            if (preview.Any()) {
                origin = new Point(preview.Keys.Min(k => k.Item1), preview.Keys.Min(k => k.Item2));
                end = new Point(preview.Keys.Max(k => k.Item1), preview.Keys.Max(k => k.Item2));
            } else {
                origin = end = new Point(0, 0);
            }
        }
        public void GetIntermediate((int, int) pos, int topLayer) {
            TileValue current = new TileValue(Color.Transparent, Color.Transparent, 0);
            foreach (var layer in layers.Take(topLayer)) {
                if (!layer.visible) {
                    continue;
                }
                Action<TileRef, TileValue> modifier = null;
                if (layer.applyBackground) {
                    modifier += (t, current) => {
                        if (t.Background.A != 0) {
                            current.Background = t.Background.Blend(current.Background);
                        }
                    };
                }
                if (layer.applyForeground) {
                    modifier += (t, current) => {
                        if (t.Foreground.A != 0) {
                            current.Foreground = t.Foreground.Blend(current.Foreground);
                        }
                    };
                }
                if (layer.applyGlyph) {
                    modifier += (t, current) => {
                        if (t.Glyph != 0) {
                            current.Glyph = t.Glyph;
                        }
                    };
                }
                if (modifier == null) {
                    continue;
                }
                if(layer.cells.TryGetValue(pos - layer.pos, out var t)) {
                    modifier?.Invoke(t, current);
                }
            }
            if (preview.Any()) {
                origin = new Point(preview.Keys.Min(k => k.Item1), preview.Keys.Min(k => k.Item2));
                end = new Point(preview.Keys.Max(k => k.Item1), preview.Keys.Max(k => k.Item2));
            } else {
                origin = end = new Point(0, 0);
            }
        }

    }
    public class Layer {
        public Point pos = new(0, 0);
        public bool visible = true, applyGlyph = true, applyForeground = true, applyBackground = true;
        public Dictionary<(int, int), TileRef> cells = new();

        public string name = "Layer 0";
        public Layer() {}
        public Layer(Layer copy) {
            pos = (copy.pos.X, copy.pos.Y);
            visible = copy.visible;
            applyGlyph = copy.applyGlyph;
            applyForeground = copy.applyForeground;
            applyBackground = copy.applyBackground;
            cells = new(copy.cells);
            name = $"{copy.name} Copy";
        }
        public static (Color, Color, int)? Triple(TileRef t) => t != null ? (t.Foreground, t.Background, t.Glyph) : (Color.Transparent, Color.Transparent, 0);
        public HashSet<Point> GetGlobalFill((Color, Color, int)? source, Point origin, Point end) {
            HashSet<Point> affected = new HashSet<Point>();
            foreach (var p in new Rectangle(origin, end).Positions()) {
                var pt = Triple(this[p]);
                if (pt == source) {
                    affected.Add(p);
                }
            }
            return affected;
        }

        public HashSet<Point> GetGrowFill((int, int) start, (Color, Color, int)? brush, Point origin, Point end) {
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            HashSet<Point> affected = new HashSet<Point>();
            Queue<(int, int)> next = new Queue<(int, int)>();

            next.Enqueue(start);
            while (next.Any()) {
                (int x, int y) = next.Dequeue();

                {
                    var p = (x, y);
                    var c = Triple(this[p]);

                    if (new List<(int, int)>() {
                                (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)
                    }.All(p => Triple(this[p]) != brush)) {
                        continue;
                    }
                    affected.Add(p);
                }

                foreach ((int x, int y) p in new List<(int, int)>() {
                            (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) }
                ) {

                    if (visited.Contains(p)) {
                        continue;
                    }

                    visited.Add(p);

                    if (p.x < origin.X || p.y < origin.Y || p.x > end.X || p.y > end.Y) {
                        continue;
                    }
                    next.Enqueue(p);
                }
            }
            return affected;
        }
        public HashSet<Point> GetOutlineFill((int, int) start, (Color, Color, int)? source, Point origin, Point end) {
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            HashSet<Point> affected = new HashSet<Point>();
            Queue<(int, int)> next = new Queue<(int, int)>();

            next.Enqueue(start);
            while (next.Any()) {
                (int x, int y) = next.Dequeue();

                {
                    var p = (x, y);
                    var c = Triple(this[p]);

                    if (c != source) {
                        continue;
                    }
                    if (new List<(int, int)>() {
                                (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)
                    }.Any(p => Triple(this[p]) != source)) {
                        affected.Add(p);
                    }
                }

                foreach ((int x, int y) p in new List<(int, int)>() {
                                (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) }
                    ) {

                    if (visited.Contains(p)) {
                        continue;
                    }

                    visited.Add(p);

                    if (p.x < origin.X || p.y < origin.Y || p.x > end.X || p.y > end.Y) {
                        continue;
                    }
                    next.Enqueue(p);
                }
            }
            return affected;
        }
        public HashSet<Point> GetFloodFill((int, int) start, (Color, Color, int)? source, Point origin, Point end) {
            HashSet<(int, int)> visited = new HashSet<(int, int)>();
            HashSet<Point> affected = new HashSet<Point>();
            Queue<(int, int)> next = new Queue<(int, int)>();

            next.Enqueue(start);
            while (next.Any()) {
                (int x, int y) = next.Dequeue();

                {
                    var p = (x, y);
                    var c = Triple(this[p]);

                    if (c != source) {
                        continue;
                    }

                    affected.Add(p);
                }

                foreach ((int x, int y) p in new List<(int, int)>() {
                                (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) }
                    ) {

                    if (visited.Contains(p)) {
                        continue;
                    }

                    visited.Add(p);

                    if (p.x < origin.X || p.y < origin.Y || p.x > end.X || p.y > end.Y) {
                        continue;
                    }
                    next.Enqueue(p);
                }
            }
            return affected;
        }

        //https://www.cs.uic.edu/~jbell/CourseNotes/ComputerGraphics/PolygonFilling.html
        //http://www.eecs.umich.edu/courses/eecs380/HANDOUTS/PROJ2/InsidePoly.html
        public HashSet<Point> GetBoundedFill(HashSet<(int x, int y)> bounds) {
            HashSet<Point> affected = new HashSet<Point>();

            var lines = bounds.GroupBy(p => p.y)
                .ToDictionary(l => l.Key,
                              l => l.OrderBy(p => p.x).ToList());
            foreach((var y, var points) in lines) {
                
                for (int i = 0; i < points.Count - 1; i++) {

                    var left = points[i];
                    var right = points[i + 1];
                    if(left.x + 1 == right.x) {
                        continue;
                    }
                    
                    for(int x = left.x + 1; x < right.x; x++) {
                        affected.Add(new Point(x, y));
                    }
                    i++;
                }
                /*
                for (int i = 0; i < points.Count - 1; i += 2) {
                    var left = points[i];
                    var right = points[i + 1];
                    for (int x = left.x + 1; x < right.x; x++) {
                        affected.Add(new Point(x, y));
                    }
                }
                */
            }
            /*
            lines = bounds.GroupBy(p => p.x)
                .ToDictionary(l => l.Key,
                              l => l.OrderBy(p => p.y).ToList());
            foreach ((var x, var points) in lines) {
                for (int i = 0; i < points.Count - 1; i += 2) {
                    var lower = points[i];
                    var upper = points[i + 1];
                    for (int y = lower.y + 1; y < upper.y; y++) {
                        affected.Add(new Point(x, y));
                    }
                }
            }
            */

            return affected;
        }

        public TileRef this[Point p] {
            get => cells.TryGetValue(p - pos, out TileRef cg) ? cg : null;
            set {
                if (value == null) cells.Remove(p - pos);
                else cells[p - pos] = value;
            }
        }
        public void Zero() {
            Dictionary<(int, int), TileRef> updated = new Dictionary<(int, int), TileRef>();
            foreach (var (point, tile) in cells) {
                updated[point + pos] = tile;
            }
            cells = updated;
            pos = new Point(0, 0);
        }
        //To do: Test with both layers moved
        public void Flatten(Layer from) {
            foreach ((var p, var t) in from.cells) {
                if(this[p + from.pos] is TileValue tv) {
                    if(from.applyGlyph) {
                        tv.Glyph = t.Glyph;
                    }
                    if(from.applyBackground) {
                        tv.Background = t.Background;
                    }
                    if(from.applyForeground) {
                        tv.Foreground = t.Foreground;
                    }
                } else if(from.applyGlyph && from.applyBackground && from.applyForeground) {
					this[p + from.pos] = t;
				}
            }
        }
    }

    public interface TileRef {
        Color Foreground { get; }
        Color Background { get; }
        int Glyph { get; }
        ColoredGlyph cg { get; }
    }
    public class TileIndex : TileRef {
        [IgnoreDataMember]
        public Color Foreground => cg.Foreground;
        [IgnoreDataMember]
        public Color Background => cg.Background;
        [IgnoreDataMember]
        public int Glyph => cg.Glyph;
        [IgnoreDataMember]
        public ColoredGlyph cg => tiles.tiles[index];

        public TileModel tiles;
        public int index;
        public TileIndex(TileModel tiles, int index) {
            this.tiles = tiles;
            this.index = index;
        }
    }
    public class TilePalette : TileRef {
        [IgnoreDataMember]
        public Color Foreground => palette.palette[foregroundIndex];
        [IgnoreDataMember]
        public Color Background => palette.palette[backgroundIndex];
        [IgnoreDataMember]
        public ColoredGlyph cg => new ColoredGlyph(Foreground, Background, Glyph);

        public PaletteModel palette;
        public int foregroundIndex;
        public int backgroundIndex;
        public int Glyph { get; set; }
        public TilePalette(PaletteModel palette, int foregroundIndex, int backgroundIndex, int Glyph) {
            this.palette = palette;
            this.foregroundIndex = foregroundIndex;
            this.backgroundIndex = backgroundIndex;
            this.Glyph = Glyph;
        }
    }
    [DataContract]
    public class TileValue : TileRef {
        [DataMember]
        public Color Foreground { get; set; }
        [DataMember]
        public Color Background { get; set; }
        [DataMember]
        public int Glyph { get; set; }
        [IgnoreDataMember]
        public ColoredGlyph cg => new ColoredGlyph(Foreground, Background, Glyph);
        public TileValue(Color Foreground, Color Background, int Glyph) {
            this.Foreground = Foreground;
            this.Background = Background;
            this.Glyph = Glyph;
        }

        public (uint F, uint B, int G) Data => (Foreground.PackedValue, Background.PackedValue, Glyph);
        public static implicit operator ColoredGlyph(TileValue tv) => new ColoredGlyph(tv.Foreground, tv.Background, tv.Glyph);
    }

    public class NotepadTile : TileRef {
        public Color Foreground => Color.Black;
        public Color Background => Color.White;
        public int Glyph { get; set; }
        public ColoredGlyph cg => new ColoredGlyph(Foreground, Background, Glyph);
        public NotepadTile(int Glyph) => this.Glyph = Glyph;
    }
}
