using Common;
using SadConsole;
using System.Collections.Generic;
using SadRogue.Primitives;
using Priority_Queue;
using System.Linq;

namespace BrainWaves;

public class Player : Entity {
    public int psychicEnergy;

    public HashSet<(int, int)> seen = new HashSet<(int, int)>();
    public HashSet<(int, int)> visible = new HashSet<(int, int)>();
    public HashSet<(int, int)> apparentEnemyVision = new HashSet<(int, int)>();
    public DictCounter<string> messages = new DictCounter<string>();


    public World World { get; set; }
    public XY Position { get; set; }


    public ColoredGlyph Tile { get => new ColoredGlyph(World.brightness.Get(Position) < 128 ? Color.White : Color.Black, Color.Transparent, '@'); }

    public bool Active { get; set; } = true;
    public Player(World World, XY Position) {
        this.World = World;
        this.Position = Position;
    }
    public void UpdateVisible() {
        visible.Clear();
        visible.Add(Position);
        HashSet<(int, int)> known = new HashSet<(int, int)>();
        known.Add(Position);
        SimplePriorityQueue<XY, double> q = new SimplePriorityQueue<XY, double>();
        q.Enqueue(Position, 0);
        while (q.Count > 0) {
            var p = q.Dequeue();

            if (((Entity)this).IsVisible(visible, p)) {
                visible.Add(p);
                foreach (var offset in new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }) {
                    var next = p + offset;
                    if (known.Add(next)) {
                        q.Enqueue(next, (next - Position).magnitude);
                    }
                }
            }
        }
        ((Entity)this).RemoveDark(visible);
    }
    public void UpdateStep() {
        messages.dict.Clear();
        UpdateVisible();

        apparentEnemyVision.Clear();
        foreach (var e in World.entities.all.OfType<Guard>().Where(g => visible.Contains(g.Position))) {
            foreach (var p in e.visible) {
                apparentEnemyVision.Add(p);
            }
        }
        if (psychicEnergy < 30) {
            psychicEnergy++;
        }
    }
    public bool Move(XY dest) {
        if (World.voxels.Get(dest) is Floor) {
            var e = World.entities[dest].FirstOrDefault();
            switch (e) {
                case null:
                    Position = dest;
                    break;
                case Guard g:
                    g.pushed = true;
                    g.distanceMap = new DistanceMap(visible.OrderBy(p => (Position - p).magnitude2).Last(), g.distanceMap.distanceFunction);
                    g.distanceMap.Calculate(g.Position);
                    break;
                case Light l:
                    foreach (var p in l.contribution.Keys) {
                        World.brightness[p] -= l.contribution[p];
                    }
                    World.RemoveEntity(l);
                    break;
            }
            return true;
        }
        return false;
    }
}
