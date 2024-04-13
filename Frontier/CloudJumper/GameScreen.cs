using System.Collections.Generic;
using SadRogue.Primitives;
using SadConsole;
using System;
using Console = SadConsole.Console;
using SadConsole.Input;
using Common;
using System.Linq;

namespace CloudJumper;

class GameScreen : Console {
    PlayerShip playership;

    List<CloudParticle> clouds;

    Random random = new Random();
    int tick = 0;
    public GameScreen(int width, int height) : base(width, height) {
        playership = new PlayerShip(new XY(Width / 2, Height / 2));
        clouds = new List<CloudParticle>();

        UseMouse = true;
    }
    public override void Update(TimeSpan time) {
        tick++;

        playership.Position += new XY(1 / 8f, 0);


        var c = clouds.FirstOrDefault(c => c.pos == (Point)playership.Position.roundAway);
        if (c != null) {
            if (playership.Velocity.magnitude > 4) {
                int a = c.symbol.Foreground.A;
                var delta = random.Next(1, 30);
                a -= delta;
                if (a > 0) {
                    c.symbol.Foreground = c.symbol.Foreground.SetAlpha((byte)a);
                    playership.fuel += delta / 6;
                } else {
                    clouds.Remove(c);
                }
                playership.Velocity -= playership.Velocity / 20;

                UpdateGravity();
            } else {
                playership.Velocity -= playership.Velocity / 20;
            }
        } else {
            UpdateGravity();
        }
        UpdatePosition();

        void UpdatePosition() {
            playership.Position += playership.Velocity / RogueFrontier.Program.TICKS_PER_SECOND;
        }
        void UpdateGravity() {
            playership.Velocity -= new XY(0, 6 / 30f);
        }
        playership.UpdateControls();
        UpdateClouds();

        void UpdateClouds() {

            //Update clouds
            if (tick % 8 == 0) {
                clouds.ForEach(c => c.Update(random));
            }
            //Spawn cloud
            if (tick % 64 == 0) {

                int effectMinY = Height / 5;
                int effectMaxY = 4 * Height / 5;

                CloudParticle.CreateClouds(effectMinY, effectMaxY, clouds, random);
                CloudParticle.CreateClouds(effectMinY, effectMaxY, clouds, random);
                CloudParticle.CreateClouds(effectMinY, effectMaxY, clouds, random);
            }
        }
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        int top = Height - 1;

        void Draw(int x, int y, ColoredGlyph symbol) {
            this.SetCellAppearance(x, top - y, symbol);
        }

        foreach (var cloud in clouds) {
            var (x, y) = cloud.pos;
            Draw(x, y, cloud.symbol);
        }

        var pos = playership.Position.clone;
        Draw(pos.xi, pos.yi, playership.Tile);

        if (playership.thrusting) {
            var p = pos + XY.Polar(playership.rotationDegrees * Math.PI / 180, -1);
            Draw(p.xi, p.yi, new ColoredGlyph(Color.Yellow, Color.Transparent, (char)249));
        }

        for (int i = 0; i < 10; i++) {
            pos += XY.Polar(playership.rotationDegrees * Math.PI / 180, 1);
            Draw(pos.xi, pos.yi, new ColoredGlyph(Color.White, Color.Transparent, (char)249));
        }

        this.Print(1, 1, $"{new string('=', playership.fuel / 15)}");

        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyDown(Keys.Up)) {
            if (playership.fuel > 0) {
                playership.SetThrusting(true);
            }
        }
        if (info.IsKeyDown(Keys.Left)) {
            playership.SetRotating(RogueFrontier.Rotating.CCW);
        }
        if (info.IsKeyDown(Keys.Right)) {
            playership.SetRotating(RogueFrontier.Rotating.CW);
        }

        return base.ProcessKeyboard(info);
    }
}
