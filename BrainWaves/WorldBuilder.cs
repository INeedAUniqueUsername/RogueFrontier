using Common;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrainWaves;

class WorldBuilder {
    World World;
    HashSet<Rectangle> rooms = new HashSet<Rectangle>();
    HashSet<(int, int)> built = new HashSet<(int, int)>();
    public WorldBuilder(World World) {
        this.World = World;
    }
    public void Build() {
        Random rnd = new Random();
        int gridSize = 9;

        RoomSource mainRoom = new RoomSource() {
            Center = new XY(0, 0),
            Dimensions = new XY(gridSize * 3, gridSize * 3)
        };
        BuildRoom(mainRoom);

        Queue<HallwaySource> hallways = new Queue<HallwaySource>();

        int i;
        for (i = 0; i < 3; i++) {
            var direction = XY.Polar((i * 90) * Math.PI / 180);
            HallwaySource mainHall = new HallwaySource() {
                Center = mainRoom.Center + direction * gridSize * 3 / 2,
                Direction = direction,
                Size = gridSize
            };
            hallways.Enqueue(mainHall);
        }

        i = 0;
        while (hallways.Count > 0) {
            var h = hallways.Dequeue();

            BuildHall(h);
            /*
            if(rnd.Next(0, 10) > 0) {
                hallways.Enqueue(h);
            }
            */
            if (h.StepsFromBranch > 2) {

                if (rnd.Next(0, 4) > 0) {
                    if (rnd.Next(0, 2) == 0) {
                        var direction = h.Direction.Rotate(90 * Math.PI / 180);
                        var branch = new HallwaySource() { Center = h.Center, Direction = direction, Size = h.Size };
                        TryBuildBranch(branch);
                    } else if (rnd.Next(0, 2) == 0) {
                        var direction = h.Direction.Rotate(90 * Math.PI / 180);
                        BuildSideRoom(direction);
                    }

                    if (rnd.Next(0, 2) == 0) {
                        var direction = h.Direction.Rotate(-90 * Math.PI / 180);
                        var branch = new HallwaySource() { Center = h.Center, Direction = direction, Size = h.Size };
                        TryBuildBranch(branch);
                    } else if (rnd.Next(0, 2) == 0) {
                        var direction = h.Direction.Rotate(-90 * Math.PI / 180);
                        BuildSideRoom(direction);
                    }
                    h.StepsFromBranch = 0;
                    void BuildSideRoom(XY direction) {

                        var s = direction * gridSize - h.Direction * gridSize * (h.StepsFromBranch + 1);
                        var p = (h.Center + h.Direction * h.Size - new XY(h.Size / 2, h.Size / 2)) + direction * h.Size + s / 2;


                        //Go to the side, and then backtrack
                        var points = Enumerable.Range(0, h.StepsFromBranch)
                            .Select(i => h.Center + direction * h.Size - h.Direction * i);
                        if (points.Select(p => (p.xi, p.yi)).All(IsOpen)) {
                            var branch = new HallwaySource() { Center = h.Center, Direction = direction, Size = h.Size };
                            TryBuildDoorway(branch);

                            BuildRoom(new RoomSource() {
                                Center = p,
                                Dimensions = s.abs
                            });
                        }

                    }
                } else if (rnd.Next(0, 2) > 0) {
                    var p = h.Center + h.Direction * gridSize * 1.5;

                    if (IsOpen(p)) {
                        var r = new RoomSource() {
                            Center = p,
                            Dimensions = new XY(gridSize * 3, gridSize * 3)
                        };
                        BuildRoom(r);
                        for (int j = 0; j < 2; j++) {
                            h.Step();
                        }
                    }
                }
            }

            if (rnd.Next(0, 15) > 0) {
                h.Step();
                h.StepsFromBranch++;
                if (IsOpen(h.Center)) {
                    hallways.Enqueue(h);
                } else {
                    TerminateHall();
                }
            } else {
                TerminateHall();
            }
            void TerminateHall() {
                var p = h.Center + h.Direction * gridSize * 1.5;
                if (IsOpen(p)) {
                    var r = new RoomSource() {
                        Center = p,
                        Dimensions = new XY(gridSize * 3, gridSize * 3)
                    };
                    BuildRoom(r);
                } else {
                    BuildDeadEnd(h);
                }
            }

            void TryBuildDoorway(HallwaySource branch) {
                if (IsOpen(branch.Center + branch.Direction * h.Size)) {
                    branch.Center += branch.Direction * (h.Size / 2);
                    BuildHall(branch);
                }
            }
            void TryBuildBranch(HallwaySource branch) {
                var dest = branch.Center + branch.Direction * h.Size;
                if (IsOpen(dest)) {
                    branch.Center = branch.Center + branch.Direction * (h.Size / 2);
                    BuildHall(branch);
                    branch.Center = dest;
                    hallways.Enqueue(branch);
                }
            }
        }
        void BuildRoom(RoomSource r) {
            var rect = r.rect;
            foreach (var p in rect.PerimeterPositions()) {
                BuildIfOpen(p, new Wall());
            }

            rect = rect.WithPosition(rect.Position + new Point(1, 1)).WithSize(rect.Size - new Point(2, 2));
            foreach (var p in rect.Positions()) {
                Build(p, new Floor());
            }
        }
        void BuildHall(HallwaySource h) {
            for (int indexForward = -h.Size / 2; indexForward <= h.Size / 2 /* 0 */; indexForward++) {
                var forward = h.Direction;
                var left = h.Direction.Rotate(90 * Math.PI / 180);
                var right = h.Direction.Rotate(-90 * Math.PI / 180);

                BuildIfOpen(h.Center + forward * indexForward + left * (h.Size / 2), new Wall());
                BuildIfOpen(h.Center + forward * indexForward + right * (h.Size / 2), new Wall());
                for (int indexSide = 0; indexSide < h.Size / 2; indexSide++) {
                    Build(h.Center + forward * indexForward + left * indexSide, new Floor());
                    Build(h.Center + forward * indexForward + right * indexSide, new Floor());
                }
            }
            World.AddEntity(new Light(World, h.Center.round));
        }
        void BuildDeadEnd(HallwaySource h) {
            var forward = h.Direction;
            var left = h.Direction.Rotate(90 * Math.PI / 180);
            var right = h.Direction.Rotate(-90 * Math.PI / 180);
            for (int indexSide = 0; indexSide < h.Size; indexSide++) {
                Build(h.Center + forward * (h.Size / 2) - left * (h.Size / 2) + right * indexSide, new Wall());
            }
        }
        void BuildIfOpen((int x, int y) point, Voxel v) {
            if (IsOpen(point)) {
                Build(point, v);
            }
        }
        bool IsOpen((int x, int y) point) => World.voxels.Get(point.x, point.y) == null;
        void Build((int x, int y) point, Voxel v) {
            World.voxels.Set(point.x, point.y, v);
            built.Add(point);
        }

    }
    class RoomSource {
        public XY Center;
        public XY Dimensions;
        public Rectangle rect => new Rectangle(Center - Dimensions / 2, Center + Dimensions / 2);
    }
    class HallwaySource {
        public XY Center;
        public XY Direction;
        public int Size;
        public int StepsFromBranch;
        public void Step() => Center += Direction * Size;
    }
}
