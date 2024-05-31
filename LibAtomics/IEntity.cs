using Common;
using LibGamer;
namespace LibAtomics;
public interface IEntity {
	XYI pos { get; }
	Tile tile { get; }
	Action Removed { get; set; }
}
public interface IActor {
	void UpdateTick () { }
	void UpdateReal (TimeSpan delta) { }
}