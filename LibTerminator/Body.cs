namespace LibTerminator;
public class Body {
	public HashSet<BodyPart> active;
	public BodyPart[] vision;
	public BodyPart[] hearing;
	public BodyPart[] grasping;


	static BodyPart makeArm () {
		var arm = new BodyPart("Arm");
		var hand = new BodyPart("Hand");
		BodyPart.Connect(arm, hand);
		return arm;
	}
	static BodyPart makeLeg () {
		var arm = new BodyPart("Leg");
		var hand = new BodyPart("Foot");
		BodyPart.Connect(arm, hand);
		return arm;
	}
	public HashSet<BodyPart> human => new Lazy<HashSet<BodyPart>>(() => {
		var b = (string s) => new BodyPart(s);
		BodyPart
			head		= b("Head"),
			body		= b("Body"),
			arm_l		= makeArm(),
			arm_r		= makeArm(),
			leg_l		= makeLeg(),
			leg_r		= makeLeg()
			;
		Dictionary<BodyPart, BodyPart[]> parts = new(){
			[head] = [body],
			[body] = [arm_l, arm_r, leg_l, leg_r],
		};
		return head.GetGraph();
	}).Value;
	public HashSet<BodyPart> insect => new Lazy<HashSet<BodyPart>>(() => {
		var b = (string s) => new BodyPart(s);
		BodyPart
			head = b("Head"),
			body = b("Body"),
			arm_l_i = makeArm(),
			arm_r_i = makeArm(),
			arm_l_ii = makeArm(),
			arm_r_ii = makeArm(),
			leg_l = makeLeg(),
			leg_r = makeLeg()
			;
		Dictionary<BodyPart, BodyPart[]> parts = new() {
			[head] = [body],
			[body] = [arm_l_i, arm_r_i, arm_l_ii, arm_r_ii, leg_l, leg_r],
		};
		return head.GetGraph();
	}).Value;
}
public class BodyPart {
	public static void ConnectAll(Dictionary<BodyPart, BodyPart[]> parts) {
		foreach(var (key, val) in parts) {
			foreach(var v in val) {
				Connect(key, v);
			}
		}
	}
	public static void ConnectAll (BodyPart[][] parts) {
		foreach(var line in parts) ConnectAll(line);
	}
	public static void ConnectAll(BodyPart[] pair) {
		foreach(var (i,p) in pair.Index().Skip(1)) {
			Connect(pair[i-1], p);
		}
	}
	public static void Connect(BodyPart left, BodyPart right) {
		left.connected.Add(right);
		right.connected.Add(left);
	}
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
		void Check (BodyPart start) {
			if(seen.Contains(start)) {
				return;
			}
			seen.Add(start);
			foreach(var b in start.connected)
				Check(b);
		}
		Check(this);
		return seen;
	}

	string name;
	public HashSet<BodyPart> connected = [];
	public BodyPart(string name) {
		this.name = name;
	}
}