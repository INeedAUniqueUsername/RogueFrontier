using Common;
using System.Collections.Generic;

namespace IslandHopper;

class Vision {
    public enum Direction {
        n = 0,
        ne = 1,
        e = 2,
        se = 3,
        s = 4,
        sw = 5,
        w = 6,
        nw = 7
    }
    public static int DirectionCount = 8;
    public Dictionary<Direction, XYZ> offsets = new Dictionary<Direction, XYZ> {
            { Direction.n, new XYZ(0, 1)},
            { Direction.ne, new XYZ(1, 1)},
            { Direction.e, new XYZ(1, 0)},
            { Direction.se, new XYZ(1, -1)},
            { Direction.s, new XYZ(0, -1)},
            { Direction.sw, new XYZ(-1, -1)},
            { Direction.w, new XYZ(-1, 0)},
            { Direction.nw, new XYZ(-1, 1)},
        };
    public Vision(Entity source) {
        HashSet<(int, int, int)> visible = new HashSet<(int, int, int)>();
        HashSet<(int, int, int)> visited = new HashSet<(int, int, int)>();
        Queue<Check> checks = new Queue<Check>();
        var center = source.Position;
        checks.Enqueue(new Check(center, Direction.n, Direction.ne, Direction.e, Direction.se, Direction.s, Direction.sw, Direction.w, Direction.nw));
        while (checks.Count > 0) {
            var check = checks.Dequeue();
            var pos = check.pos;
            if (CanSee(pos)) {
                visible.Add(pos);
                foreach (var direction in check.spread) {
                    Direction[] nextDirections = new Direction[0];
                    switch (direction) {
                        case Direction.n:
                        case Direction.e:
                        case Direction.s:
                        case Direction.w:
                            nextDirections = new Direction[] {
                                    direction
                                };
                            break;
                        case Direction.ne:
                        case Direction.se:
                        case Direction.sw:
                        case Direction.nw:
                            nextDirections = new Direction[] {
                                    (Direction)(((int) direction + DirectionCount - 1) % DirectionCount),
                                    (direction),
                                    (Direction)(((int) direction + 1) % DirectionCount),
                                };
                            break;
                    }

                    checks.Enqueue(new Check(pos + offsets[direction], nextDirections));
                }
            }
            visited.Add(pos);
        }


        bool CanSee(XYZ point) {
            return true;
        }
    }
    class Check {
        public XYZ pos;
        public HashSet<Direction> spread;
        public Check(XYZ pos, params Direction[] next) {
            this.pos = pos;
            this.spread = new HashSet<Direction>(next);
        }
    }
}
