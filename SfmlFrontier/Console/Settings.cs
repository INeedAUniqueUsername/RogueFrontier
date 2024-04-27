using Newtonsoft.Json;
using static RogueFrontier.Control;
using LibGamer;
namespace RogueFrontier;

public class Settings {
    //Remember to update whenever we load game
    public Dictionary<Control, KC> controls = new Dictionary<Control, KC>();
    [JsonIgnore]
    public static Settings standard => new Settings() {
        controls = PlayerControls.standard
    };
    public Settings() {
    }
    public string GetString() {
        const int w = -16;
        return @$"[Controls]

{$"[Escape]",-16}Pause

{$"[{controls[Thrust]}]",w}Thrust
{$"[{controls[TurnLeft]}]",w}Turn left
{$"[{controls[TurnRight]}]",w}Turn right
{$"[{controls[Brake]}]",w}Brake

{$"[{controls[InvokePowers]}]",w}Power menu

{$"[{controls[Autopilot]}]",w}Autopilot
{$"[{controls[ShipStatus]}]",w}Ship Screen
{$"[{controls[Dock]}]",w}Dock

{$"[Minus]",w}Megamap zoom out
{$"[Plus]",w}Megamap zoom in

{$"[{controls[ClearTarget]}]",w}Clear target
{$"[{controls[TargetEnemy]}]",w}Target next enemy
{$"[{controls[TargetFriendly]}]",w}Target next friendly

{$"[{controls[NextPrimary]}]",w}Next primary weapon
{$"[{controls[AutoAim]}]",w}Turn to aim target
{$"[{controls[FirePrimary]}]",w}Fire primary weapon

{$"[Left Click]",w}Next primary weapon
{$"[Right Click]",w}Thrust
{$"[Middle Click]",w}Target nearest
{$"[Mouse Wheel]",w}Select primary weapon".Replace("\r", null);
    }

}
