using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using LibGamer;
using Silk.NET.OpenGLES;
namespace WebAtomics;
internal static partial class Interop
{
	[JSImport("initialize", "main.js")]
	public static partial void Initialize();
	public static HashSet<KC> down = [];
	//public static KB kb = new();
	public static HandState hand = new((0, 0), 0, false, false, false, true);
	[JSExport]
	public static void OnKeyDown(bool shift, bool ctrl, bool alt, bool repeat, int code){
		down.Add((KC)code);
	}
	[JSExport]
	public static void OnKeyUp(bool shift, bool ctrl, bool alt, int code){
		down.Remove((KC)code);
	}
	[JSExport]
	public static void OnMouseMove(float x, float y) {
		Console.WriteLine($"move: {x},{y}");
		hand = hand with { pos = ((int)(x*1.5), (int)Math.Round(y*1.5, MidpointRounding.ToNegativeInfinity)) };
	}
	[JSExport]
	public static void OnMouseDown(bool shift, bool ctrl, bool alt, int button){
		if(button == 0) {
			hand = hand with { leftDown = true };
		} else if(button == 2) {
			hand = hand with { rightDown = true };
		}
	}
	[JSExport]
	public static void OnMouseUp(bool shift, bool ctrl, bool alt, int button){
		if(button == 0) {
			hand = hand with { leftDown = false };
		} else if(button == 2) {
			hand = hand with { rightDown = false };
		}
	}
	[JSExport]
	public static void OnCanvasResize(float width, float height, float devicePixelRatio){
		Program.CanvasResized((int)width, (int)height);
	}
	[JSExport]
	public static void SetRootUri(string uri) {
		Program.BaseAddress = new Uri(uri);
	}

	[JSExport]
	public static void AddLocale(string locale) {
	}
}
