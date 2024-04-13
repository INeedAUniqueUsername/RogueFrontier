using Common;
using Priority_Queue;
using SadConsole;
using System;
using System.Collections.Generic;
using SadRogue.Primitives;
namespace BrainWaves;

class Light : Entity {
    public World World { get; set; }
    public XY Position { get; set; }

    public ColoredGlyph Tile => new ColoredGlyph(World.brightness[Position] < 128 ? Color.White : Color.Black, Color.Transparent, '*');

    public bool Active { get; set; } = true;

    HashSet<(int, int)> visible = new HashSet<(int, int)>();
    public Dictionary<(int, int), byte> contribution = new Dictionary<(int, int), byte>();
    public Light(World World, XY Position) {
        this.World = World;
        this.Position = Position;
    }
    public void UpdateLight() {
        visible.Clear();
        visible.Add(Position);
        HashSet<(int, int)> known = new HashSet<(int, int)>();
        SimplePriorityQueue<XY, double> q = new SimplePriorityQueue<XY, double>();
        q.Enqueue(Position, 0);
        while (q.Count > 0) {
            var p = q.Dequeue();

            if (IsVisible(p)) {
                visible.Add(p);
                foreach (var offset in new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }) {
                    var next = p + offset;
                    if (known.Add(next)) {
                        q.Enqueue(next, (next - Position).magnitude);
                    }
                }
            }
        }
        bool IsVisible(XY p) {
            var prev = p + (Position - p).normal;
            prev = prev.round;
            return visible.Contains(prev) && (World.voxels[prev] is Floor);
        }

        foreach (var p in visible) {
            ref byte b = ref World.brightness[p];
            var prev = b;
            var d = (Position - p).magnitude2 / 3;
            if (d < 1) {
                b = 255;
            } else {
                b = (byte)Math.Min(255, b + (255 / d));
            }
            contribution[p] = (byte)(b - prev);
        }
    }

    public void UpdateStep() {
    }
}
