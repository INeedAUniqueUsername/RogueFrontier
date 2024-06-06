using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using LibGamer;
using System.IO;
namespace RogueFrontier;
public interface ItemUse {
	string GetDesc(PlayerShip player, Item item);
	void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) { }
}
public record DeployShip : ItemUse {
	[Req(parse = false)] public ShipClass shipClass;
	public DeployShip() { }
	public DeployShip(Assets tc, XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(shipClass)] = (string s) => tc.Lookup<ShipClass>(s)
		});
	}
	public string GetDesc(PlayerShip player, Item item) => $"Deploy {shipClass.name}";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {
		var w = new Wingmate(player);
		var a = new AIShip(
			new(player.world, shipClass, player.position),
			player.sovereign,
			behavior:w
			);
		player.onDestroyed += w;
		player.world.AddEntity(a);
		player.world.AddEffect(new Heading(a));
		player.wingmates.Add(a);
		player.AddMessage(new Transmission(a, $"Deployed {shipClass.name}"));
		player.cargo.Remove(item);
		callback?.Invoke();
	}
}
public record DeployStation : ItemUse {
	[Req(parse = false)] public StationType stationType;
	public DeployStation() { }
	public DeployStation(Assets tc, XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(stationType)] = (string s) => tc.Lookup<StationType>(s)
		});
	}
	public string GetDesc(PlayerShip player, Item item) => $"Deploy {stationType.name}";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {
		var a = new Station(player.world, stationType, player.position) { sovereign = player.sovereign };
		player.world.AddEntity(a);
		a.CreateSegments();
		player.AddMessage(new Transmission(a, $"Deployed {stationType.name}"));
		player.cargo.Remove(item);
		callback?.Invoke();
	}
}


public record InstallWeapon : ItemUse {
	public string GetDesc(PlayerShip player, Item item) =>
		player.cargo.Contains(item) ? "Install this weapon" : "Remove this weapon";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {
		if (player.cargo.Contains(item)) {
			if(player.shipClass.restrictWeapon?.Matches(item) == false) {
				player.AddMessage(new Message($"Unable to install weapon (incompatible): {item.type.name}"));
			} else {
				player.AddMessage(new Message($"Installed weapon: {item.type.name}"));
				player.cargo.Remove(item);
				player.devices.Install(item.Get<Weapon>());
			}
		} else {
			player.AddMessage(new Message($"Removed weapon: {item.type.name}"));
			player.devices.Remove(item.weapon);
			player.cargo.Add(item);
		}
		callback?.Invoke();
	}
}
public record RepairArmor : ItemUse {
	[Req] public int repairHP;
	public string GetDesc(PlayerShip player, Item item) => "Repair armor";
	public RepairArmor() { }
	public RepairArmor(XElement e) {
		e.Initialize(this);
	}
	public void Invoke(SceneCtx c, Item item, Action callback) {
		c.prev.Go(SMenu.RepairArmorFromItem(c, item, this, callback));
	}
}
public record InvokePower : ItemUse {
	[Req] public int charges;
	[Req(parse = false)] public PowerType powerType;
	public InvokePower() { }
	public InvokePower(Assets tc, XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(powerType)] = (string s) => tc.Lookup<PowerType>(s)
		});
	}
	public string GetDesc(PlayerShip player, Item item) =>
		$"Invoke {powerType.name} ({charges} charges remaining)";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {
		player.AddMessage(new Message($"Invoked the power of {item.type.name}"));
		charges--;
		if (charges == 0) {
			player.cargo.Remove(item);
		}
		powerType.Effect.ForEach(e=>e.Invoke(player));
		callback?.Invoke();
	}
}
public record Refuel : ItemUse {
	[Req] public int energy;
	public Refuel() { }
	public Refuel(Assets tc, XElement e) {
		e.Initialize(this);
	}
	public string GetDesc(PlayerShip player, Item item) => "Refuel reactor";
	public void Invoke(SceneCtx c, Item item, Action callback = null) {
		c.prev.Go(SMenu.RefuelFromItem(c, item, this, callback));
	}
}
public record DepleteTargetShields() : ItemUse {
	public DepleteTargetShields(XElement e) : this() {
		e.Initialize(this);
	}
	public string GetDesc(PlayerShip player, Item item) =>
		player.GetTarget(out var t) ? $"Deplete shields on {t.name}" : "Deplete shields on target";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {

		//var am = Common.Main.PreBind(player.AddMessage, (string s) => new Message(s));
		var am = Main.PreBind((string s) => player.AddMessage(new Message(s)));
		(
			!player.GetTarget(out var t) ?
				am($"No target available") :
			!(t is IShip s) ?
				am($"Target must be a ship") :
			s.devices.Shield.Count == 0 ?
				am($"Target does not have installed shields") :
			!s.devices.Shield.Any(s => s.hp > 0) ?
				am($"Target does not have active shields") + (() => { }) :
			() => {
				s.devices.Shield.ForEach(s => s.Deplete());
				player.AddMessage(new Message($"Depleted shields on {s.name}"));

				player.cargo.Remove(item);
				callback?.Invoke();
			}
		).Invoke();
	}
}
public record ReplaceDevice() : ItemUse {
	[Req(parse =false)]public ItemType from, to;
	public ReplaceDevice(Assets tc, XElement e) : this() => e.Initialize(this, transform: new() {
		[nameof(from)] = tc.Lookup<ItemType>,
		[nameof(to)] = tc.Lookup<ItemType>,
	});
	public string GetDesc(PlayerShip player, Item item) =>
		$"Replace installed {from.name}";
	public void Invoke(SceneCtx c, Item item, Action callback = null) {
		c.Go(SMenu.ReplaceDeviceFromItem(c, item, this, callback));
		c.playerShip.cargo.Remove(item);
		callback?.Invoke();
	}
}
public record RechargeWeapon() : ItemUse {
	[Req(parse = false)] public WeaponDesc weaponType;
	[Req] public int charges;
	public RechargeWeapon(Assets tc, XElement e) : this() {
		e.Initialize(this, transform: new() {
			[nameof(weaponType)] = (string s) => tc.Lookup<ItemType>(s).Weapon
		});
	}
	public string GetDesc(PlayerShip player, Item item) =>
		$"Recharged {item.name}";
	public void Invoke(SceneCtx c, Item item, Action callback = null) {
		c.Go(SMenu.RechargeWeaponFromItem(c, item, this, callback));
		c.playerShip.cargo.Remove(item);
		callback?.Invoke();
	}
}
public record UnlockPrescience() : ItemUse {
	Power prescience = new(new() {
		codename="power_prescience",
		name="PRESCIENCE",
		cooldownTime=9000,
		invokeDelay=90,
		message="You invoked PRESCIENCE!",
		Effect = new()
	});
	public string GetDesc(PlayerShip player, Item item) =>
		$"Read book";
	public void Invoke(IScene prev, PlayerShip player, Item item, Action callback = null) {
		if (player.powers.Contains(prescience)) {
			player.AddMessage(new Message("You already have PRESCIENCE!"));
		} else {
			player.powers.Add(prescience);
			player.AddMessage(new Message("You have gained PRESCIENCE!"));
		}
		callback?.Invoke();
	}
}
public record ApplyMod() : ItemUse {
	[Par] FragmentMod mod;
	public ApplyMod(XElement e) : this() {
		e.Initialize(this);
	}
	public string GetDesc(PlayerShip player, Item item) =>
		$"Apply modifier to item (shows menu)";
	public void Invoke(SceneCtx c, Item item, Action callback = null) {
		c.Go(SMenu.SetMod(c, item, mod, callback));
	}
}
public record ItemType : IDesignType {
	[Req] public string codename;
	[Req] public string name;
	[Opt] public string desc = "";
	[Req] public int level;
	[Req] public int mass;
	[Opt] public int value = 0;
	[Opt(separator = ";")] public HashSet<string> attributes = new();
	[Sub] public FragmentDesc Ammo;
	[Sub] public ArmorDesc Armor;
	[Sub] public EngineDesc Engine;
	[Sub] public ReactorDesc Reactor;
	[Sub] public ServiceDesc Service;
	[Sub] public ShieldDesc Shield;
	[Sub] public SolarDesc Solar;
	[Sub(construct = false)] public LauncherDesc Launcher;
	[Sub(construct = false)] public WeaponDesc Weapon;
	[Par(construct = false, fallback = true)] public ItemUse Invoke;
	[Opt] public Dictionary<(int, int), Tile> sprite = [];


	public bool HasAtt(string att) => attributes.Contains(att);
	public T Get<T>() =>
		(T)new Dictionary<Type, object>{
			[typeof(FragmentDesc)] = Ammo,
			[typeof(ArmorDesc)] = Armor,
			[typeof(EngineDesc)] = Engine,
			[typeof(LaunchDesc)] = Launcher,
			[typeof(ReactorDesc)] = Reactor,
			[typeof(ServiceDesc)] = Service,
			[typeof(ShieldDesc)] = Shield,
			[typeof(SolarDesc)] = Solar,
			[typeof(WeaponDesc)] = Weapon,
			[typeof(ItemUse)] = Invoke
		}[typeof(T).GetType()];
	public enum EItemUse {
		none,
		fireWeapon,
		deployShip,
		deployStation,
		installWeapon,
		repairArmor,
		invokePower,
		refuel,
		depleteTargetShields,
		replaceDevice,
		unlockPrescience
	}

	class InvokeFrom {
		[Opt(type = typeof(EItemUse))] public ItemUse invoke;
		public InvokeFrom(Assets tc, XElement e) => e.Initialize(this, transform: new() {
			[nameof(invoke)] = (EItemUse u) => Create(u, tc, e)
		});
		public static ItemUse Create(EItemUse u, Assets tc, XElement e) => u switch {
			EItemUse.none => null,
			EItemUse.deployShip => new DeployShip(tc, e),
			EItemUse.deployStation => new DeployStation(tc, e),
			EItemUse.installWeapon => new InstallWeapon(),
			EItemUse.repairArmor => new RepairArmor(e),
			EItemUse.invokePower => new InvokePower(tc, e),
			EItemUse.refuel => new Refuel(tc, e),
			EItemUse.depleteTargetShields => new DepleteTargetShields(e),
			EItemUse.replaceDevice => new ReplaceDevice(tc, e),
			EItemUse.unlockPrescience => new UnlockPrescience(),
			_ => throw new Exception("Unsupported use type")
		};
	}
	public void Initialize (Assets tc, XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(Launcher)] = (XElement e) => new LauncherDesc(tc, e),
			[nameof(Weapon)] = (XElement e) => new WeaponDesc(tc, e),
			[nameof(Invoke)] = (XElement e) => new InvokeFrom(tc, e).invoke
		});
		if(e.TryAtt("sprite", out var _sprite)) {
#if GODOT
			_sprite = $"{_sprite}_gd";
#endif
			sprite = ImageLoader.ReadTile(Assets.GetSprite(_sprite)).ToDictionary(
				pair => (X: pair.Key.X, Y: -pair.Key.Y),
				pair => new Tile(pair.Value.Foreground, pair.Value.Background, pair.Value.Glyph));
		}
	}
}
public record ArmorDesc() {
	[Req] public int maxHP;
	[Opt] public int initialHP = -1;
	[Opt] public double radioRegenRate;
	[Opt] public double radioDegradeRate;
	[Opt] public double recoveryFactor;
	[Opt] public double recoveryRate;
	[Opt(alias = "regenRate")]
	public double freeRegenRate;
	[Opt] public int killHP;
	[Opt] public double stealth;
	/// <summary>For every damage HP taken, the maxHP decreases by this amount.</summary>
	[Opt] public double lifetimeDegrade = 1/50.0;
	[Opt] public double reflectFactor;
	[Opt] public int minAbsorb = 0;
	[Opt] public int powerUse = -1;
	[Opt] public int maxAbsorb = -1;

	/// <summary>This armor is more resistant to attacks in the silent dimension</summary>
	[Opt] public bool silenceStrength;
	/// <summary>This armor protects its user from Silence attacks</summary>
	[Opt] public double silenceResist;
	[Sub] public TitanDesc Titan;
	[Sub] public ItemFilter RestrictRepair;
	public Armor GetArmor(Item i) => new(i, this);
	public ArmorDesc(XElement e) : this() {
		e.Initialize(this);
	}
	public record TitanDesc() {
		/// <summary>Of the damage taken, this proportion converts to titan HP</summary>
		[Opt] public double gain = 1.0;
		/// <summary>Titan HP can be raised up to <c>desc.maxHP * factor</c></summary>
		[Opt] public double factor = 1.0;
		/// <summary>Titan HP decreases by this amount each second</summary>
		[Opt] public double decay = 1.0;
		/// <summary>The amount of damage-free time before Titan HP starts decaying</summary>
		[Opt] public double duration = 1.0;
		public TitanDesc(XElement e) : this() => e.Initialize(this);
	}
}
public record EngineDesc {
	[Req] public int powerUse;
	[Req] public double thrust;
	[Req] public double maxSpeed;
	[Req] public double rotationMaxSpeed;
	[Req] public double rotationDecel;
	[Req] public double rotationAccel;
	public Engine GetEngine(Item i) => new(i, this);
	public EngineDesc() { }
	public EngineDesc(XElement e) {
		e.Initialize(this);
	}
}
public record EnhancerDesc {
	[Req] public int powerUse;
	[Par] public FragmentMod mod;
	public Enhancer GetEnhancer(Item i) => new(i, this);
	public EnhancerDesc() { }
	public EnhancerDesc(XElement e) {
		e.Initialize(this);
	}
}
public record LaunchDesc {
	[Req(parse = false)] public ItemType ammoType;
	public FragmentDesc shot;
	public LaunchDesc() {}
	public LaunchDesc(Assets types, XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(ammoType)] = (string s) => types.Lookup<ItemType>(s),
		});
		
		shot = ammoType.Ammo ?? new(e);
	}
}
public record LauncherDesc {
	[Req] public int powerUse;
	[Req] public int fireCooldown;
	[Opt] public int recoil = 0;
	[Opt] public int repeat = 0;
	[Sub] public CapacitorDesc Capacitor;
	public List<LaunchDesc> missiles;
	public Launcher GetLauncher(Item i) => new(i, this);
	public Weapon GetWeapon(Item i) => new(i, weaponDesc);
	public LauncherDesc() { }
	public LauncherDesc(Assets types, XElement e) {
		e.Initialize(this);
		missiles = new();
		if(e.HasElements("Missile", out var xmlMissileArr)) {
			missiles.AddRange(xmlMissileArr.Select(m => new LaunchDesc(types, m)));
		}
	}
	public WeaponDesc weaponDesc => new() {
		powerUse = powerUse,
		fireCooldown = fireCooldown,
		recoil = recoil,
		repeat = repeat,
		Projectile = missiles.First().shot,
		initialCharges = -1,
		Capacitor = Capacitor,
		ammoType = missiles.First().ammoType,
		targetProjectile = false,
		autoFire = false
	};
}
public record WeaponDesc {
	[Req] public int powerUse;
	[Req] public int fireCooldown;
	[Opt] public int recoil = 0;
	[Opt] public int repeat = 0;
	[Opt] public int repeatDelay = 3;
	[Opt] public int repeatDelayEnd = 3;
	[Opt] public double failureRate = 0;
	[Opt] public int initialCharges = -1;

	[Opt] public bool pointDefense;
	public bool targetProjectile;
	[Opt] public bool autoFire;
	[Opt] public bool spray;
	[Opt] public bool structural;
	[Opt] public bool omnidirectional = false;
	[Opt] public double angle = 0, sweep, leftRange, rightRange = 0;

	
	[Opt(parse = false)] public byte[] sound;
	[Opt(parse = false)] public ItemType ammoType;
	[Sub(required = true)] public FragmentDesc Projectile;
	[Sub] public CapacitorDesc Capacitor;

	public int barrageSize => (repeat + 1) * Projectile.count;
	public int missileSpeed => Projectile.missileSpeed;
	public int damageType => Projectile.damageType;
	public IDice damageHP => Projectile.damageHP;
	public int lifetime => Projectile.lifetime;
	public int minRange => Projectile.missileSpeed * Projectile.lifetime / (Constants.TICKS_PER_SECOND * Constants.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
	public Weapon GetWeapon(Item i) => new(i, this);
	public WeaponDesc() { }
	public WeaponDesc(Assets types, XElement e) {
		var toRad = (double d) => d * Math.PI / 180;
		e.Initialize(this, transform:new() {
			[nameof(angle)] = toRad,
			[nameof(sweep)] = toRad,
			[nameof(leftRange)] = toRad,
			[nameof(rightRange)] = toRad,

			[nameof(ammoType)] = (string at) => types.Lookup<ItemType>(at),
			[nameof(sound)] = (string s) => Assets.GetAudio(s)
		});
		//Projectile = new(e.ExpectElement("Projectile"));
		//sound = e.TryAtt("sound", out string s) ? new SoundBuffer(s) : null;
		if (pointDefense) {
			Projectile.hitProjectile = true;
			targetProjectile = true;
			autoFire = true;
		}
		if (spray) {
			autoFire = true;
		}
	}
}
public record FragmentDesc {
	[Opt] public int count = 1;
	[Opt] public bool? targetLocked;
	[Opt] public bool omnidirectional;
	[Opt] public double spreadAngle;
	[Opt] public bool spreadOmni;
	[Req] public int missileSpeed;
	[Req] public int damageType;
	[Req] public IDice damageHP;
	[Opt] public double antiReflect;
	/// <summary>No more than this many projectiles from the same burst can hit the target</summary>
	[Opt] public int hitCap = int.MaxValue;

	[Opt] public double detonateFailChance;

	[Opt] public int knockback;
	[Opt] public int shock = 1;
	[Req] public int lifetime;
	[Opt] public bool passthrough;
	///<summary>Subfragments cannot hit objects that are excluded by this fragment</summary>
	[Opt] public bool precise = true;
	[Opt] public int armorSkip = 0;
	/// <summary>If armor integrity ratio is below this amount, then we bypass the shield completely</summary>
	[Opt] public double shieldDrill;
	/// <summary>If armor integrity ratio is below this amount, then we bypass the armor completely and go to the next layer</summary>
	[Opt] public double armorDrill = 0;
	[Opt] public double shieldFactor = 1;
	[Opt] public double armorFactor = 1;
	/// <summary>If true, then the weapon has Targeting</summary>
	[Opt] public bool acquireTarget;
	[Opt] public bool multiTarget;
	[Opt] public double maneuver;
	[Opt] public double maneuverRadius;
	[Opt] public int detonateRadius;
	[Opt] public int fragmentInitialDelay;
	[Opt] public int fragmentInterval;
	[Opt] public double fragmentSpin;
	[Opt] public bool hitSource;
	[Opt] public bool hitProjectile;
	[Opt] public bool hitBarrier = true;
	[Opt] public bool hitNonTarget = true;
	[Opt] public bool magic;
	[Opt] public IDice blind;
	[Opt] public int ricochet;
	[Opt] public int tracker;

	/// <summary>If the target shield is up, sets the remaining delay to at least this value</summary>
	[Opt] public int shieldDelay = 0;
	/// <summary>If the target shield is down, sets the remaining depletion delay to at least this value</summary>
	[Opt] public int shieldSuppress = 0;


	//[Opt] public bool beacon;
	[Opt] public bool hook;
	/// <summary>On hit, the projectile attaches an overlay that automatically makes future shots hit instantly</summary>
	[Opt] public bool lightning;

	[Opt] public double silenceFactor;
	/// <summary>Inflicts silence on the target</summary>
	[Opt] public double silenceInflict;
	[Sub] public FlashDesc Flash;
	[Sub] public CorrodeDesc Corrode;
	[Sub] public DisruptorDesc Disruptor;


	public double CalcSilenceRatio(double targetSilence) => FragmentDesc.GetSilenceMatch(silenceFactor, targetSilence);
	/// <summary>Calculates the total damage dealt (silent plus non-silent)</summary>
	public static double GetSilenceMatch(double silenceFactor, double targetSilence) {
		var s = Math.Min(1, targetSilence);
		return (silenceFactor * s) + ((1 - silenceFactor) * (1 - s));
		//0.0 * 1.0 + 1.0 * 1.0

		//.30 * .70 + .70 * .3
		//.21 + .21
		//.42
		
		//.70 * .70 + .30 * .30
		//.49 + .9
		//.58

		//.90 * .10 + .10 * .90
		//.09 + .09
		//.18

		//.40 * .80 + .60 * .20
		//.32 + .12
		//.44
	}

	public int range => missileSpeed * lifetime / Constants.TICKS_PER_SECOND;
	public double angleInterval => spreadAngle / count;
	public int range2 => range * range;
	[Sub(required = false, multiple = true)] public HashSet<FragmentDesc> Fragment = new();
	[Par] public StaticTile effect;
	[Sub] public TrailDesc Trail;

	[Opt(parse = false)] public byte[] detonateSound;
	public FragmentDesc() { }
	public FragmentDesc(XElement e) {
		Initialize(e);
		/*
		if(e.HasElements("Fragment", out var xmlFragmentList)) {
			Fragment.UnionWith(xmlFragmentList.Select(xmlFragment => new FragmentDesc(xmlFragment)));
		}
		*/
	}
	public void Initialize(XElement e) {
		e.Initialize(this, transform: new() {
			[nameof(count)] = (int c) => {
				//init default
				this.spreadAngle = count == 1 ? 0 : 3 * Math.PI / 180;
				return c;
			},
			[nameof(detonateSound)] = (string s) => Assets.GetAudio(s),
			[nameof(spreadAngle)] = (double d) => d * Math.PI / 180,
			[nameof(spreadOmni)] = (bool b) => {
				spreadAngle = 2 * Math.PI / count;
				return b;
			},
			[nameof(maneuver)] = (double d) => {
				acquireTarget |= d > 0;
				return d * Math.PI / 180; },
		});
		/*
		if(e.HasElement("Flash", out var xmlFlash)) {
			Flash = new(xmlFlash);
		}
		if(e.HasElement("Decay", out var xmlDecay)) {
			Decay = new(xmlDecay);
		}
		*/
		/*
		if (e.HasElements("Fragment", out var fragmentsList)) {
			Fragment = new(fragmentsList.Select(f => new FragmentDesc(f)));
		}
		*/
		/*
		disruptor = e.HasElement("Disruptor", out var xmlDisruptor) ?
			new(xmlDisruptor) : null;
		Trail = e.HasElement("Trail", out var xmlTrail) ? 
			new(xmlTrail) : null;
		*/
	}
	public IEnumerable<double> GetAngles(double direction) =>
		Enumerable.Range(0, count).Select(i => direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1));
	public List<Projectile> CreateProjectiles(ActiveObject owner, List<ActiveObject> targets, double direction, XY offset = null, HashSet<Entity> exclude = null /*, int projectilesPerTarget = 0*/) {
		var position = owner.position + (offset ?? new(0, 0));
		var i = 0;
		var adj = count % 2 == 0 ? -angleInterval / 2 : 0;
		
		Func<Maneuver> getManeuver = targets switch {
			{ Count: 1 } => () => GetManeuver(targets.First()),
			{ Count: > 1 } => () => GetManeuver(targets[i++ % targets.Count]),
			_ => () => null
		};
		var angles = GetAngles(direction + adj);
		/*
		if(projectilesPerTarget > 0 && targets?.Any() == true) {
			angles = angles.Take(projectilesPerTarget * targets.Count);
		}
		*/
		var burst = new Projectile.Burst();
		burst.projectiles.AddRange(angles.Select(angle =>
			new Projectile(owner, this,
				position + XY.Polar(angle),
				owner.velocity + XY.Polar(angle, missileSpeed),
				angle,
				getManeuver(),
				exclude
				) { burst = burst }
		));
		return burst.projectiles;
	}
	public Maneuver GetManeuver(ActiveObject target) =>
		(acquireTarget && target != null) ? new(target, maneuver, maneuverRadius) : null;
}
public record TrailDesc : ITrail {
	[Req] public int lifetime;
	[Req(parse = false)] public char glyph;
	[Req(parse = false)] public uint foreground;
	[Req(parse = false)] public uint background;
	public TrailDesc() { }
	public TrailDesc(XElement e) {
		e.Initialize(this, transform: new() {
			["foreground"] = (string s) => ABGR.Parse(s),
			["background"] = (string s) => ABGR.Parse(s),
			["glyph"] = (string s) => s.Length == 1 ? s[0] : (char)uint.Parse(s[1..])
		});
	}
	public Effect GetParticle(XY Position, XY Velocity = null) => new FadingTile(Position, new(foreground, background, glyph), lifetime);
}
public record DisruptorDesc {
	[Opt(parse = false)] bool? thrustMode, turnMode, brakeMode, fireMode;
	[Opt] public int lifetime = 60;
	public DisruptorDesc() { }
	public DisruptorDesc(XElement e) {
		e.Initialize(this, transform:new() {
			[nameof(thrustMode)] = GetMode,
			[nameof(turnMode)] = GetMode,
			[nameof(brakeMode)] = GetMode,
			[nameof(fireMode)] = GetMode,
		});
	}
	public Disrupt GetHijack() => new() {
		thrustMode = thrustMode,
		turnMode = turnMode,
		brakeMode = brakeMode,
		fireMode = fireMode,
		ticksLeft = lifetime
	};
	public bool? GetMode(string str) => str switch {
		"on" => true,
		"off" => false,
		"none" => null,
		null => null,
		_ => throw new Exception($"Invalid value {str}")
	};
}
public record CapacitorDesc {
	[Opt] public double minChargeToFire = 0;
	[Req] public double dischargeOnFire,
						rechargePerTick,
						maxCharge;
	[Opt] public double bonusSpeedPerCharge,
						bonusDamagePerCharge,
						bonusLifetimePerCharge;
	
	public CapacitorDesc() { }
	public CapacitorDesc(XElement e) {
		e.Initialize(this);
	}
}
public record ReactorDesc {
	[Req] public int maxOutput;
	[Req] public int capacity;
	[Opt] public bool startFull = true;
	[Opt] public double efficiency = 1;
	[Opt] public double combatFactor = 1.0;
	//[Opt] public double minOutput = 0;
	[Opt] public double degradeDelay = 0;
	[Opt] public double lifetimeDegrade = 0;
	//[Opt] public double maxInput = 0;
	//public bool isBattery => maxInput > 0;
	[Opt] public bool allowRefuel = true;

	/// <summary> If true, always consumes full rate</summary>
	[Opt] public bool fixedOutput = false;

	/// <summary>
	/// Recharge using power from other reactors when available
	/// </summary>
	[Opt] public bool rechargeable = false;
	/// <summary>
	/// Maximum input rate (if this Reactor is a battery). Default is maxOutput.
	/// </summary>
	[Opt] public int maxInput;

	public Reactor GetReactor(Item i) => new(i, this);
	public ReactorDesc() { }
	public ReactorDesc(XElement e) =>
		e.Initialize(this, fallback: new() {
			[nameof(maxInput)] = () => this.maxOutput
		});
}
public enum ServiceType {
	missileJack,
	armorRepair,
	grind
}
public record ServiceDesc {
	[Req] public ServiceType type;
	[Req] public int powerUse;
	[Req] public int interval;
	public Service GetService(Item i) => new(i, this);
	public ServiceDesc() { }
	public ServiceDesc(XElement e) {
		e.Initialize(this);
	}
}
public record ShieldDesc {
	[Req] public int powerUse, idlePowerUse;
	[Req] public int maxHP;
	[Req] public int damageDelay, depletionDelay;
	[Req] public double regen;
	[Opt] public double absorbFactor = 1;
	[Opt] public int stealth;

	[Opt] public double reflectFactor = 0;
	public Shield GetShield(Item i) => new(i, this);
	public ShieldDesc() { }
	public ShieldDesc(XElement e) {
		e.Initialize(this);
	}
}
public record SolarDesc {
	[Req] public int maxOutput;
	[Opt] public int durability = -1 + 0 * 360000;
	public Solar GetSolar(Item i) => new(i, this);
	public SolarDesc() { }
	public SolarDesc(XElement e) {
		e.Initialize(this);
	}
}
