using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common;

public interface IContainer<T> {
    T Value { get; }
}
public interface Lis<T> : IContainer<T> where T:Delegate {
    T IContainer<T>.Value => Value;
    T Value { get; }
}
public class Dict<T, U> : IContainer<Dictionary<T, U>> {
    public Dictionary<T, U> Value { get; private set; }
    public U this[T key] {
        get => Value[key];
        set => Value[key] = value;
    }
    public Dict() {
        Value = new Dictionary<T, U>();
    }
    public Dict(Dictionary<T, U> Value) {
        this.Value = Value;
    }
    public bool TryGetValue(T key, out U result) => Value.TryGetValue(key, out result);
    public static implicit operator Dictionary<T, U>(Dict<T, U> d) => d.Value;
}
public class Container<T> : IContainer<T> {
    public T Value { get; set; }
    public Container(T Value) {
        this.Value = Value;
    }
    public static implicit operator T(Container<T> c) => c.Value;
}

public static class SFuncSet {
    public static void ForEach<T>(this FuncSet<IContainer<T>> f, Action<T> a) {
        foreach(var t in f.set) {
            a(t.Value);
        }
    }
    public static void RemoveNull<T>(this FuncSet<IContainer<T>> f) {
        f.set.RemoveWhere(f => f.Value == null);
    }
}
public class FuncSet<T> {
    public HashSet<T> set = new();
    public static FuncSet<T> operator -(FuncSet<T> f, T t) {
        f.set.Remove(t);
        return f;
    }
    public static FuncSet<T> operator +(FuncSet<T> f, T t) {
        f.set.Add(t);
        return f;
    }
    public bool Add(T t) => set.Add(t);
    public bool Remove(T t) => set.Remove(t);
    public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
    public List<T> ToList() => set.ToList();
    public static implicit operator HashSet<T>(FuncSet<T> f) => f.set;
}
public static class SEv {
    public static void ForEach<T>(this Ev<T> f, Action<T> a) {
        foreach (var t in f.set) {
            a(t.Value);
        }
    }
    public static void RemoveNull<T>(this Ev<T> f) {
        f.set.RemoveWhere(f => f.Value == null);
    }
}
public class Ev<T> {
    public HashSet<IContainer<T>> set = new();
    public IEnumerable<T> values => set.Select(i => i.Value);
    public static Ev<T> operator -(Ev<T> f, IContainer<T> t) {
        f.set.Remove(t);
        return f;
    }
    public static Ev<T> operator +(Ev<T> f, IContainer<T> t) {
        f.set.Add(t);
        return f;
    }
    public bool Add(IContainer<T> t) => set.Add(t);
    public bool Remove(IContainer<T> t) => set.Remove(t);
    public bool any => set.Count > 0;
    public IEnumerator<T> GetEnumerator() => values.GetEnumerator();
    public List<T> ToList() => set.Select(i => i.Value).ToList();
    public static implicit operator HashSet<T>(Ev<T> f) => f.values.ToHashSet();
}