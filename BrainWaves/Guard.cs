using Common;
using SadConsole;
using System.Collections.Generic;
using SadRogue.Primitives;
using Priority_Queue;
using System.Linq;

namespace BrainWaves;

class Guard : Entity {
    public HashSet<(int, int)> visible = new HashSet<(int, int)>();
    public World World { get; set; }
    public XY Position { get; set; }

    public bool pushed;
    public Player target;
    public DistanceMap distanceMap;


    public ColoredGlyph Tile { get => new ColoredGlyph(World.brightness.Get(Position) < 128 ? Color.White : Color.Black, Color.Transparent, 'G'); }

    public bool Active { get; set; } = true;
    public Guard(World World, XY Position) {
        this.World = World;
        this.Position = Position;
        distanceMap = new DistanceMap(Position);
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
        UpdateVisible();
        if (pushed) {
            var dest = distanceMap.prev[Position];
            if (((int, int))Position == dest) {
                pushed = false;
            } else {
                Position = new XY(dest.x, dest.y);
            }
        } else {
            if (target == null) {
                foreach (var p in visible) {
                    target = World.entities[p].OfType<Player>().FirstOrDefault();
                    if (target != null) {
                        var playerPos = p;
                        if (playerPos != distanceMap.center) {
                            var distanceMap = new DistanceMap(playerPos, this.distanceMap.distanceFunction);
                            if (distanceMap[Position] > -1) {
                                this.distanceMap = distanceMap;
                            }
                        }
                        break;
                    }
                }
            } else if (visible.Contains(target.Position)) {
                if (((int, int))target.Position != distanceMap.center) {
                    var distanceMap = new DistanceMap(target.Position, this.distanceMap.distanceFunction);
                    if (distanceMap[Position] > -1) {
                        this.distanceMap = distanceMap;
                    }
                }
            } else if ((Position - distanceMap.center).magnitude < 10) {

            }
            var dest = distanceMap.prev[Position];
            Position = new XY(dest.x, dest.y);
        }
    }
    public int tick;
    public void UpdateRealtime() {
        tick++;
    }
}
