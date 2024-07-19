using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibGamer;
class BufferedSet<T> {

	public HashSet<T> items;
	public HashSet<T> add;
	public HashSet<T> remove;

	public bool Add(T e) {
		remove.Remove(e);
		if(items.Contains(e))
			return false;
		add.Add(e);
		return true;
	}
	public bool Remove(T e) {
		add.Remove(e);
		if(!items.Contains(e))
			return false;
		remove.Add(e);
		return true;
	}
	public void Update () {
		items.UnionWith(add);
		items.ExceptWith(remove);
		add.Clear();
		remove.Clear();
	}
}
