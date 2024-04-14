using Common;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using TranscendenceRL;
using SadConsole;

namespace CloudJumper {
    class PlayerShip {
        public World World { get; private set; }
        public XY Position { get; set; }
        public XY Velocity { get; set; }
        public bool Active { get; set; }

        public delegate void Destroyed();
        public event Destroyed OnDestroyed;

        public double rotationDegrees { get; set; }

        public bool thrusting;
        public Rotating rotating;

        public readonly double thrust = 0.4;
        public readonly double turningSpeed = 5;

        public PlayerShip(World world) {
            this.World = world;
            this.Position = Position;
            Velocity = new XY();

            this.Active = true;
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void Destroy() {
            Active = false;
        }
        public void Update() {
            UpdateControls();
            UpdateMotion();
            //Devices.Update(this);
        }
        public void UpdateControls() {
            UpdateThrust();
            UpdateTurn();

            void UpdateThrust() {
                if (thrusting) {
                    var rotationRads = rotationDegrees * Math.PI / 180;

                    var exhaust = new EffectParticle(Position + XY.Polar(rotationRads, -1),
                        Velocity + XY.Polar(rotationRads, -thrust),
                        new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                        4);
                    World.AddEffect(exhaust);

                    Velocity += XY.Polar(rotationRads, thrust);
                    thrusting = false;
                }
            }
            void UpdateTurn() {

                if (rotating != Rotating.None) {
                    if (rotating == Rotating.CCW) {
                        rotationDegrees += turningSpeed;
                    } else if (rotating == Rotating.CW) {
                        rotationDegrees -= turningSpeed;
                    }
                    rotating = Rotating.None;
                }
            }
        }
        public void UpdateMotion() {
            Position += new XY(1, 0);
            Position += Velocity / TranscendenceRL.TranscendenceRL.TICKS_PER_SECOND;
        }
        public ColoredGlyph Tile => new ColoredGlyph(Color.Magenta, Color.Transparent, 'A');
    }


}
