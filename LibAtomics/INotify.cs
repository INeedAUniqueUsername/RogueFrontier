using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAtomics;
public interface INotify {
	
}
public class Notify {
	(int, int) pos;
	object sound;
	string message;
}