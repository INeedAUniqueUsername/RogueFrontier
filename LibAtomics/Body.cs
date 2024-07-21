using Common;
using System.Xml.Linq;

namespace LibAtomics;
public class Body {
	public HashSet<BodyPart> parts;

	public void UpdateTick() {
		foreach(var p in parts) p.UpdateTick();
	}

	public Body (HashSet<BodyPart> parts) => this.parts = parts;

	public static readonly string cockroach = """

		<Body>
			<Part id="head"			name="Head"/>
			<Part id="upperBody"	name="Upper Body"	heart="true"	connect="head"/>
			<Part id="lowerBody"	name="Lower Body"					connect="upperBody"/>
		
			<Part id="leftLegA"		name="Left Foreleg"					connect="upperBody"/>
			<Part id="rightLegA"	name="Right Foreleg"					connect="upperBody"/>
			<Part id="leftLegB"		name="Left Midleg"					connect="lowerBody"/>
			<Part id="rightLegB"	name="Right Midleg"					connect="lowerBody"/>
			<Part id="leftLegC"		name="Left Hindleg"					connect="lowerBody"/>
			<Part id="rightLegC"	name="Right Hindleg"					connect="lowerBody"/>
		</Body>

		""";

	public static readonly string human = """
<Body>
	<Part id="head"			name="Head"/>
	<Part id="upperBody"	name="Upper Body"	heart="true"	connect="head"/>
	<Part id="lowerBody"	name="Lower Body"					connect="upperBody"/>

	<Part id="leftArm"		name="Left Arm"						connect="upperBody"/>
	<Part id="rightArm"		name="Right Arm"					connect="upperBody"/>
	<Part id="leftLeg"		name="Left Leg"						connect="lowerBody"/>
	<Part id="rightLeg"		name="Right Leg"					connect="lowerBody"/>
</Body>
""";
	public static HashSet<BodyPart> Parse(XElement el) {
		Action pass = () => { };
		Dictionary<string, BodyPart> parts = [];
		foreach(var e in el.Elements()) {
			var id = e.Att("id");

			var pA = parts[id] = new();
			pass += () => {
				pA.Init(e, parts);
			};
		}
		pass();
		return [.. parts.Values];
	}
}
public class BodyPart {
	public bool IsConnected(BodyPart dest) {
		HashSet<BodyPart> seen = [];
		bool Check(BodyPart start) {
			if(seen.Contains(start)) {
				return false;
			}
			if(start == dest) {
				return true;
			}
			seen.Add(start);
			return start.connected.Any(b => Check(b));
		}
		return Check(this);
	}

	public HashSet<BodyPart> GetGraph() {
		HashSet<BodyPart> seen = [];
		void Visit (BodyPart start) {
			if(seen.Contains(start)) {
				return;
			}
			seen.Add(start);
			foreach(var b in start.connected)
				Visit(b);
		}
		Visit(this);
		return seen;
	}

	/// <summary>Determines the max HP that this part can maintain. If bloodflow is low, then the body part starts atrophying</summary>
	public double bloodflow;
	public double hp = 100;
	public double hpDelta = 0;

	[Req] public string name;
	[Opt] public bool heart = false;
	public HashSet<BodyPart> connected = [];
	public BodyPart () { }

	public void Init(XElement e, Dictionary<string, BodyPart> map) {
		e.Initialize(this);
		foreach(var other in e.TryAtt("connect", "").Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => map[s])) {
			other.connected.Add(this);
			connected.Add(other);
		}
	}
	public void UpdateTick () {
		hp += hpDelta;
		hpDelta = 0;
		if(!heart) {
			var minHp = connected.Min(bp => bp.hp);
			hpDelta = -Math.Min(hp, (hp - minHp) / 30);
		}
	}
}