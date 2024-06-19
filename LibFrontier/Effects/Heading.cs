using Common;
using System;
using System.Linq;
using LibGamer;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
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


    static char g = (char)776;
    /*
    public static int WN = 776,
        W_NE = 777,
        W_E = 778,
        W_SE = 779,
        W_S = 780,
        NW_NE = 781,
        NW_E = 782,
        NW_SE = 783,
        NW_S = 784,
        NW_SW = 785,
        N_E = 786,
        N_SE = 787,
        N_S = 788,
        N_SW = 789,
        NE_SE = 790,
        NE_S = 791,
        NE_SW = 792,
        E_S = 793,
        E_SW = 794,
        SE_SW = 795;
    */
    public static Dictionary<((int x, int y) prev,(int x, int y) next), int> Connectors;
	static Heading()  {


        (int, int)
			W = (-1, 0), N = (0, 1), E = (1, 0), S = (0, -1), NE = (1, 1), NW = (-1, 1), SE = (1, -1), SW = (-1, -1);
        int i = 776;
        Connectors = new () {
			[(W, N)] = i+0,
			[(W, NE)]= i+1,
			[(W, E)] = i+2,
			[(W, SE)]= i+3,
			[(W, S)]= i+4,
			[(NW, NE)]= i+5,
			[(NW, E)]=i+6,
			[(NW, SE)]= i+7,
			[(NW, S)]=i+8,
			[(NW, SW)]= i+9,
			[(N, E)]=i+10,
			[(N, SE)]= i+11,
			[(N, S)]=i+12,
			[(N, SW)]=i+13,
			[(N, W)]=i+0,
			[(NE,SE)]=i+14,
			[(NE,S)]=i+15,
			[(NE,SW)]= i+16,
			[(NE,W)] = i+1,
			[(NE,NW)] = i+5,
			[(E,S)]= i+17,
			[(E, SW)] = i+18,
			[(E,W)] = i+2,
			[(E,N)] = i+10,
			[(E,NW)] = i+6,
			[(SE, SW)] = i+19,
			[(SE,W)] = i+3,
			[(SE,NW)] = i+7,
			[(SE,N)] = i+11,
			[(SE,NE)] = i+14,
			[(S,E)] = i+17,
			[(S,NE)] = i+15,
			[(S,N)] = i+12,
			[(S,NW)] = i+8,
			[(S,W)] = i+14,
			[(SW,E)] = i+18,
			[(SW,NW)] = i+9,
			[(SW,N)] = i+13,
			[(SW,NE)] = i+16,
			[(SW,E)] = i+18,
			[(SW,SE)] = i+19,
		};
    }

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






				var here = start + inc * (i + 1);
                var value = (byte)(153 - Math.Max(1, i) * 153 / length);

				var cg = new Tile(ABGR.RGB(value, value, value), ABGR.Transparent, g);
                var particle = new EffectParticle(here, cg, interval + 1);
                particles[i] = particle;
                parent.world.AddEffect(particle);
            }
        } else {
            for (int i = 0; i < particles.Length; i++) {
                var p = particles[i];

                var here = start + inc * (i + 1);
				var prev = here - inc;
				var next = here + inc;
				var c = (double x) => (int)Math.Round(Math.Clamp(x, -1, 1));
				var _c = ((double x, double y) p) => (c(p.x), c(p.y));
				var __c = ((double x, double y) prev, (double x, double y) next) => (_c(prev), _c(next));
				var g = Connectors.GetValueOrDefault(__c((prev.roundDown - here.roundDown), (next.roundDown - here.roundDown)), 249);

                p.tile = p.tile with { Glyph = (char)g };

				p.position = here;
				p.lifetime = interval + 1 + (interval * (i - particles.Length)) / particles.Length;
            }
        }
        ticks++;

    }
    public static void AimLine(System World, XY start, double angle, int lifetime = 1) {
        //ColoredGlyph pointEffect = new ColoredGlyph((char)249, new Color(153, 153, 76), Color.Transparent);
        var here = start;
        var inc = XY.Polar(angle);
        int length = 20;
        int interval = 2;
        for (int i = 0; i < length / interval; i++) {
            here += inc * interval;



			var prev = here - inc;
			var next = here + inc;
			var c = (double x) => (int)Math.Round(Math.Clamp(x, -1, 1));
			var _c = ((double x, double y) p) => (c(p.x), c(p.y));
			var __c = ((double x, double y) prev, (double x, double y) next) => (_c(prev), _c(next));
			var g = Connectors.GetValueOrDefault(__c((prev.roundDown - here.roundDown), (next.roundDown - here.roundDown)), 249);

			var pointEffect = new Tile(ABGR.RGBA(255, 255, 0, 204), ABGR.Transparent, g);

			World.AddEffect(new EffectParticle(here, pointEffect, lifetime));
        }
    }
    public static void AimLine(ActiveObject owner, double angle, Weapon w) {
        //Idea: Highlight a segment of the aimline based on the firetime left on the weapon

        var start = owner.position;
        var World = owner.world;

        //ColoredGlyph pointEffect = new ColoredGlyph((char)249, new Color(153, 153, 76), Color.Transparent);
        //ColoredGlyph dark = new ColoredGlyph(new Color(255, 255, 0, 102), Color.Transparent, (char)249);
        var bright = new Tile(ABGR.RGBA(255, 255, 0, 204), ABGR.Transparent, g);
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
        var cg = (int c) => new Tile(foreground, background, c);

		World.AddEffect(new EffectParticle(point + (1, 0), cg(256-64-12), 1));
		World.AddEffect(new EffectParticle(point + (-1, 0), cg(256-64+3), 1));
		World.AddEffect(new EffectParticle(point + (0, 1), cg(256-64+2), 1));
		World.AddEffect(new EffectParticle(point + (0, -1), cg(256-64+1), 1));
		/*
        World.AddEffect(new EffectParticle(point + (1, 0), cg(826), 1));
        World.AddEffect(new EffectParticle(point + (-1, 0), cg(825), 1));
        World.AddEffect(new EffectParticle(point + (0, 1), cg(824), 1));
        World.AddEffect(new EffectParticle(point + (0, -1), cg(856), 1));
        */
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
