using Newtonsoft.Json;
using System.Collections.Generic;

namespace Common;

public interface IGridGenerator<T> {
    public T Generate((long, long) pos);
}
public class GeneratedGrid<T> : GridTree<T> {
    [JsonIgnore]
    public QTree<T> tree;
    public IGridGenerator<T> generator;
    public GeneratedGrid(IGridGenerator<T> generator) {
        tree = new();
        this.generator = generator;
    }
    public ref T this[long x, long y] => ref Initialize(x, y);
    public T Get(long x, long y) =>
        Initialize(x, y);
    public ref T At(long x, long y) =>
        ref Initialize(x, y);
    public ref T Initialize(long x, long y) {
        ref var t = ref tree.At(x, y);
        if (EqualityComparer<T>.Default.Equals(t, default(T))) {
            t = generator.Generate((x, y));
        }
        return ref t;
    }
    public void Set(long x, long y, T t) => tree.Set(x, y, t);
    public bool IsInit(long x, long y) =>
        !EqualityComparer<T>.Default.Equals(tree.Get(x, y), default(T));
}
