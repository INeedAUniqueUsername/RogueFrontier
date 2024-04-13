using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Linq;

namespace RogueFrontier;

using LineIndex = ICellSurface.ConnectedLineIndex;

class Heading : Effect {
    public IShip parent;
    public Heading(IShip parent) {
        this.parent = parent;
    }
    public XY position => parent?.position ?? new();
    public bool active => parent.active;
    public ColoredGlyph tile => null;
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
                var value = 153 - Math.Max(1, i) * 153 / length;
                var cg = new ColoredGlyph(new Color(value, value, value), Color.Transparent, (char)249);
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
        var pointEffect = new ColoredGlyph(new Color(255, 255, 0, 204), Color.Transparent, (char)249);
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
        var bright = new ColoredGlyph(new(255, 255, 0, 204), Color.Transparent, (char)249);
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
    public static void Crosshair(System World, XY point, Color foreground) {
        //Color foreground = new Color(153, 153, 153);
        var background = Color.Transparent;
        var cg = (int c) => new ColoredGlyph(foreground, background, c);
        World.AddEffect(new EffectParticle(point + (1, 0), cg('-'), 1));
        World.AddEffect(new EffectParticle(point + (-1, 0), cg('-'), 1));
        World.AddEffect(new EffectParticle(point + (0, 1), cg('|'), 1));
        World.AddEffect(new EffectParticle(point + (0, -1), cg('|'), 1));
    }
    public static void Box(Station st, Color foreground) {
        //Color foreground = new Color(153, 153, 153);
        var background = Color.Transparent;
        var p = st.segments.Select(s => s.position).Concat(new XY[] { st.position });
        var leftX = p.Min(p => p.x);
        var rightX = p.Max(p => p.x);
        var topX = p.Min(p => p.y);
        var bottomX = p.Max(p => p.y);
        var f = st.world.AddEffect;
        var l = ICellSurface.ConnectedLineThin;
        var cg = (int c) => new ColoredGlyph(foreground, background, c);
        f(new EffectParticle(new(leftX - 1, topX - 1), cg(l[(int)LineIndex.BottomLeft]), 1));
        f(new EffectParticle(new(leftX - 1, bottomX + 1), cg(l[(int)LineIndex.TopLeft]), 1));
        f(new EffectParticle(new(rightX + 1, topX - 1), cg(l[(int)LineIndex.BottomRight]), 1));
        f(new EffectParticle(new(rightX + 1, bottomX + 1), cg(l[(int)LineIndex.TopRight]), 1));
    }
}
