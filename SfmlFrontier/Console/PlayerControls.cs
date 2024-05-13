using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using static LibGamer.KC;
using static RogueFrontier.Control;
using Helper = Common.Main;
using System;
using LibGamer;

namespace RogueFrontier;

public enum Control {
    Thrust,
    TurnRight,
    TurnLeft,
    Brake,
    Autopilot,
    Dock,
    TargetFriendly,
    ClearTarget,
    ShipStatus,
    Gate,
    TargetEnemy,
    InvokePowers,
    NextPrimary,
    FirePrimary,
    FireSecondary,
    AutoAim
}
public class PlayerControls {
    public PlayerShip playerShip;
    private Mainframe playerMain;
    public PlayerInput input=new();
    public PlayerControls(PlayerShip playerShip, Mainframe playerMain) {
        this.playerShip = playerShip;
        this.playerMain = playerMain;
    }
    public void ProcessArrows() {
        if (input.Thrust) {
            playerShip.SetThrusting();
        }
        if (input.TurnLeft) {
            playerShip.SetRotating(Rotating.CCW);
            if (!input.UsingMouse) {
                playerMain?.SleepMouse();
            }
        }
        if (input.TurnRight) {
            playerShip.SetRotating(Rotating.CW);
            if (!input.UsingMouse) {
                playerMain?.SleepMouse();
            }
        }
        if (input.Brake) {
            playerShip.SetDecelerating();
        }
    }
    public void ProcessTargeting() {
        if (input.TargetFriendly) {
            playerShip.NextTargetFriendly();
        }
        if (input.TargetMouse && playerMain.mouseWorldPos != null) {
            playerMain.TargetMouse();
        }
        if (input.ClearTarget) {
            playerShip.ClearTarget();
        }
        if (input.TargetEnemy) {
            playerShip.NextTargetEnemy();
        }
        if (input.NextPrimary) {
            playerShip.NextPrimary();
        }
        if (input.NextSecondary) {
            playerShip.NextSecondary();
        }
        if (input.FirePrimary) {
            playerShip.SetFiringPrimary();
        }
        if (input.FireSecondary) {
            playerShip.SetFiringSecondary();
        }
        if (input.AutoAim) {
            if (playerShip.GetTarget(out var target) && playerShip.CanSee(target) && playerShip.GetPrimary(out Weapon w)) {
                playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.position - playerShip.position, target.velocity - playerShip.velocity, w.projectileDesc.missileSpeed, out _));
            }
        }
    }

    public void ProcessCommon() {
        if (input.ToggleUI) {
            playerMain.audio.button_press.Play();
            playerMain.uiMain.visible = !playerMain.uiMain.visible;
        }
        if (input.Gate) {
            playerShip.DisengageAutopilot();
            playerMain.Gate();
        }
        if (input.Autopilot && !playerMain.autopilotUpdate) {
            playerShip.autopilot = !playerShip.autopilot;
            playerMain.audio.PlayAutopilot(playerShip.autopilot);
            playerShip.AddMessage(new Message($"Autopilot {(playerShip.autopilot ? "engaged" : "disengaged")}"));
        }
        if (input.Dock) {
            if (input.Shift) {
                var dockable = playerShip.world.entities.all.OfType<IDockable>().OrderBy(d => (d.position - playerShip.position).magnitude2).ToList();
                playerMain.dialog = SListWidget.DockList(playerMain, dockable, playerShip);
            } else if (playerShip.dock.Target != null) {
                if (playerShip.dock.docked) {
                    playerShip.AddMessage(new Message("Undocked"));
                } else {
                    playerShip.AddMessage(new Message("Docking canceled"));
                }
                playerShip.dock.Clear();
                playerMain.audio.PlayDocking(false);
            } else {
                if (playerShip.GetTarget(out var t) && playerShip.position.Dist(t.position) < 24) {
                    if (t is not IDockable d) {
                        playerShip.AddMessage(new Transmission(t, "Target is not dockable"));
                    } else {
                        Dock(d);
                    }
                } else {
                    var dest = playerShip.world.entities
                        .FilterKey(p => (playerShip.position - p).magnitude < 8)
                        .Select(p => p is ISegment s ? s.parent : p)
                        .OfType<IDockable>()
                        .MinBy(d => playerShip.position.Dist(d.position));
                    if (dest != null) {
                        Dock(dest);
                    } else {
                        playerShip.AddMessage(new Message("No dock target in range"));
                        playerMain.audio.PlayError();
                    }
                }
                void Dock(IDockable dest) {
                    playerShip.AddMessage(new Transmission(dest, "Docking initiated"));
                    playerShip.dock.SetTarget(dest, dest.GetDockPoints().MinBy(playerShip.position.Dist));
                    playerMain.audio.PlayDocking(true);
                }
            }
        }
        if (input.ShipMenu) {
            playerMain.audio.button_press.Play();
            playerShip.DisengageAutopilot();
            playerMain.dialog = new ShipMenu(playerMain, playerMain.sf, playerShip, playerMain.story);
        }
    }
    public void ProcessWithMenu() {
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
    }
    public void ProcessAll() {
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
        var pw = playerMain.powerWidget;
        if (input.Escape) {
            playerMain.audio.button_press.Play();
            if (pw?.Surface.IsVisible == true) {
                pw.Surface.IsVisible = false;
            } else {
                playerMain.pauseScreen.IsVisible = true;
            }
        }
        if (input.InvokePowers && pw != null) {
            playerMain.audio.button_press.Play();
            pw.Surface.IsVisible = !pw.Surface.IsVisible;
        }
        if (keys?[U] == KS.Pressed) {
            playerMain.audio.button_press.Play();
#if false
            playerMain.sceneContainer.Children.Add(SListWidget.UsefulItems(playerMain, playerShip));
#endif
        }
        if (input.NetworkMap && playerMain.networkMap is var nm) {
            playerMain.audio.button_press.Play();
            nm.IsVisible = !nm.IsVisible;
        }


        if(keys?[B] == KS.Pressed) {
            playerMain.audio.button_press.Play();
#if false
            playerMain.sceneContainer.Children.Add(SListWidget.ManageDevices(playerMain, playerShip));
#endif
        }
        if(keys?[C] == KS.Pressed) {
            playerMain.audio.button_press.Play();
#if false
            playerMain.sceneContainer.Children.Add(SListWidget.Communications(playerMain, playerShip));
#endif
        }


        if (keys[F1] == KS.Pressed) {
            playerMain.audio.button_press.Play();
            playerMain.dialog = new IdentityScreen(playerMain);
            //playerMain.OnIntermission();
        }
    }
    KB keys = null;
    public void UpdateInput(KB info) {
        keys = info;
        input.Read(playerMain.Settings.controls, info);
    }
    public static Dictionary<Control, KC> standard => new() {
        { Thrust, Up },
        { TurnRight, Right },
        { TurnLeft, Left },
        { Brake, Down },
        { Autopilot, A },
        { Dock, D },
        { TargetFriendly, F },
        { ClearTarget, R },
        { Gate, G },
        { Control.ShipStatus, S },
        { TargetEnemy, T },
        { InvokePowers, I },
        { NextPrimary, W },
        { FirePrimary, X },
        { FireSecondary, LeftControl },
        { AutoAim, Z }
    };
}
public class PlayerInput {
    public bool Shift;
    public bool Thrust, TurnLeft, TurnRight, Brake;
    public bool TargetFriendly, TargetMouse, TargetEnemy, ClearTarget, NextPrimary, NextSecondary, FirePrimary, FireSecondary, AutoAim;
    public bool QuickZoom;
    public bool ToggleUI, Gate, Autopilot, Dock, ShipMenu;
    public bool Escape, InvokePowers, Communications, NetworkMap;

    public bool UsingMouse;
    public PlayerInput() { }
    public void Read(Dictionary<Control, KC> controls, KB kb) {
        var p = (Control c) => kb[controls[c]] == KS.Pressed;
        var d = (Control c) => kb[controls[c], 1];
        Shift = kb[LeftShift, 1] || kb[RightShift, 1];
        Thrust =        d(Control.Thrust);
        TurnLeft =      d(Control.TurnLeft);
        TurnRight =     d(Control.TurnRight);
        Brake =         d(Control.Brake);
        TargetFriendly =p(Control.TargetFriendly) && !Shift;
        TargetMouse =   p(Control.TargetFriendly) && Shift;
        ClearTarget =   p(Control.ClearTarget);
        TargetEnemy =   p(Control.TargetEnemy) && !Shift;
        TargetMouse =   p(Control.TargetEnemy) && Shift;
        NextPrimary =   p(Control.NextPrimary) && !Shift;
        NextSecondary = p(Control.NextPrimary) && Shift;
        FirePrimary =   d(Control.FirePrimary);
        FireSecondary = d(Control.FireSecondary);
        AutoAim =       d(Control.AutoAim);
        ToggleUI =      kb[Tab] == KS.Pressed;
        Gate =          p(Control.Gate);
        Autopilot =     p(Control.Autopilot);
        Dock =          p(Control.Dock);
        ShipMenu =      p(Control.ShipStatus);
        Escape =        kb[KC.Escape] == KS.Pressed;
        InvokePowers =  p(Control.InvokePowers);
        Communications= kb[C] == KS.Pressed;
        NetworkMap=     kb[N] == KS.Pressed;
        QuickZoom =     kb[M] == KS.Pressed;
    }
    public void ClientOnly() {
        Autopilot = false;
    }
    public void ServerOnly() {
        ToggleUI = false;
        Autopilot = false;
        ShipMenu = false;
        Escape = false;
        InvokePowers = false;
    }
}