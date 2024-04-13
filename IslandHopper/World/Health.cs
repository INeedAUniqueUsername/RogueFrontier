namespace IslandHopper;

public class Health {
    public double bodyHP;
    public double bloodHP;
    public double bleeding;
    public Health() {
        bodyHP = 100;
        bloodHP = 200;
    }
    public void Damage(int hp) {
        bodyHP -= hp;
        bleeding = hp;
    }
    public void UpdateStep() {
        if (bleeding > 0) {
            bloodHP -= bleeding / 30;
            bleeding -= 1 / (30 * bleeding);
        }
    }
}
