using Common;
using System.Xml.Linq;

namespace RogueFrontier;

public class GenomeType : IDesignType {
    [Req] public string name, species, gender, subjective, objective, possessiveAdj, possessiveNoun, reflexive;
    public void Initialize(TypeCollection collection, XElement e) {
        e.Initialize(this);
    }
}
