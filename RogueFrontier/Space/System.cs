using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

class EffectLocator : ILocator<Effect, (int, int)> {
    public (int, int) Locate(Effect e) => (e.position.xi, e.position.yi);
}
class EntityLocator : ILocator<Entity, (int, int)> {
    public (int, int) Locate(Entity e) => (e.position.xi, e.position.yi);
}

public record EntityAdded(Entity e);


public interface ISystem {

}
public class SilentSystem {
    public PlayerShip playerShip;
}
public class System {
    [JsonIgnore]
    public static readonly System empty = new(new());

    public string id, name;

    public HashSet<Event> events = new();
    public List<Event> eventsAdded = new();
    public List<Event> eventsRemoved = new();

    public LocatorDict<Entity, (int, int)> entities = new(new EntityLocator());
    public List<Entity> entitiesAdded = new();
    public List<Entity> entitiesRemoved = new();
    public Vi<EntityAdded> onEntityAdded = new();
    public LocatorDict<Effect, (int, int)> effects = new(new EffectLocator());
    public List<Effect> effectsAdded = new();
    public List<Effect> effectsRemoved = new();

    public List<Star> stars = new();

    public Universe universe;
    [JsonIgnore]
    public TypeCollection types => universe.types;
    [JsonIgnore]
    public Rand karma => universe.karma;
    public Backdrop backdrop=new();

    public double time;
    public int tick;
    public ulong nextId;

    private bool updating;

    public System() {
        this.universe = new();
        universe.systems["origin"] = this;
        onEntityAdded += universe;
    }
    public System(Universe universe) {
        this.universe = universe;
    }
    public void AddEvent(Event e) => eventsAdded.Add(e);
    public void AddEffect(Effect e) => effectsAdded.Add(e);
    public void AddEntity(Entity e) => entitiesAdded.Add(e);
    public void RemoveEvent(Event e) => eventsRemoved.Add(e);
    public void RemoveEffect(Effect e) => effectsRemoved.Add(e);
    public void RemoveEntity(Entity e) => entitiesRemoved.Add(e);
    public void RemoveAll() {
        events.Clear();
        entities.Clear();
        effects.Clear();
        eventsAdded.Clear();
        entitiesAdded.Clear();
        effectsAdded.Clear();
        eventsRemoved.Clear();
        entitiesRemoved.Clear();
        effectsRemoved.Clear();
    }
    public void UpdateAdded() {
        events.UnionWith(eventsAdded);
        entities.all.UnionWith(entitiesAdded);
        effects.all.UnionWith(effectsAdded);

        entitiesAdded.ForEach(e => onEntityAdded.Observe(new(e)));

        eventsAdded.Clear();
        entitiesAdded.Clear();
        effectsAdded.Clear();
    }
    public void UpdateRemoved() {
        events.ExceptWith(eventsRemoved);
        entities.all.ExceptWith(entitiesRemoved);
        effects.all.ExceptWith(effectsRemoved);
        eventsRemoved.Clear();
        entitiesRemoved.Clear();
        effectsRemoved.Clear();

        entities.all.RemoveWhere(e => !e.active);
        effects.all.RemoveWhere(e => !e.active);
        events.RemoveWhere(e => !e.active);
    }
    public void UpdatePresent() {
        UpdateAdded();
        UpdateRemoved();
    }
    public void UpdateSpace() {
        //Place everything in the grid
        entities.UpdateSpace();
        effects.UpdateSpace();
    }
    public void UpdateActive(double delta) {
        UpdateSpace();

        //updating = true;
        //Update everything
        foreach (var e in entities.all) {
            e.Update(delta);
        }
        foreach (var e in effects.all) {
            e.Update(delta);
        }
        foreach (var e in events) {
            e.Update(delta);
        }
        //updating = false;
        time += delta;
        tick++;
    }

    public void PlaceTiles(Dictionary<(int, int), ColoredGlyph> tiles) {
        foreach (var e in entities.all) {
            if (e.tile == null) continue;
            var p = e.position.roundDown;
            if (!tiles.ContainsKey(p) || e is ISegment) {
                tiles[p] = e.tile;
            }
        }
        foreach (var e in effects.all) {
            if (e.tile == null) continue;
            var p = e.position.roundDown;
            if (!tiles.ContainsKey(p)) {
                tiles[p] = e.tile;
            }
        }
    }
    public void PlaceTilesVisible(Dictionary<(int, int), ColoredGlyph> tiles, Func<Entity, double> getVisibleDistanceLeft) {
        
        Dictionary<(int, int), List<ColoredGlyph>> all = new();
        List<ColoredGlyph> Initialize((int, int) key) => 
            all.TryGetValue(key, out var l) ? l : all[key] = new(10);
        foreach (var e in entities.all) {
            if (e.tile == null) continue;
            var dist = getVisibleDistanceLeft(e);
            if (dist<0) {
                continue;
            }
            var t = e.tile;
            const double threshold = 8;
            if (dist < threshold) {
                t = t.Clone() as ColoredGlyph;
                t.Foreground = t.Foreground.SetAlpha((byte)(255 * dist / threshold));
            }
            var p = e.position.roundDown;
            Initialize(p).Add(t);
        }
        foreach (var e in effects.all) {
            if (e.tile == null) continue;
            var p = e.position.roundDown;
            Initialize(p).Add(e.tile);
        }

        int time = tick / 20;
        foreach((var p, var set) in all) {
            tiles[p] = set[time % set.Count];
        }
    }

    public void PlaceTilesOver(Dictionary<(int, int), ColoredGlyph> tiles, Func<Entity, double> getVisibleDistanceLeft) {
        //Add probabilistic overwrites
        //To do: Handle stealth
        foreach (var e in entities.all) {
            if (e.tile == null) continue;


            var dist = getVisibleDistanceLeft(e);
            if (dist < 0) {
                continue;
            }

            var t = e.tile;
            const double threshold = 16;
            if (dist < threshold) {
                t = t.Clone() as ColoredGlyph;
                t.Background = t.Background.SetAlpha((byte)(255 * dist / threshold));
            }

            tiles[e.position.roundDown] = t;
        }
        foreach (var e in effects.all) {
            if (e.tile == null) continue;
            tiles[e.position.roundDown] = e.tile;
        }
    }

    public Stargate FindGateTo(System to) => universe.FindGateTo(this, to);

    public record SoundPlayed(XY position, SoundBuffer sb);
    public Vi<SoundPlayed> onSoundPlayed = new();
    public void PlaySound(XY position, SoundBuffer sb) {
        onSoundPlayed.Observe(new(position, sb));
    }
}
