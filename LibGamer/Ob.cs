using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common;
public interface Ob<T> {
    public void Observe<U>(U u) {
        (this as Ob<U>).Observe(u);
    }
    public void Observe(T t);
}
public class Vi<T> {
    public HashSet<Ob<T>> set = new();
    public static Vi<T> operator -(Vi<T> f, Ob<T> t) {
        f.set.Remove(t);
        return f;
    }
    public static Vi<T> operator +(Vi<T> f, Ob<T> t) {
        f.set.Add(t);
        return f;
    }
    public bool Add(Ob<T> t) => set.Add(t);
    public bool Remove(Ob<T> t) => set.Remove(t);
    public void Observe(T t) {
        foreach(var o in set) {
            o.Observe(t);
        }
    }
    public bool any => set.Count > 0;
    public IEnumerator<Ob<T>> GetEnumerator() => set.GetEnumerator();
    public static implicit operator HashSet<Ob<T>>(Vi<T> f) => f.set;
}