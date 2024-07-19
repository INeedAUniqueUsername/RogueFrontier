using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibGamer;
public class BufferedSet<T> {

	public HashSet<T> items = [];
	public HashSet<T> add = [];
	public HashSet<T> remove = [];

	public bool Add(T e, bool busy = true) {
		remove.Remove(e);
		if(items.Contains(e))
			return false;
		if(busy) {
			add.Add(e);
		} else {
			items.Add(e);
		}
		return true;
	}
	public bool Remove(T e, bool busy = true) {
		add.Remove(e);
		if(!items.Contains(e))
			return false;
		if(busy) {
			remove.Add(e);
		} else {
			items.Remove(e);
		}
		return true;
	}
	public void Update () {
		items.UnionWith(add);
		items.ExceptWith(remove);
		add.Clear();
		remove.Clear();
	}
}
