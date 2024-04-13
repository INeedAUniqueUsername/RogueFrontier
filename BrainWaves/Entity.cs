using Common;
using SadConsole;
using System;
using System.Collections.Generic;

namespace BrainWaves;

public interface Entity {
    World World { get; }
    XY Position { get; set; }
    ColoredGlyph Tile { get; }
    bool Active { get; }
    void UpdateStep();
    public bool IsVisible(HashSet<(int, int)> visible, XY p) {
        var displacement = (Position - p);
        var direction = displacement.normal;
        var dist = displacement.magnitude;
        bool result = true;


        var v = World.voxels.Get(p);
        if (v == null) {
            result = false;
        } else if (v is Floor) {

            //Looking down a hallway at an angle
            for (int i = 1; i < dist / 2 + 1; i++) {
                var behind = p + direction * i;
                behind = behind.round;
                result = result && visible.Contains(behind) && World.voxels.Get(behind) is Floor;
            }
        } else if (v is Wall) {
            //Looking down a hallway at an angle
            for (int i = 1; i < dist / 2 + 1; i++) {
                var behind = p + direction * i;
                behind = behind.round;

                var left = behind + direction.Rotate(90 * Math.PI / 180);
                var right = behind + direction.Rotate(90 * Math.PI / 180);

                result = result && visible.Contains(behind) &&
                    ((visible.Contains(left) /* && World.voxels.Get(left) is Floor */)
                        || (visible.Contains(right) /* && World.voxels.Get(right) is Floor */));
            }
        }

        return result;
    }
    public void RemoveDark(HashSet<(int, int)> visible) {
        visible.RemoveWhere(p => {
            var b = World.brightness[p];
            return (b < 128 && (Position - p).magnitude > Math.Max(2, b / 16));
        });
    }

    public void UpdateRealtime() { }
}
