using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using LibGamer;
using static LibGamer.ABGR;
namespace RogueFrontier;

//A space background made up of randomly generated layers with different depths
public class Backdrop {
    public List<GeneratedLayer> layers;
    public CompositeColorLayer starlight;
    public GridLayer planets;
    public GridLayer orbits;
    public GridLayer nebulae;
    public Backdrop() {

        Rand r = new Rand();
        int layerCount = 5;
        layers = new List<GeneratedLayer>(layerCount);
        for (int i = 0; i < layerCount; i++) {
            var layer = new GeneratedLayer(1f / (i * i * 1.5 + i + 1), r);
            layers.Insert(0, layer);
        }
        planets = new(1);
        orbits = new(1);
        nebulae = new(1);
        starlight = new();
    }
    public uint GetBackgroundFixed(XY point) => GetBackground(point, XY.Zero);
    public uint GetBackground(XY point, XY camera) {
        //ColoredGlyph result = new ColoredGlyph(Color.Transparent, Color.Black, ' ');
        var b = Transparent;
        layers.Reverse<GeneratedLayer>()
            .ToList()
            .ForEach(l => BlendBack(l.GetBackground(point, camera)));
        void BlendBack(uint c) {
            if (A(c) > 0) {
                b = Blend(Premultiply(b),c);
            }
        }
        BlendBack(starlight.GetBackgroundFixed(point));
		BlendBack(orbits.GetBackground(point, camera));
		BlendBack(planets.GetBackground(point, camera));
		BlendBack(nebulae.GetBackground(point, camera));
        return b;
    }
    public Tile GetTile(XY point, XY camera) {
        //ColoredGlyph result = new ColoredGlyph(Color.Transparent, Color.Black, ' ');
        var (f, b, g) = (Transparent, Transparent, 0u);

        for (int i = layers.Count - 1; i > -1; i--) {
            Blend(layers[i].GetTile(point, camera));
        }
        void Blend(Tile front) {
            b = ABGR.Blend(Premultiply(b), front.Background);
            if (front.Glyph != ' ' && front.Glyph != 0) {
                f = front.Foreground;
                g = front.Glyph;
            }
        }
        void BlendBack(uint back) {
            if (A(back) > 0) {
                b = ABGR.Blend(Premultiply(b), back);
            }
        }
        BlendBack(starlight.GetBackgroundFixed(point));
        Blend(planets.GetTile(point, camera));
        Blend(nebulae.GetTile(point, camera));
        return new Tile(f, b, g);
    }
    public Tile GetTileFixed (XY point) => GetTile(point, XY.Zero);
}
public interface ILayer {
	Tile GetTile (XY point, XY camera);
}
public class GridLayer : ILayer {
    public double parallaxFactor { get; private set; }
    public Dictionary<(int, int), Tile> tiles;
    public GridLayer() { }
    public GridLayer(double parallaxFactor) {
        this.parallaxFactor = parallaxFactor;
        this.tiles = new Dictionary<(int, int), Tile>();
    }
    public Tile GetTile (XY point, XY camera) {
        var apparent = point - camera * (1 - parallaxFactor);
        return tiles.TryGetValue(apparent.roundDown, out var result) ? result : new Tile(Transparent, Transparent, ' ');
    }
    public uint GetBackground(XY point, XY camera) {
        var apparent = point - camera * (1 - parallaxFactor);
        return tiles.TryGetValue(apparent.roundDown, out var result) ? result.Background : Transparent;
    }
}
public class CompositeLayer : ILayer {
    public List<ILayer> layers = new List<ILayer>();
    public CompositeLayer() { }
    public uint GetBackgroundFixed(XY point) => GetBackground(point, XY.Zero);
    public uint GetBackground (XY point, XY camera) {
		uint result = Black;
        foreach (var layer in layers) {
            result = Blend(result, layer.GetTile(point, camera).Background);
        }
        return result;
    }
    public Tile GetTile(XY point, XY camera) {
        if (layers.Any()) {
            var top = layers.Last().GetTile(point, camera);
            var g = top.Glyph;
            var b = top.Background;
            var f = top.Foreground;
            for (int i = layers.Count - 2; i > -1; i--) {
                BlendTile(layers[i].GetTile(point, camera));
            }
            void BlendTile(Tile tile) {
                b = Blend(Premultiply(b),tile.Background);
                if (g == ' ' || g == 0) {
                    if (tile.Glyph != ' ' && tile.Glyph != 0) {
                        f = tile.Foreground;
                        g = tile.Glyph;
                    }
                }
            }
            return new Tile(f, b, g);
        } else {
            return new Tile(Transparent, Transparent, ' ');
        }
    }
    public Tile GetTileFixed(XY point) => GetTile(point, XY.Zero);
}

public class CompositeColorLayer {
    Rect active = new();

    private List<GeneratedGrid<uint>> layers = new List<GeneratedGrid<uint>>();
    public CompositeColorLayer() { }
    public void AddLayer(int index, GeneratedGrid<uint> layer, Rect area) {
        layers.Insert(index, layer);
        active = active.Union(area);
    }

    public uint GetBackgroundFixed(XY point) {
        uint result = Transparent;
        if (active.Contains(point)) {
            foreach (var layer in layers.Reverse<GeneratedGrid<uint>>()) {
                var apparent = point.roundDown;
                result = Blend(Premultiply(result), layer[apparent.xi, apparent.yi]);
            }
        }
        return result;
    }
}
public class SpaceGenerator : IGridGenerator<Tile> {
    public GeneratedLayer layer;
    public Rand random;
    public SpaceGenerator() { }
    public SpaceGenerator(GeneratedLayer layer, Rand random) {
        this.layer = layer;
        this.random = random;
    }
    public Tile Generate((long, long) p) {
        var tiles = layer.tiles;
        var parallaxFactor = layer.parallaxFactor;

        var (x, y) = p;
        var value = random.NextInteger(51);
        var (r, g, b) = (value, value, value + random.NextInteger(25));

        var init = new XY[] {
                    new XY(-1, -1),
                    new XY(-1, 0),
                    new XY(-1, 1),
                    new XY(0, -1),
                    new XY(0, 1),
                    new XY(1, -1),
                    new XY(1, 0),
                    new XY(1, 1),}.Select(xy => new XY(xy.xi + x, xy.yi + y)).Where(xy => tiles.IsInit(xy.xi, xy.yi));

        var count = init.Count() + 1;
        foreach (var xy in init) {
            ABGR t = tiles.Get(xy.xi, xy.yi).Background;
            (r, g, b) = (r + R(t), g + G(t), b + B(t));
        }
        (r, g, b) = (r / count, g / count, b / count);
        var a = (byte)random.NextInteger(25, 104);
        var background = RGBA((byte)r, (byte)g, (byte)b, a);

        if (random.NextDouble() * 100 < (1 / (parallaxFactor + 1))) {
            const string vwls = "?&%~=+;";
            var star = vwls[random.NextInteger(vwls.Length)];
            var foreground = RGBA(255, (byte)random.NextInteger(204, 230), (byte)random.NextInteger(204, 230), (byte)(225 * Sqrt(parallaxFactor)));
            return new Tile(foreground, background, star);
        } else {
            return new Tile(Transparent, background, ' ');
        }
    }
}
public class GeneratedLayer : ILayer {
    public double parallaxFactor { get; private set; }                   //Multiply the camera by this value
    public GeneratedGrid<Tile> tiles;  //Dynamically generated grid of tiles
    public GeneratedLayer() {
    }
    public GeneratedLayer(double parallaxFactor, GeneratedGrid<Tile> tiles) {
        this.parallaxFactor = parallaxFactor;
        this.tiles = tiles;
    }
    public GeneratedLayer(double parallaxFactor, Rand random) {
        //Random r = new Random();
        this.parallaxFactor = parallaxFactor;
        tiles = new(new SpaceGenerator(this, random));
    }
    public Tile GetTile(XY point, XY camera) {
        var apparent = point - camera * (1 - parallaxFactor);
        apparent = apparent.roundDown;
        return tiles[apparent.xi, apparent.yi];
    }
    public uint GetBackground(XY point, XY camera) => GetTile(point, camera).Background;
    public Tile GetTileFixed(XY point) {
        point = point.roundDown;
        return tiles[point.xi, point.yi];
    }
}
