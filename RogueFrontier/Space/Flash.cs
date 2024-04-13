using Common;
using RogueFrontier;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Linq;
using System.Xml.Linq;
using Sys = RogueFrontier.System;
public record FlashDesc(){
    [Req] public int intensity;
    public FlashDesc(XElement e) : this() {
        e.Initialize(this);
    }
    public void Create(Sys world, XY position) {
        var center = new Center(position, (int)(255 * Math.Sqrt(intensity)), 60);
        world.AddEffect(center);
        int radius = (int)(Math.Sqrt(intensity) * 1.5);
        var particles = Enumerable.Range(-radius*2, radius * 2 * 2)
            .SelectMany(x => Enumerable.Range(-radius*2, radius * 2 * 2).Select(y => new XY(x, y)))
            .Where(p => (p - position).magnitude > radius)
            .Select(p => new Particle(center, center.position + p))
            .Where(p => p.active)
            .ToList();
        particles.ForEach(world.AddEffect);
    }
    public class Center : Effect {
        public XY position { get; set; }
        public int maxBrightness;
        public int maxLifetime;
        public double lifetime;
        public int brightness => (int) (maxBrightness * Math.Sqrt(lifetime / maxLifetime));
        public bool active => brightness>128;
        public ColoredGlyph tile => new(Color.Transparent, new(255, 255, 255, brightness), ' ');
        public Center(XY position, int brightness, int lifetime) {
            this.position = position;
            this.maxBrightness = brightness;
            this.maxLifetime = lifetime;
            this.lifetime = lifetime;
        }
        public void Update(double delta) {
            lifetime -= delta * Program.TICKS_PER_SECOND;
        }
    }
    public class Particle : Effect {
        public Center parent;
        public XY position { get; set; }
        public double distance;
        public int brightness => (int)(parent.brightness / distance);
        public bool active => brightness> 128;
        public ColoredGlyph tile => delay > 0 ? null : new(Color.Transparent, new(255, 255, 255, brightness), ' ');

        public double delay;
        public Particle(Center parent, XY position) {
            this.parent = parent;
            this.position = position;
            distance = (parent.position - position).magnitude;
            delay = distance / 64;
        }
        public void Update(double delta) {
            delay -= delta;
        }
    }
}
