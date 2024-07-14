using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibTerminator;
public interface INotify {
	
}
public class Notify {
	(int, int) pos;
	object sound;
	string message;
}