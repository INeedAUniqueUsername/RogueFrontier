namespace LibAtomics;
public class Body {
	public HashSet<BodyPart> active;
	public BodyPart[] vision;
	public BodyPart[] hearing;
	public BodyPart[] grasping;
	public HashSet<BodyPart> humanoid => new Lazy<HashSet<BodyPart>>(() => {
		var b = (string s) => new BodyPart(s);
		BodyPart
			head		= b("Head"),
			body		= b("Body"),
			leftArm		= b("Left Arm"),
			rightArm	= b("Right Arm"),
			leftHand	= b("Left Hand"),
			rightHand	= b("Right Hand"),
			leftLeg		= b("Left Leg"),
			leftFoot	= b("Left Foot"),
			rightLeg	= b("Right Leg"),
			rightFoot	= b("Right Foot")
			;
		Dictionary<BodyPart, BodyPart[]> parts = new(){
			[head] = [body],
			[body] = [leftArm, rightArm, leftLeg, rightLeg],
			[leftArm] = [leftHand],
			[rightArm] = [rightHand],
			[leftLeg] = [leftFoot],
			[rightLeg] = [rightFoot],
		};
		return [head, body, leftArm, rightArm, leftHand, rightHand, leftLeg, rightLeg, leftFoot, rightFoot];
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
	string name;
	public HashSet<BodyPart> connected = [];
	public BodyPart(string name) {
		this.name = name;
	}
}