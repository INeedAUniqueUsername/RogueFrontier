using SadRogue.Primitives;
using SadConsole;
using System;

namespace IslandHopper;

public enum VoxelDefaults {
    Air, Grass
}
static class VoxelHelp {
    public static Voxel FromString(string name, Island World) {
        switch (name) {
            case "Air": return new Air();
            case "Grass": return new Grass(World);
            default: throw new Exception("Unknown voxel type");
        }
    }
    public static Voxel Create(VoxelDefaults v, Island World) {
        switch (v) {
            case VoxelDefaults.Air: return new Air();
            case VoxelDefaults.Grass: return new Grass(World);
            default: throw new Exception("Unknown voxel type");
        }
    }
}
public enum VoxelType {
    Empty,
    Floor,
    Solid
};
public interface Voxel {
    VoxelType Collision { get; }
    ColoredGlyph CharAbove { get; }
    ColoredGlyph CharCenter { get; }
}
public class Air : Voxel {
    public VoxelType Collision => VoxelType.Empty;

    public ColoredGlyph CharAbove => new(Color.White, Color.Transparent, 176);
    public ColoredGlyph CharCenter => CharAbove;
}
public class Grass : Voxel {
    public VoxelType Collision => VoxelType.Solid;
    public Color foreground { get; private set; }
    public Color background { get; private set; }
    public char glyph;
    private char[] symbols = {
            '"', '\'', 'w', 'v', ',', '.', '`',
        };
    public Grass(Island World) {
        Func<int, int> next = World.karma.NextInteger;

        foreground = new Color(next(102), 153, next(102));
        int r = next(26);
        background = new Color(r, next(26) + 13, 26 - r);
        glyph = symbols[next(symbols.Length)];
    }
    public ColoredGlyph CharAbove => new(foreground, background, glyph);
    public ColoredGlyph CharCenter => new(Color.Transparent, foreground, ' ');
}
public class Dirt : Voxel {
    public VoxelType Collision => VoxelType.Solid;
    public Color foreground { get; private set; } = Color.Brown;
    public Color background { get; private set; } = Color.Black;
    public char glyph = '=';
    public Dirt() {
    }
    public ColoredGlyph CharAbove => new(foreground, background, glyph);
    public ColoredGlyph CharCenter => new(Color.Transparent, foreground, ' ');
}
public class Floor : Voxel {
    public VoxelType Collision => VoxelType.Floor;
    public Color color { get; private set; }
    public Floor(Color c) {
        this.color = c;
    }
    public ColoredGlyph CharAbove => new(color, Color.Transparent, '.');
    public ColoredGlyph CharCenter => new(Color.Transparent, color, '+');
}
