namespace RogueFrontier;
public class Disrupt {
    public int ticksLeft;
    public bool? thrustMode;
    public bool? turnMode;
    public bool? brakeMode;
    public bool? fireMode;
    public bool active => ticksLeft > 0;
    public void Update() {
        ticksLeft--;
    }
}
