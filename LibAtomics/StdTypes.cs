using LibGamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibAtomics;
public static class StdTypes {
	public static readonly ItemType
		item_marrow_ring = new() { name = "Marrow Ring", tile = (ABGR.White, ABGR.Black, 'o') },
		item_rattle_blade = new() { name = "Rattle Blade", tile = (ABGR.LightGreen, ABGR.Black, '/') },
		item_miasma_grenade = new() { name = "Miasma Grenade", tile = (ABGR.Tan, ABGR.Black, 'g') },
		item_machine_gun = new() { name = "Machine Gun", tile = (ABGR.LightGray, ABGR.Black, 'R') };
}