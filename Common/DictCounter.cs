using System.Collections.Generic;

namespace Common;

public class DictCounter<T> {
    public Dictionary<T, int> dict;
    public int this[T t] {
        get => dict.TryGetValue(t, out int result) ? result : 0;
        set => dict[t] = value;
    }
    public DictCounter() {
        dict = new Dictionary<T, int>();
    }
    public void Add(T t) {
        if (dict.ContainsKey(t)) {
            dict[t]++;
        } else {
            dict[t] = 1;
        }
    }

    public void Increment(T t) => Add(t);
    public void Decrement(T t) {
        if (dict.ContainsKey(t)) {
            if (dict[t] > 1) {
                dict[t]--;
            } else {
                dict.Remove(t);
            }
        }
    }
}
