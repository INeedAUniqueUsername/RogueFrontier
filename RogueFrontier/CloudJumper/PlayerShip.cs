using Common;
using System;
using SadRogue.Primitives;
using RogueFrontier;
using SadConsole;

namespace CloudJumper;

class PlayerShip {
    public XY Position { get; set; }
    public XY Velocity { get; set; }
    public bool Active { get; set; }

    public delegate void Destroyed();
    public event Destroyed OnDestroyed;

    public int fuel = 300;

    public double rotationDegrees { get; set; }

    public bool thrusting;
    public Rotating rotating;

    public readonly double thrust = 18 / 30f;
    public readonly double turningSpeed = 60 / 30f;

    public PlayerShip(XY Position) {
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
    public void UpdateControls() {
        UpdateThrust();
        UpdateTurn();

        void UpdateThrust() {
            if (thrusting) {
                var rotationRads = rotationDegrees * Math.PI / 180;

                Velocity += XY.Polar(rotationRads, thrust);
                thrusting = false;
                fuel--;
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
    public ColoredGlyph Tile => new ColoredGlyph(Color.Magenta, Color.Transparent, 'A');
}
