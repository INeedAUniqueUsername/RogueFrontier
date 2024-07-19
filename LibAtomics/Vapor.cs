using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibAtomics;
public class Vapor : IActor {
	VaporDesc desc;
}
public record VaporDesc {
	public static VaporDesc nerveAgent = new() {
		name = "Nerve Agent",
	};
	string name;
}