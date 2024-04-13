namespace RogueFrontier;

public interface Event
{
    bool active { get; }
    void Update(double delta);
}
