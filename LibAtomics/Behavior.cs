using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace LibAtomics;
public interface IBehavior {}
public record ShootAt (IEntity target) {

}