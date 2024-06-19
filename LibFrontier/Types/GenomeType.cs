using Common;
using System.Xml.Linq;

namespace RogueFrontier;

public class GenomeType : IDesignType {
    [Req] public string name, kind, gender, subjective, objective, possessiveAdj, possessiveNoun, reflexive;
    public void Initialize(Assets collection, XElement e) {
        e.Initialize(this);
    }
}
