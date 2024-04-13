using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RogueFrontier;
public interface ICrime {
    string name { get; }
    bool resolved { get; set; }
}
public class Crime : ICrime {
    public string name { get; }
    public bool resolved { get; set; }
    public Crime(string name, bool resolved = false) {
        this.name = name;
        this.resolved = resolved;
    }
}
public class DestructionCrime : ICrime {
    public string name => $"destruction of {destroyed.name}";
    public bool resolved { get; set; } = false;
    public ActiveObject destroyed;
    public DestructionCrime(ActiveObject destroyed) {
        this.destroyed = destroyed;
    }
}