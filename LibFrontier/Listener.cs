using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public class NotifyStationDestroyed : Ob<Station.Destroyed> {
	public PlayerShip playerShip;
	public Station source;
	public void Observe (Station.Destroyed ev) {
		var (s, d, w) = ev;
		playerShip?.AddMessage(new Transmission(source,
			$"{source.name} destroyed by {d?.name ?? "unknown forces"}!"
			));
	}
	public NotifyStationDestroyed (PlayerShip playerShip, Station source) {
		this.playerShip = playerShip;
		this.source = source;
	}
}
public class Camera {
	public XY position;
	//For now we don't allow shearing
	public double rotation { get => Math.Atan2(right.y, right.x); set => right = XY.Polar(value, 1); }
	public XY up => right.Rotate(Math.PI / 2);
	public XY right;
	public Camera (XY position) {
		this.position = position;
		right = new XY(1, 0);
	}
	public Camera () {
		position = new XY();
		right = new XY(1, 0);
	}
	public void Rotate (double angle) {
		right = right.Rotate(angle);
	}
}


public class SilenceListener : IWeaponListener {
	public void Observe (IWeaponListener.WeaponFired ev) => Add(ev.proj);
	private void Add (List<Projectile> p) =>
		p.ForEach(silence.AddEntity);
	System silence;
	public SilenceListener (System s) {
		this.silence = s;
	}
}