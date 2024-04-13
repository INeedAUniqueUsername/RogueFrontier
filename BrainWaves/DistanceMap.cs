using Priority_Queue;
using System.Collections.Generic;
using System.Linq;

namespace BrainWaves;

class DistanceMap {
    public double this[(int x, int y) point] {
        get {
            if (distances.TryGetValue(point, out var d)) {
                return d;
            }
            Calculate(point);
            if (distances.TryGetValue(point, out d)) {
                return d;
            }
            return -1;
        }
    }
    public DistanceMap((int x, int y) center, Distance distanceFunction = null) {
        this.center = center;
        this.prev[center] = center;
        this.distanceFunction = distanceFunction ?? ((to, from, prev) => prev + 1);
        var d = distances[center] = this.distanceFunction(center, center, 0);
        pending.Enqueue(center, d);
    }
    public delegate double Distance((int x, int y) to, (int x, int y) from, double prevDistance);
    public Distance distanceFunction;
    public (int x, int y) center;
    public Dictionary<(int x, int y), (int x, int y)> prev = new Dictionary<(int, int), (int, int)>();
    public Dictionary<(int, int), double> distances = new Dictionary<(int, int), double>();
    public SimplePriorityQueue<(int x, int y), double> pending = new SimplePriorityQueue<(int, int), double>();

    public void Calculate((int x, int y) dest) {
        while (pending.Any()) {
            var p = pending.Dequeue();
            var d = distances[p];
            if (d > -1) {

                foreach (var next in new (int x, int y)[] {
                        (-1, 0), (1, 0), (0, -1), (0, 1)
                    }.Select(offset => (offset.x + p.x, offset.y + p.y))) {
                    var dist = distanceFunction(next, p, d);
                    if (!distances.TryGetValue(next, out var oldDist) || dist < oldDist) {
                        prev[next] = p;
                        distances[next] = dist;
                        pending.Enqueue(next, dist);
                    }
                }
            }
            if (dest == p) {
                return;
            }
        }
    }
}
