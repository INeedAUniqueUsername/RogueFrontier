using Common;
using LibGamer;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace RogueFrontier;
public record SceneCtx {
	public int Width;
	public int Height;
	public IScene prev;
	public PlayerShip playerShip;

    public void Go (IScene s) => prev.Go(s);

	public SceneCtx (Mainframe main) {
		Width = main.Width;
		Height = main.Height;
		prev = main;
		playerShip = main.playerShip;
	}
    public SceneCtx(IScene prev, LibGamer.Sf sf, PlayerShip pl) {
        (Width, Height) = (sf.GridWidth, sf.GridHeight);
        this.prev = prev;
        this.playerShip = pl;
    }
}
public interface IDockable : StructureObject {
    IEnumerable<XY> GetDockPoints();
    IScene GetDockScene(SceneCtx ctx);
}
public interface MovingObject : Entity {

    World world { get; }
    XY velocity { get; }
}
public interface StructureObject : MovingObject {
    string name { get; }
    //World world { get; }
    XY velocity { get; set; }

    void Damage(Projectile p);
    void Destroy(ActiveObject source = null);
}
public interface ActiveObject : StructureObject {
    Sovereign sovereign { get; }
}
public static class SSpaceObject {
    public static bool IsEqual(this Entity o1, Entity o2) {
        { if (o1 is ISegment s) o1 = s.parent; }
        { if (o2 is ISegment s) o2 = s.parent; }
        
        return o1 == o2;
    }
    /// <summary>
    /// Get all objects targeted by at least one weapon on <c>actor</c>
    /// </summary>
    /// <param name="actor"></param>
    /// <returns></returns>
    public static IEnumerable<ActiveObject> GetWeaponTargets(ActiveObject actor) {
        IEnumerable<Weapon> weapons = actor switch {
            Station st => st.weapons,
            AIShip ai => ai.devices.Weapon,
            PlayerShip pl => pl.devices.Weapon,
            _ => Enumerable.Empty<Weapon>()
        };
        return weapons.SelectMany(w => w.targeting?.GetMultiTarget() ?? Enumerable.Empty<ActiveObject>());
    }

    public static bool CanTarget(this ActiveObject owner, ActiveObject target) {
        if (owner is TargetingMarker t)
            owner = t.Owner;
        if (!target.active)
            return false;
        if (IsEqual(owner, target))
            return false;
        if (target is Wreck)
            return false;
        if (target is Stargate)
            return false;

        return owner.sovereign.IsEnemy(target.sovereign)
            || (owner is PlayerShip pl && pl.GetTarget() == target)
            || (owner is AIShip s && s.behavior.GetOrder()?.CanTarget(target) == true);
    }
}