using Common;
using System;
using System.Linq;
using LibGamer;
namespace RogueFrontier;
public class Heading : Effect {
    public IShip parent;
    public Heading(IShip parent) {
        this.parent = parent;
    }
    public XY position => parent?.position ?? new();
    public bool active => parent.active;
    public Tile tile => null;
    int ticks;
    public EffectParticle[] particles;
    public void Update(double delta) {
        if (parent.dock.docked == true) {
            ticks = 0;
            return;
        }
        const int interval = 30;
        XY start = parent.position;
        int step = 2;
        XY inc = XY.Polar(parent.rotationDeg * Math.PI / 180, 1) * step;
        if (ticks == 0) {

            //Idea: Highlight a segment of the aimline based on the firetime left on the weapon
            int length = 16;
            int count = length / step;
            particles = new EffectParticle[count];
            for (int i = 0; i < count; i++) {
                var point = start + inc * (i + 1);
                var value = (byte)(153 - Math.Max(1, i) * 153 / length);
                var cg = new Tile(ABGR.RGB(value, value, value), ABGR.Transparent, (char)249);
                var particle = new EffectParticle(point, cg, interval + 1);
                particles[i] = particle;
                parent.world.AddEffect(particle);
            }
        } else {
            for (int i = 0; i < particles.Length; i++) {
                var p = particles[i];
                p.position = start + inc * (i + 1);
                p.lifetime = interval + 1 + (interval * (i - particles.Length)) / particles.Length;
            }
        }
        ticks++;

    }
    public static void AimLine(System World, XY start, double angle, int lifetime = 1) {
        //ColoredGlyph pointEffect = new ColoredGlyph((char)249, new Color(153, 153, 76), Color.Transparent);
        var pointEffect = new Tile(ABGR.RGBA(255, 255, 0, 204), ABGR.Transparent, (char)249);
        var point = start;
        var inc = XY.Polar(angle);
        int length = 20;
        int interval = 2;
        for (int i = 0; i < length / interval; i++) {
            point += inc * interval;
            World.AddEffect(new EffectParticle(point, pointEffect, lifetime));
        }
    }
    public static void AimLine(ActiveObject owner, double angle, Weapon w) {
        //Idea: Highlight a segment of the aimline based on the firetime left on the weapon

        var start = owner.position;
        var World = owner.world;

        //ColoredGlyph pointEffect = new ColoredGlyph((char)249, new Color(153, 153, 76), Color.Transparent);
        //ColoredGlyph dark = new ColoredGlyph(new Color(255, 255, 0, 102), Color.Transparent, (char)249);
        var bright = new Tile(ABGR.RGBA(255, 255, 0, 204), ABGR.Transparent, (char)249);
        var point = start;
        var inc = XY.Polar(angle);
        //var length = w.target == null ? 20 : (w.target.Position - owner.Position).Magnitude;
        var length = 20;
        int interval = 2;

        var points = length / interval;
        //var highlights = points * (1 - w.fireTime / w.desc.fireCooldown);

        for (int i = 0; i < points; i++) {
            point += inc * interval;
            //World.AddEffect(new EffectParticle(point, i < highlights ? bright : dark, 1));
            World.AddEffect(new EffectParticle(point, bright, 1));
        }
    }
    public static void Crosshair(System World, XY point, uint foreground) {
        //Color foreground = new Color(153, 153, 153);
        var background = ABGR.Transparent;
        var cg = (uint c) => new Tile(foreground, background, c);
        World.AddEffect(new EffectParticle(point + (1, 0), cg('-'), 1));
        World.AddEffect(new EffectParticle(point + (-1, 0), cg('-'), 1));
        World.AddEffect(new EffectParticle(point + (0, 1), cg('|'), 1));
        World.AddEffect(new EffectParticle(point + (0, -1), cg('|'), 1));
    }
    public static void Box(Station st, uint foreground) {
        //Color foreground = new Color(153, 153, 153);
        var background = ABGR.Transparent;
        var p = st.segments.Select(s => s.position).Concat([st.position]);
        var xMin = p.Min(p => p.x);
        var xMax = p.Max(p => p.x);
        var yMin = p.Min(p => p.y);
        var yMax = p.Max(p => p.y);
        var f = st.world.AddEffect;
        var t = (int c) => new Tile(foreground, background, c);

        
        f(new EffectParticle(new(xMin - 1, yMin - 1), t(BoxInfo.IBMCGA.glyphFromInfo[new(n: Line.Single, e: Line.Single)]), 1));
        f(new EffectParticle(new(xMin - 1, yMax + 1), t(BoxInfo.IBMCGA.glyphFromInfo[new(s: Line.Single, e: Line.Single)]), 1));
        f(new EffectParticle(new(xMax + 1, yMin - 1), t(BoxInfo.IBMCGA.glyphFromInfo[new(n: Line.Single, w: Line.Single)]), 1));
        f(new EffectParticle(new(xMax + 1, yMax + 1), t(BoxInfo.IBMCGA.glyphFromInfo[new(s: Line.Single, w: Line.Single)]), 1));
    }
}
