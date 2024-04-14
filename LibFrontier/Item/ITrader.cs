using RogueFrontier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public interface ITrader {
	string name { get; }
	HashSet<Item> cargo { get; }

	//public static implicit operator Dealer(ITrader r) => new(r.name, r.cargo);
}
public delegate int GetPrice (Item i);