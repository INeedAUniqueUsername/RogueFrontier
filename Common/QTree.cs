using System;
using System.Collections.Generic;
namespace Common;

public static class SGrid {
    public static T Get<T>(this GridTree<T> g, XY xy) =>
        g.Get(xy.xi, xy.yi);
    public static T At<T>(this GridTree<T> g, XY xy) =>
        g.At(xy.xi, xy.yi);
    public static void Set<T>(this GridTree<T> g, XY xy, T t) =>
        g.Set(xy.xi, xy.yi, t);
    public static bool IsInit<T>(this GeneratedGrid<T> g, XY xy, out T t) {
        if (g.IsInit(xy.xi, xy.yi)) {
            t = g.Get(xy.xi, xy.yi);
            return true;
        }
        t = default(T);
        return false;
    }
}
public interface GridTree<T> {
    T Get(long x, long y);
    ref T At(long x, long y);
    void Set(long x, long y, T t);
}
public class QTree<T> : GridTree<T> {
    public T center;
    public Dictionary<(ulong, ulong), Section> q1, q2, q3, q4;
    public Dictionary<ulong, Segment> xPositive, xNegative, yPositive, yNegative;
    public uint level;
    public uint scale;
    public int segmentCount => q1.Count + q2.Count + q3.Count + q4.Count;
    public uint size => (uint)Math.Pow(scale, level);
    public QTree(uint level = 1, uint scale = 32) {
        q1 = new();
        q2 = new();
        q3 = new();
        q4 = new();
        xPositive = new();
        xNegative = new();
        yPositive = new();
        yNegative = new();
        this.level = level;
        this.scale = scale;
    }
    public void Clear() {
        center = default(T);
        foreach (var d in new[] { q1, q2, q3, q4 }) {
            d.Clear();
        }
        foreach (var d in new[] { xPositive, xNegative, yPositive, yNegative }) {
            d.Clear();
        }
    }
    public const byte CODE_OFFSET = 2;
    public const byte CODE_SHIFT = 2;
    public const byte CODE_ORIGIN = (0 + CODE_OFFSET) | ((0 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_X_POSITIVE = (1 + CODE_OFFSET) | ((0 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_X_NEGATIVE = (-1 + CODE_OFFSET) | ((0 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_Y_POSITIVE = (0 + CODE_OFFSET) | ((1 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_Y_NEGATIVE = (0 + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);

    public const byte CODE_QUADRANT_1 = (1 + CODE_OFFSET) | ((1 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_QUADRANT_2 = (-1 + CODE_OFFSET) | ((1 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_QUADRANT_3 = (-1 + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);
    public const byte CODE_QUADRANT_4 = (1 + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);
    public static byte SignCode(long x, long y) => (byte)((Math.Sign(x) + CODE_OFFSET) | ((Math.Sign(y) + CODE_OFFSET) << CODE_SHIFT));
    public ref T this[(long x, long y) p] => ref At(p.x, p.y);
    public T Get(long x, long y) {
        switch (SignCode(x, y)) {
            case CODE_ORIGIN:
                return center;
            case CODE_X_NEGATIVE:
                return GetSegment((uint)Math.Abs(x), xNegative);
            case CODE_X_POSITIVE:
                return GetSegment((uint)Math.Abs(x), xPositive);
            case CODE_Y_NEGATIVE:
                return GetSegment((uint)Math.Abs(y), yNegative);
            case CODE_Y_POSITIVE:
                return GetSegment((uint)Math.Abs(y), yPositive);
            case CODE_QUADRANT_1:
                return GetSection(q1);
            case CODE_QUADRANT_2:
                return GetSection(q2);
            case CODE_QUADRANT_3:
                return GetSection(q3);
            case CODE_QUADRANT_4:
                return GetSection(q4);
            default:
                throw new ArgumentOutOfRangeException("Unknown location");
        }
        T GetSegment(ulong ia, Dictionary<ulong, Segment> strip) {
            ulong index = ia / size;
            if (strip.TryGetValue(index, out Segment segment)) {
                return segment.Get(ia - index * size);
            } else {
                return default(T);
            }
        }
        T GetSection(Dictionary<(ulong, ulong), Section> quadrant) {
            var xa = (ulong)Math.Abs(x);
            var ya = (ulong)Math.Abs(y);

            var xIndex = xa / size;
            var yIndex = ya / size;
            if (quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                return section.Get(xa - xIndex * size, ya - yIndex * size);
            } else {
                return default(T);
            }
        }
    }
    public ref T At(long x, long y) {
        var code = SignCode(x, y);
        switch (code) {
            case CODE_ORIGIN:
                return ref center;
            case CODE_X_NEGATIVE:
                return ref AtSegment((ulong)Math.Abs(x), xNegative);
            case CODE_X_POSITIVE:
                return ref AtSegment((ulong)Math.Abs(x), xPositive);
            case CODE_Y_NEGATIVE:
                return ref AtSegment((ulong)Math.Abs(y), yNegative);
            case CODE_Y_POSITIVE:
                return ref AtSegment((ulong)Math.Abs(y), yPositive);
            case CODE_QUADRANT_1:
                return ref AtSection(q1);
            case CODE_QUADRANT_2:
                return ref AtSection(q2);
            case CODE_QUADRANT_3:
                return ref AtSection(q3);
            case CODE_QUADRANT_4:
                return ref AtSection(q4);
            default:
                throw new ArgumentOutOfRangeException("Unknown location");
        }
        ref T AtSegment(ulong ia, Dictionary<ulong, Segment> strip) {
            var index = ia / size;
            if (!strip.TryGetValue(index, out Segment segment)) {
                Initialize(out segment);
                strip[index] = segment;
            }
            return ref segment.At(ia - index * size);
        }
        ref T AtSection(Dictionary<(ulong, ulong), Section> quadrant) {
            var xa = (ulong)Math.Abs(x);
            var ya = (ulong)Math.Abs(y);

            var xIndex = xa / size;
            var yIndex = ya / size;
            if (!quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                Initialize(out section);
                quadrant[(xIndex, yIndex)] = section;
            }
            return ref section.At(xa - xIndex * size, ya - yIndex * size);
        }
    }
    public void Set(long x, long y, T t) {

        switch (SignCode(x, y)) {
            case CODE_ORIGIN:
                center = t;
                break;
            case CODE_X_NEGATIVE:
                SetSegment((ulong)Math.Abs(x), xNegative);
                break;
            case CODE_X_POSITIVE:
                SetSegment((ulong)Math.Abs(x), xPositive);
                break;
            case CODE_Y_NEGATIVE:
                SetSegment((ulong)Math.Abs(y), yNegative);
                break;
            case CODE_Y_POSITIVE:
                SetSegment((ulong)Math.Abs(y), yPositive);
                break;
            case CODE_QUADRANT_1:
                SetSection(q1);
                break;
            case CODE_QUADRANT_2:
                SetSection(q2);
                break;
            case CODE_QUADRANT_3:
                SetSection(q3);
                break;
            case CODE_QUADRANT_4:
                SetSection(q4);
                break;
            default:
                throw new ArgumentOutOfRangeException("Unknown location");
        }
        void SetSegment(ulong ia, Dictionary<ulong, Segment> strip) {
            var index = ia / size;
            if (!strip.TryGetValue(index, out Segment segment)) {
                Initialize(out segment);
                strip[index] = segment;
            }
            segment.Set(ia - index * size, t);
        }
        void SetSection(Dictionary<(ulong, ulong), Section> quadrant) {
            var xa = (ulong)Math.Abs(x);
            var ya = (ulong)Math.Abs(y);

            var xIndex = xa / size;
            var yIndex = ya / size;
            if (!quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                Initialize(out section);
                quadrant[(xIndex, yIndex)] = section;
            }
            section.Set(xa - xIndex * size, ya - yIndex * size, t);
        }
    }
    private void Initialize(out Section section) =>
        section = level == 1 ? new Leaf(scale) : new Quadrant(level - 1, scale);
    private void Initialize(out Segment segment) =>
        segment = level == 1 ? new Slice(scale) : new Strip(level - 1, scale);

    public interface Segment {
        T Get(ulong i);
        ref T At(ulong i);
        void Set(ulong x, T t);
    }
    public class Strip : Segment {
        public Dictionary<ulong, Segment> segments;
        public uint scale;
        public uint level;
        public uint size => (uint)Math.Pow(scale, level);
        public Strip(uint level, uint scale = 8) {
            segments = new();
            this.level = level;
            this.scale = scale;
        }
        public T Get(ulong i) {
            var index = i / size;
            if (segments.TryGetValue(index, out var segment)) {
                return segment.Get(i - index * size);
            } else {
                return default(T);
            }
        }
        public ref T At(ulong i) {
            var index = i / size;

            if (!segments.TryGetValue(index, out var segment)) {
                Initialize(out segment);
                segments[index] = segment;
            }
            return ref segment.At(i - index * size);
        }
        public void Set(ulong i, T t) {
            var index = i / size;

            if (!segments.TryGetValue(index, out var section)) {
                Initialize(out section);
                segments[index] = section;
            }
            section.Set(i - index * size, t);
        }
        private void Initialize(out Segment section) =>
            section = level == 1 ? new Slice(scale) : new Strip(level - 1, scale);
    }
    public class Slice : Segment {
        public T[] items;
        public uint scale;
        public Slice(uint scale) => (this.scale, items) = (scale, new T[scale]);
        public T Get(ulong i) => items[i];
        public ref T At(ulong i) => ref items[i];
        public void Set(ulong i, T t) => items[i] = t;
        
    }

    public interface Section {
        T Get(ulong x, ulong y);
        ref T At(ulong x, ulong y);
        void Set(ulong x, ulong y, T t);
    }
    public class Quadrant : Section {
        public Dictionary<(ulong, ulong), Section> sections;
        public uint scale;
        public uint level;
        public uint size => (uint)Math.Pow(scale, level);
        public Quadrant(uint level, uint scale = 8) {
            sections = new();
            this.level = level;
            this.scale = scale;
        }
        public T Get(ulong x, ulong y) {
            var xIndex = x / size;
            var yIndex = y / size;
            if (sections.TryGetValue((xIndex, yIndex), out var section)) {
                return section.Get(x - xIndex * size, y - yIndex * size);
            } else {
                return default(T);
            }
        }
        public ref T At(ulong x, ulong y) {
            var xIndex = x / size;
            var yIndex = y / size;

            if (!sections.TryGetValue((xIndex, yIndex), out var section)) {
                Initialize(out section);
                sections[(xIndex, yIndex)] = section;
            }
            return ref section.At(x - xIndex * size, y - yIndex * size);
        }
        public void Set(ulong x, ulong y, T t) {
            var xIndex = x / size;
            var yIndex = y / size;

            if (!sections.TryGetValue((xIndex, yIndex), out var section)) {
                Initialize(out section);
                sections[(xIndex, yIndex)] = section;
            }
            section.Set(x - xIndex * size, y - yIndex * size, t);
        }
        private void Initialize(out Section section) =>
            section = level == 1 ? new Leaf(scale) : new Quadrant(level - 1, scale);
    }
    class Leaf : Section {
        public T[,] items;
        public uint scale;
        public Leaf(uint scale) =>
            (this.scale, items) = (scale, new T[scale, scale]);
        public T Get(ulong x, ulong y) => items[x, y];
        public ref T At(ulong x, ulong y) => ref items[x, y];
        public void Set(ulong x, ulong y, T t) => items[x, y] = t;
    }
}
