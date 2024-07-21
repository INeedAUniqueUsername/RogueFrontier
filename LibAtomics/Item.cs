using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAtomics;
public class Item {
	public ItemType type;
	public Item (ItemType type) => this.type = type;
}
public class ItemType {
	public string name;

	public Tile tile;
}
